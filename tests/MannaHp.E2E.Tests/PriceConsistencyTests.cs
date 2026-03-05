using System.Text.Json;
using FluentAssertions;
using MannaHp.E2E.Tests.Fixtures;
using MannaHp.PrintAgent;
using MannaHp.Server.Data;
using MannaHp.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace MannaHp.E2E.Tests;

/// <summary>
/// Verifies that every customer-visible price is consistent across all layers:
///   DB (admin-entered) → Menu API → UI display → Cart → API order response →
///   Database order snapshot → Stripe amount → Receipt PDF → Confirmation page
/// </summary>
[TestFixture]
public class PriceConsistencyTests
{
    // Known seed data IDs
    private static readonly Guid BowlMenuItemId = Guid.Parse("c0000000-0001-0000-0000-000000000001");
    private static readonly Guid LatteMenuItemId = Guid.Parse("c0000000-0009-0000-0000-000000000009");

    // ═══════════════════════════════════════════════════════════════
    //  Test 1: Burrito Bowl — every ingredient price across all layers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies each bowl ingredient's customerPrice (set on the admin side) flows
    /// consistently to: Menu API → Bowl Builder UI → Cart → API order response →
    /// DB snapshot (OrderItemIngredient.PriceCharged) → Stripe → Receipt PDF →
    /// Order confirmation page.
    /// </summary>
    [Test]
    public async Task BowlOrder_EveryIngredientPrice_ConsistentAcrossAllLayers()
    {
        // ── Phase 1: Source of truth — admin-set prices in the database ──

        await using var db = CreateDbContext();
        var bowlIngredients = await db.MenuItemAvailableIngredients
            .Include(a => a.Ingredient)
            .Where(a => a.MenuItemId == BowlMenuItemId && a.Active)
            .ToListAsync();

        // ingredient name → admin-set customerPrice
        var adminPrices = bowlIngredients.ToDictionary(
            a => a.Ingredient!.Name,
            a => a.CustomerPrice);

        adminPrices.Should().NotBeEmpty("bowl should have seeded ingredients");

        // ── Phase 2: Menu API must return matching prices ──

        using var http = new HttpClient();
        var apiMenu = JsonDocument.Parse(
            await http.GetStringAsync($"{E2EFixture.ApiBaseUrl}/api/menu-items/{BowlMenuItemId}"));

        foreach (var apiIng in apiMenu.RootElement.GetProperty("availableIngredients").EnumerateArray())
        {
            var name = apiIng.GetProperty("ingredientName").GetString()!;
            var price = apiIng.GetProperty("customerPrice").GetDecimal();
            price.Should().Be(adminPrices[name],
                $"Menu API customerPrice for '{name}' should match DB");
        }

        // ── Phase 3: Bowl builder UI must display matching prices ──

        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);

        await page.WaitForSelectorAsync("text=Bowls");
        await page.GetByText("Bowls").ClickAsync();

        await page.WaitForSelectorAsync("text=Burrito Bowl");
        await page.GetByText("Burrito Bowl").First.ClickAsync();

        await page.WaitForSelectorAsync("text=Build Your Burrito Bowl");

        // Extract ingredient name → displayed price from the builder tiles
        var displayedPrices = await page.EvaluateAsync<JsonElement>(@"() => {
            const cards = document.querySelectorAll('.cursor-pointer');
            const prices = {};
            for (const card of cards) {
                const nameEl = card.querySelector('.font-medium');
                if (!nameEl) continue;
                const spans = card.querySelectorAll('span');
                for (const span of spans) {
                    const t = span.textContent.trim();
                    if (t.startsWith('$')) {
                        prices[nameEl.textContent.trim()] = t.replace('$', '');
                        break;
                    }
                }
            }
            return prices;
        }");

        foreach (var prop in displayedPrices.EnumerateObject())
        {
            var displayedPrice = decimal.Parse(prop.Value.GetString()!);
            adminPrices.Should().ContainKey(prop.Name);
            displayedPrice.Should().Be(adminPrices[prop.Name],
                $"bowl builder price for '{prop.Name}' should match DB admin price");
        }

        // ── Phase 4: Select ingredients, verify running total ──

        var selectedNames = new[] { "Jasmine Rice", "Ground Beef", "Fresh Salsa", "Shredded Cheese" };
        var expectedUnitPrice = selectedNames.Sum(n => adminPrices[n]);

        foreach (var name in selectedNames)
            await page.Locator(".cursor-pointer").Filter(new() { HasText = name }).ClickAsync();

        // Read the bowl total from the sticky footer
        var footerTotal = await ExtractFooterTotal(page);
        footerTotal.Should().Be(expectedUnitPrice,
            "bowl builder total should equal sum of selected ingredient prices");

        // ── Phase 5: Add to cart, verify cart line total ──

        await page.GetByRole(AriaRole.Button, new() { Name = "Add to Cart" }).ClickAsync();
        await page.WaitForTimeoutAsync(500);

        await OpenCartDrawer(page);
        var cartData = await ExtractCartTotals(page);
        cartData.Subtotal.Should().Be(expectedUnitPrice,
            "cart subtotal should equal bowl unit price");

        // ── Phase 6: Place order and capture API response ──

        var (orderId, apiOrder) = await PlaceInStoreOrderAndCapture(page);

        apiOrder.Subtotal.Should().Be(expectedUnitPrice, "API subtotal should match expected");
        apiOrder.Items[0].UnitPrice.Should().Be(expectedUnitPrice,
            "API item unitPrice should equal sum of ingredient prices");

        // Verify every ingredient's priceCharged in the API response
        foreach (var apiIng in apiOrder.Items[0].Ingredients)
        {
            adminPrices.Should().ContainKey(apiIng.Name);
            apiIng.PriceCharged.Should().Be(adminPrices[apiIng.Name],
                $"API priceCharged for '{apiIng.Name}' should match admin price");
        }

        // ── Phase 7: Database order — verify all price snapshots ──

        await using var db2 = CreateDbContext();
        var dbOrder = await LoadOrderWithIncludes(db2, orderId);

        dbOrder.Subtotal.Should().Be(apiOrder.Subtotal, "DB subtotal should match API");
        dbOrder.Tax.Should().Be(apiOrder.Tax, "DB tax should match API");
        dbOrder.Total.Should().Be(apiOrder.Total, "DB total should match API");

        var dbItem = dbOrder.Items.Single();
        dbItem.UnitPrice.Should().Be(expectedUnitPrice);
        dbItem.TotalPrice.Should().Be(expectedUnitPrice * dbItem.Quantity);

        foreach (var dbIng in dbItem.Ingredients)
        {
            var ingName = dbIng.Ingredient!.Name;
            dbIng.PriceCharged.Should().Be(adminPrices[ingName],
                $"DB PriceCharged snapshot for '{ingName}' should match admin price");
        }

        // ── Phase 8: Stripe amount ──

        var stripeCents = (long)(dbOrder.Total * 100);
        stripeCents.Should().Be((long)(apiOrder.Total * 100));

        // ── Phase 9: Receipt PDF — verify every ingredient price ──

        var receiptText = GenerateReceiptText(dbOrder);

        foreach (var name in selectedNames)
        {
            receiptText.Should().Contain($"${adminPrices[name]:F2}",
                $"receipt should show price for '{name}'");
        }
        receiptText.Should().Contain($"${dbItem.TotalPrice:F2}", "receipt should show item total");
        VerifyReceiptOrderTotals(receiptText, dbOrder);

        // ── Phase 10: Confirmation page ──

        await page.WaitForURLAsync("**/order/**", new() { Timeout = 30_000 });
        await page.WaitForSelectorAsync("text=Order Details");

        var confirmData = await ExtractPageTotals(page);
        VerifyTotalsMatch(confirmData, dbOrder, "confirmation page");

        // ── Tax math integrity ──
        VerifyTaxMath(dbOrder);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Test 2: Coffee + Add-ons — variant price and add-on prices
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies variant price (e.g., Latte 16oz = $5.25) and add-on prices
    /// (Extra Espresso = $1.00, Whipped Cream = $0.50) flow consistently
    /// from DB → Menu API → Item Detail UI → Cart → API → DB snapshot →
    /// Stripe → Receipt PDF → Confirmation page.
    /// </summary>
    [Test]
    public async Task CoffeeWithAddOns_VariantAndAddOnPrices_ConsistentAcrossAllLayers()
    {
        // ── Phase 1: Source of truth — admin-set prices in the database ──

        await using var db = CreateDbContext();

        var latteVariants = await db.MenuItemVariants
            .Where(v => v.MenuItemId == LatteMenuItemId && v.Active)
            .ToDictionaryAsync(v => v.Name, v => v.Price);

        var latteAddOns = await db.MenuItemAvailableIngredients
            .Include(a => a.Ingredient)
            .Where(a => a.MenuItemId == LatteMenuItemId && a.Active)
            .ToListAsync();

        var addOnPrices = latteAddOns.ToDictionary(
            a => a.Ingredient!.Name,
            a => a.CustomerPrice);

        // Verify known seed data matches expectations
        latteVariants["12oz"].Should().Be(4.75m);
        latteVariants["16oz"].Should().Be(5.25m);
        addOnPrices["Espresso"].Should().Be(1.00m);
        addOnPrices["Whipped Cream"].Should().Be(0.50m);

        // ── Phase 2: Menu API must return matching prices ──

        using var http = new HttpClient();
        var apiMenu = JsonDocument.Parse(
            await http.GetStringAsync($"{E2EFixture.ApiBaseUrl}/api/menu-items/{LatteMenuItemId}"));
        var root = apiMenu.RootElement;

        foreach (var v in root.GetProperty("variants").EnumerateArray())
        {
            var name = v.GetProperty("name").GetString()!;
            v.GetProperty("price").GetDecimal().Should().Be(latteVariants[name],
                $"API variant price for '{name}' should match DB");
        }

        foreach (var a in root.GetProperty("availableIngredients").EnumerateArray())
        {
            var name = a.GetProperty("ingredientName").GetString()!;
            a.GetProperty("customerPrice").GetDecimal().Should().Be(addOnPrices[name],
                $"API add-on price for '{name}' should match DB");
        }

        // ── Phase 3: Item detail UI must display matching prices ──

        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);

        await page.WaitForSelectorAsync("text=Traditional Drinks");
        await page.GetByText("Traditional Drinks").ClickAsync();

        // Verify menu card shows correct price range ($4.75 – $5.25)
        var latteCard = page.Locator("a").Filter(
            new() { Has = page.Locator("h3:text-is('Latte')") });
        var cardPriceText = await latteCard.Locator(".font-semibold").First.TextContentAsync();
        cardPriceText.Should().Contain($"${latteVariants.Values.Min():F2}",
            "menu card should show min variant price");
        cardPriceText.Should().Contain($"${latteVariants.Values.Max():F2}",
            "menu card should show max variant price");

        await latteCard.ClickAsync();
        await page.WaitForSelectorAsync("text=Choose a size");

        // Extract variant button prices
        var displayedVariants = await page.EvaluateAsync<JsonElement>(@"() => {
            const buttons = document.querySelectorAll('button .flex.items-center.justify-between');
            const result = {};
            for (const row of buttons) {
                const spans = row.querySelectorAll('span');
                if (spans.length >= 2) {
                    const name = spans[0].textContent.trim();
                    const price = spans[1].textContent.trim();
                    if (price.startsWith('$'))
                        result[name] = price.replace('$', '');
                }
            }
            return result;
        }");

        foreach (var prop in displayedVariants.EnumerateObject())
        {
            var varPrice = decimal.Parse(prop.Value.GetString()!);
            latteVariants.Should().ContainKey(prop.Name);
            varPrice.Should().Be(latteVariants[prop.Name],
                $"displayed variant price for '{prop.Name}' should match DB");
        }

        // Extract add-on prices (prefixed with +$)
        var displayedAddOns = await page.EvaluateAsync<JsonElement>(@"() => {
            const cards = document.querySelectorAll('.cursor-pointer');
            const result = {};
            for (const card of cards) {
                const nameEl = card.querySelector('.font-medium');
                if (!nameEl) continue;
                const spans = card.querySelectorAll('span');
                for (const span of spans) {
                    const t = span.textContent.trim();
                    if (t.startsWith('+$')) {
                        result[nameEl.textContent.trim()] = t.replace('+$', '');
                        break;
                    }
                }
            }
            return result;
        }");

        foreach (var prop in displayedAddOns.EnumerateObject())
        {
            var price = decimal.Parse(prop.Value.GetString()!);
            addOnPrices.Should().ContainKey(prop.Name);
            price.Should().Be(addOnPrices[prop.Name],
                $"displayed add-on price for '{prop.Name}' should match DB");
        }

        // ── Phase 4: Select 16oz + Espresso + Whipped Cream, verify total ──

        await page.GetByText("16oz").ClickAsync();

        await page.Locator(".cursor-pointer")
            .Filter(new() { HasText = "Espresso" }).First.ClickAsync();
        await page.Locator(".cursor-pointer")
            .Filter(new() { HasText = "Whipped Cream" }).First.ClickAsync();

        var selectedVariantPrice = latteVariants["16oz"]; // 5.25
        var selectedAddOnNames = new[] { "Espresso", "Whipped Cream" };
        var addOnTotal = selectedAddOnNames.Sum(n => addOnPrices[n]); // 1.50
        var expectedUnitPrice = selectedVariantPrice + addOnTotal; // 6.75

        var footerTotal = await ExtractFooterTotal(page);
        footerTotal.Should().Be(expectedUnitPrice,
            "item detail footer should show variant + add-on total");

        // ── Phase 5: Add to cart, verify cart prices ──

        await page.GetByRole(AriaRole.Button, new() { Name = "Add to Cart" }).ClickAsync();
        await page.WaitForTimeoutAsync(500);

        await OpenCartDrawer(page);
        var cartData = await ExtractCartTotals(page);
        cartData.Subtotal.Should().Be(expectedUnitPrice,
            "cart subtotal should equal variant + add-ons");

        // ── Phase 6: Place order, verify API response item + ingredient prices ──

        var (orderId, apiOrder) = await PlaceInStoreOrderAndCapture(page);

        apiOrder.Subtotal.Should().Be(expectedUnitPrice);
        apiOrder.Items[0].UnitPrice.Should().Be(expectedUnitPrice,
            "API unitPrice should equal variant + add-ons");

        foreach (var apiIng in apiOrder.Items[0].Ingredients)
        {
            addOnPrices.Should().ContainKey(apiIng.Name);
            apiIng.PriceCharged.Should().Be(addOnPrices[apiIng.Name],
                $"API priceCharged for '{apiIng.Name}' should match admin price");
        }

        // ── Phase 7: Database order snapshots ──

        await using var db2 = CreateDbContext();
        var dbOrder = await LoadOrderWithIncludes(db2, orderId);

        dbOrder.Subtotal.Should().Be(apiOrder.Subtotal);
        dbOrder.Tax.Should().Be(apiOrder.Tax);
        dbOrder.Total.Should().Be(apiOrder.Total);

        var dbItem = dbOrder.Items.Single();
        dbItem.UnitPrice.Should().Be(expectedUnitPrice,
            "DB UnitPrice should equal variant + add-ons");

        foreach (var dbIng in dbItem.Ingredients)
        {
            var ingName = dbIng.Ingredient!.Name;
            dbIng.PriceCharged.Should().Be(addOnPrices[ingName],
                $"DB PriceCharged for '{ingName}' should match admin price");
        }

        // ── Phase 8: Stripe amount ──

        ((long)(dbOrder.Total * 100)).Should().Be((long)(apiOrder.Total * 100));

        // ── Phase 9: Receipt PDF — variant line implied, add-on lines explicit ──

        var receiptText = GenerateReceiptText(dbOrder);

        foreach (var addOnName in selectedAddOnNames)
        {
            receiptText.Should().Contain($"${addOnPrices[addOnName]:F2}",
                $"receipt should show add-on price for '{addOnName}'");
        }
        receiptText.Should().Contain($"${expectedUnitPrice:F2}",
            "receipt should show correct item total");
        VerifyReceiptOrderTotals(receiptText, dbOrder);

        // ── Phase 10: Confirmation page ──

        await page.WaitForURLAsync("**/order/**", new() { Timeout = 30_000 });
        await page.WaitForSelectorAsync("text=Order Details");

        var confirmData = await ExtractPageTotals(page);
        VerifyTotalsMatch(confirmData, dbOrder, "confirmation page");

        // Verify individual item price on confirmation
        var confirmItemPrice = await page.EvaluateAsync<string>(@"() => {
            const rows = document.querySelectorAll('.flex.justify-between.text-sm');
            for (const row of rows) {
                if (row.textContent.includes('Latte')) {
                    const m = row.textContent.match(/\$(\d+\.\d{2})/);
                    return m ? m[1] : '0';
                }
            }
            return '0';
        }");
        decimal.Parse(confirmItemPrice!).Should().Be(expectedUnitPrice,
            "confirmation page should show correct item total price");

        VerifyTaxMath(dbOrder);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Test 3: Multi-item order — bowl + coffee aggregate totals
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Orders both a customizable bowl and a fixed coffee drink with add-ons
    /// in a single order. Verifies that individual item prices AND the aggregate
    /// subtotal/tax/total are consistent across cart → API → DB → Stripe →
    /// receipt → confirmation.
    /// </summary>
    [Test]
    public async Task MultiItemOrder_AllItemPricesAndTotals_ConsistentAcrossAllLayers()
    {
        // ── Phase 1: Source of truth from DB ──

        await using var db = CreateDbContext();

        var bowlAdminPrices = (await db.MenuItemAvailableIngredients
            .Include(a => a.Ingredient)
            .Where(a => a.MenuItemId == BowlMenuItemId && a.Active)
            .ToListAsync())
            .ToDictionary(a => a.Ingredient!.Name, a => a.CustomerPrice);

        var latteVariantPrices = await db.MenuItemVariants
            .Where(v => v.MenuItemId == LatteMenuItemId && v.Active)
            .ToDictionaryAsync(v => v.Name, v => v.Price);

        var latteAddOnPrices = (await db.MenuItemAvailableIngredients
            .Include(a => a.Ingredient)
            .Where(a => a.MenuItemId == LatteMenuItemId && a.Active)
            .ToListAsync())
            .ToDictionary(a => a.Ingredient!.Name, a => a.CustomerPrice);

        // ── Phase 2: Add bowl to cart ──

        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);

        // Navigate to bowl builder
        await page.WaitForSelectorAsync("text=Bowls");
        await page.GetByText("Bowls").ClickAsync();
        await page.WaitForSelectorAsync("text=Burrito Bowl");
        await page.GetByText("Burrito Bowl").First.ClickAsync();
        await page.WaitForSelectorAsync("text=Build Your Burrito Bowl");

        // Select: Rice ($3) + Beans ($2) + Chicken ($3) + Lettuce ($0.50) + Salsa ($0.50)
        var bowlIngredients = new[] { "Jasmine Rice", "Beans", "Chicken", "Lettuce", "Fresh Salsa" };
        foreach (var name in bowlIngredients)
            await page.Locator(".cursor-pointer").Filter(new() { HasText = name }).ClickAsync();

        var bowlUnitPrice = bowlIngredients.Sum(n => bowlAdminPrices[n]); // 9.00

        var bowlFooter = await ExtractFooterTotal(page);
        bowlFooter.Should().Be(bowlUnitPrice, "bowl footer should match sum of ingredients");

        await page.GetByRole(AriaRole.Button, new() { Name = "Add to Cart" }).ClickAsync();
        await page.WaitForTimeoutAsync(500);

        // ── Phase 3: Navigate back and add coffee with add-ons ──

        await page.GotoAsync(E2EFixture.NextBaseUrl);

        await page.WaitForSelectorAsync("text=Traditional Drinks");
        await page.GetByText("Traditional Drinks").ClickAsync();
        await page.WaitForSelectorAsync("text=Latte");
        await page.GetByText("Latte").First.ClickAsync();
        await page.WaitForSelectorAsync("text=Choose a size");

        // Select 12oz + Extra Espresso
        await page.GetByText("12oz").ClickAsync();
        await page.Locator(".cursor-pointer")
            .Filter(new() { HasText = "Espresso" }).First.ClickAsync();

        var latteUnitPrice = latteVariantPrices["12oz"] + latteAddOnPrices["Espresso"]; // 5.75

        var latteFooter = await ExtractFooterTotal(page);
        latteFooter.Should().Be(latteUnitPrice, "latte footer should match variant + add-on");

        await page.GetByRole(AriaRole.Button, new() { Name = "Add to Cart" }).ClickAsync();
        await page.WaitForTimeoutAsync(500);

        // ── Phase 4: Open cart, verify both items and aggregate totals ──

        await OpenCartDrawer(page);
        var cartData = await ExtractCartTotals(page);

        var expectedSubtotal = bowlUnitPrice + latteUnitPrice;
        var expectedTax = Math.Round(expectedSubtotal * 0.0825m, 2);
        var expectedTotal = expectedSubtotal + expectedTax;

        cartData.Subtotal.Should().Be(expectedSubtotal, "cart subtotal should be bowl + latte");
        cartData.Tax.Should().Be(expectedTax, "cart tax should be correct");
        cartData.Total.Should().Be(expectedTotal, "cart total should be subtotal + tax");

        // ── Phase 5: Place order, verify API response ──

        var (orderId, apiOrder) = await PlaceInStoreOrderAndCapture(page);

        apiOrder.Subtotal.Should().Be(expectedSubtotal, "API subtotal");
        apiOrder.Tax.Should().Be(expectedTax, "API tax");
        apiOrder.Total.Should().Be(expectedTotal, "API total");
        apiOrder.Items.Should().HaveCount(2);

        // Find bowl item and latte item in the API response
        var apiBowl = apiOrder.Items.First(i => i.Ingredients.Count > 1);
        var apiLatte = apiOrder.Items.First(i => i != apiBowl);

        apiBowl.UnitPrice.Should().Be(bowlUnitPrice, "API bowl unitPrice");
        apiLatte.UnitPrice.Should().Be(latteUnitPrice, "API latte unitPrice");

        // Verify bowl ingredient prices
        foreach (var ing in apiBowl.Ingredients)
        {
            ing.PriceCharged.Should().Be(bowlAdminPrices[ing.Name],
                $"API bowl ingredient '{ing.Name}' priceCharged should match admin");
        }

        // Verify latte add-on price
        foreach (var ing in apiLatte.Ingredients)
        {
            ing.PriceCharged.Should().Be(latteAddOnPrices[ing.Name],
                $"API latte add-on '{ing.Name}' priceCharged should match admin");
        }

        // ── Phase 6: Database verification ──

        await using var db2 = CreateDbContext();
        var dbOrder = await LoadOrderWithIncludes(db2, orderId);

        dbOrder.Subtotal.Should().Be(expectedSubtotal);
        dbOrder.Tax.Should().Be(expectedTax);
        dbOrder.Total.Should().Be(expectedTotal);

        foreach (var dbItem in dbOrder.Items)
        {
            dbItem.TotalPrice.Should().Be(dbItem.UnitPrice * dbItem.Quantity);
            foreach (var dbIng in dbItem.Ingredients)
            {
                var ingName = dbIng.Ingredient!.Name;
                // Determine which price map to use based on the menu item
                var expected = dbItem.MenuItemId == BowlMenuItemId
                    ? bowlAdminPrices[ingName]
                    : latteAddOnPrices[ingName];
                dbIng.PriceCharged.Should().Be(expected,
                    $"DB PriceCharged for '{ingName}' should match admin price");
            }
        }
        dbOrder.Subtotal.Should().Be(dbOrder.Items.Sum(i => i.TotalPrice),
            "DB subtotal should equal sum of item totals");

        // ── Phase 7: Stripe amount ──

        ((long)(dbOrder.Total * 100)).Should().Be((long)(expectedTotal * 100));

        // ── Phase 8: Receipt PDF ──

        var receiptText = GenerateReceiptText(dbOrder);

        // Both item totals on the receipt
        receiptText.Should().Contain($"${bowlUnitPrice:F2}", "receipt should show bowl total");
        receiptText.Should().Contain($"${latteUnitPrice:F2}", "receipt should show latte total");

        // Each bowl ingredient price
        foreach (var name in bowlIngredients)
            receiptText.Should().Contain($"${bowlAdminPrices[name]:F2}",
                $"receipt should show bowl ingredient '{name}' price");

        // Latte add-on price
        receiptText.Should().Contain($"${latteAddOnPrices["Espresso"]:F2}",
            "receipt should show espresso add-on price");

        VerifyReceiptOrderTotals(receiptText, dbOrder);

        // ── Phase 9: Confirmation page ──

        await page.WaitForURLAsync("**/order/**", new() { Timeout = 30_000 });
        await page.WaitForSelectorAsync("text=Order Details");

        var confirmData = await ExtractPageTotals(page);
        VerifyTotalsMatch(confirmData, dbOrder, "confirmation page");

        VerifyTaxMath(dbOrder);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Shared helpers
    // ═══════════════════════════════════════════════════════════════

    private static MannaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MannaDbContext>()
            .UseNpgsql(E2EFixture.TestConnectionString)
            .Options;
        return new MannaDbContext(options);
    }

    private static async Task<Order> LoadOrderWithIncludes(MannaDbContext db, Guid orderId) =>
        await db.Orders
            .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
            .Include(o => o.Items).ThenInclude(oi => oi.Variant)
            .Include(o => o.Items).ThenInclude(oi => oi.Ingredients)
                .ThenInclude(oii => oii.Ingredient)
            .FirstAsync(o => o.Id == orderId);

    private static async Task OpenCartDrawer(IPage page)
    {
        await page.Locator("header button").Last.ClickAsync();
        await page.WaitForSelectorAsync("text=Your Order");
    }

    private static async Task<decimal> ExtractFooterTotal(IPage page)
    {
        var text = await page.EvaluateAsync<string>(@"() => {
            const footer = document.querySelector('.sticky.bottom-0');
            if (!footer) return '0';
            const el = footer.querySelector('.text-2xl.font-bold');
            if (!el) return '0';
            return el.textContent.trim().replace('$', '');
        }");
        return decimal.Parse(text!);
    }

    // Parsed totals record
    private record TotalsData(decimal Subtotal, decimal Tax, decimal Total);

    private static async Task<TotalsData> ExtractCartTotals(IPage page) =>
        await ExtractTotalsFromSelector(page, @"
            const sheet = document.querySelector('[data-slot=""sheet-content""]');
            if (!sheet) return { subtotal: '0', tax: '0', total: '0' };
            const footer = sheet.querySelector('.border-t');
            if (!footer) return { subtotal: '0', tax: '0', total: '0' };
            const rows = footer.querySelectorAll('.flex.justify-between');
        ");

    private static async Task<TotalsData> ExtractPageTotals(IPage page) =>
        await ExtractTotalsFromSelector(page, @"
            const rows = document.querySelectorAll('.flex.justify-between');
        ");

    private static async Task<TotalsData> ExtractTotalsFromSelector(IPage page, string rowSelector)
    {
        var json = await page.EvaluateAsync<JsonElement>($@"() => {{
            {rowSelector}
            const result = {{ subtotal: '0', tax: '0', total: '0' }};
            for (const row of rows) {{
                const text = row.textContent || '';
                const match = text.match(/\$(\d+\.\d{{2}})/);
                if (!match) continue;
                if (text.includes('Subtotal')) result.subtotal = match[1];
                else if (text.includes('Tax')) result.tax = match[1];
                else if (text.includes('Total') && !text.includes('Subtotal')) result.total = match[1];
            }}
            return result;
        }}");

        return new TotalsData(
            decimal.Parse(json.GetProperty("subtotal").GetString()!),
            decimal.Parse(json.GetProperty("tax").GetString()!),
            decimal.Parse(json.GetProperty("total").GetString()!));
    }

    // Parsed order from API response
    private record ApiOrderData(
        decimal Subtotal, decimal Tax, decimal Total, decimal TaxRate,
        List<ApiOrderItem> Items);

    private record ApiOrderItem(
        decimal UnitPrice, decimal TotalPrice, int Quantity,
        List<ApiOrderIngredient> Ingredients);

    private record ApiOrderIngredient(string Name, decimal PriceCharged);

    private static async Task<(Guid OrderId, ApiOrderData Order)> PlaceInStoreOrderAndCapture(IPage page)
    {
        var responseTask = page.WaitForResponseAsync(r =>
            r.Url.Contains("/api/orders") && r.Request.Method == "POST" &&
            !r.Url.Contains("confirm-payment"));

        await page.GetByRole(AriaRole.Button, new() { Name = "Pay In-Store" }).ClickAsync();

        var apiResponse = await responseTask;
        var orderJson = (await apiResponse.JsonAsync())!.Value.GetProperty("order");

        var orderId = Guid.Parse(orderJson.GetProperty("id").GetString()!);

        var items = new List<ApiOrderItem>();
        foreach (var itemJson in orderJson.GetProperty("items").EnumerateArray())
        {
            var ingredients = new List<ApiOrderIngredient>();
            var ingProp = itemJson.GetProperty("ingredients");
            if (ingProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var ingJson in ingProp.EnumerateArray())
                {
                    ingredients.Add(new ApiOrderIngredient(
                        ingJson.GetProperty("ingredientName").GetString()!,
                        ingJson.GetProperty("priceCharged").GetDecimal()));
                }
            }

            items.Add(new ApiOrderItem(
                itemJson.GetProperty("unitPrice").GetDecimal(),
                itemJson.GetProperty("totalPrice").GetDecimal(),
                itemJson.GetProperty("quantity").GetInt32(),
                ingredients));
        }

        var order = new ApiOrderData(
            orderJson.GetProperty("subtotal").GetDecimal(),
            orderJson.GetProperty("tax").GetDecimal(),
            orderJson.GetProperty("total").GetDecimal(),
            orderJson.GetProperty("taxRate").GetDecimal(),
            items);

        return (orderId, order);
    }

    private static string GenerateReceiptText(Order order)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var receipt = new ReceiptDocument(order, new Dictionary<string, string>());
        var pdfBytes = receipt.GeneratePdf();

        using var pdfDoc = PdfDocument.Open(pdfBytes);
        return string.Join(" ", pdfDoc.GetPages().Select(p => p.Text));
    }

    private static void VerifyReceiptOrderTotals(string receiptText, Order dbOrder)
    {
        receiptText.Should().Contain($"${dbOrder.Subtotal:F2}", "receipt subtotal");
        receiptText.Should().Contain($"${dbOrder.Tax:F2}", "receipt tax");
        receiptText.Should().Contain($"${dbOrder.Total:F2}", "receipt total");
    }

    private static void VerifyTotalsMatch(TotalsData actual, Order expected, string source)
    {
        actual.Subtotal.Should().Be(expected.Subtotal, $"{source} subtotal");
        actual.Tax.Should().Be(expected.Tax, $"{source} tax");
        actual.Total.Should().Be(expected.Total, $"{source} total");
    }

    private static void VerifyTaxMath(Order order)
    {
        order.TaxRate.Should().Be(0.0825m, "tax rate should be 8.25%");
        order.Tax.Should().Be(Math.Round(order.Subtotal * order.TaxRate, 2),
            "tax = round(subtotal × rate, 2)");
        order.Total.Should().Be(order.Subtotal + order.Tax,
            "total = subtotal + tax");
        order.Subtotal.Should().Be(order.Items.Sum(i => i.TotalPrice),
            "subtotal = sum of item totals");
    }
}
