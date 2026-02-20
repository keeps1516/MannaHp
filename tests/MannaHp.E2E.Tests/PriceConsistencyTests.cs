using System.Text.Json;
using FluentAssertions;
using MannaHp.E2E.Tests.Fixtures;
using MannaHp.PrintAgent;
using MannaHp.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace MannaHp.E2E.Tests;

[TestFixture]
public class PriceConsistencyTests
{
    /// <summary>
    /// Verifies that the exact same total flows consistently through every layer:
    ///   1. Cart UI (what the customer sees)
    ///   2. API response (what the server calculates)
    ///   3. Stripe amount (what would be charged — order.Total * 100 in cents)
    ///   4. Database (what is persisted)
    ///   5. Order confirmation page (what the customer sees after placing the order)
    ///   6. Receipt PDF (what gets printed)
    /// </summary>
    [Test]
    public async Task Total_IsConsistent_AcrossCart_Api_Stripe_Database_Confirmation_AndReceipt()
    {
        // ── 1. Add a Latte (12oz) to the cart ──────────────────────────

        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);

        await page.WaitForSelectorAsync("text=Traditional Drinks");
        await page.GetByText("Traditional Drinks").ClickAsync();

        await page.WaitForSelectorAsync("text=Latte");
        await page.GetByText("Latte").First.ClickAsync();

        await page.WaitForSelectorAsync("text=12oz");
        await page.GetByText("12oz").ClickAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Add to Cart" }).ClickAsync();

        // ── 2. Open cart drawer and extract displayed totals ───────────

        await page.GetByRole(AriaRole.Button, new() { Name = "cart" })
            .Or(page.Locator("header button").Last)
            .ClickAsync();
        await page.WaitForSelectorAsync("text=Your Order");

        // Extract subtotal, tax, total from the cart drawer footer via JS
        var cartTotals = await page.EvaluateAsync<JsonElement>(@"() => {
            const sheet = document.querySelector('[data-slot=""sheet-content""]');
            if (!sheet) return { subtotal: '0', tax: '0', total: '0' };
            const footer = sheet.querySelector('.border-t');
            if (!footer) return { subtotal: '0', tax: '0', total: '0' };
            const rows = footer.querySelectorAll('.flex.justify-between');
            const result = { subtotal: '0', tax: '0', total: '0' };
            for (const row of rows) {
                const text = row.textContent || '';
                const match = text.match(/\$(\d+\.\d{2})/);
                if (!match) continue;
                if (text.includes('Subtotal')) result.subtotal = match[1];
                else if (text.includes('Tax')) result.tax = match[1];
                else if (text.includes('Total') && !text.includes('Subtotal')) result.total = match[1];
            }
            return result;
        }");

        var cartSubtotal = decimal.Parse(cartTotals.GetProperty("subtotal").GetString()!);
        var cartTax = decimal.Parse(cartTotals.GetProperty("tax").GetString()!);
        var cartTotal = decimal.Parse(cartTotals.GetProperty("total").GetString()!);

        // Sanity: cart should have non-zero values
        cartSubtotal.Should().BeGreaterThan(0, "cart should have a subtotal");
        cartTotal.Should().BeGreaterThan(cartSubtotal, "total should include tax");

        // ── 3. Intercept the POST /api/orders response, then place order ─

        var responseTask = page.WaitForResponseAsync(r =>
            r.Url.Contains("/api/orders") && r.Request.Method == "POST" &&
            !r.Url.Contains("confirm-payment"));

        await page.GetByRole(AriaRole.Button, new() { Name = "Pay In-Store" }).ClickAsync();

        var apiResponse = await responseTask;
        var responseJson = (await apiResponse.JsonAsync())!.Value;
        var orderJson = responseJson.GetProperty("order");

        var apiSubtotal = orderJson.GetProperty("subtotal").GetDecimal();
        var apiTaxRate = orderJson.GetProperty("taxRate").GetDecimal();
        var apiTax = orderJson.GetProperty("tax").GetDecimal();
        var apiTotal = orderJson.GetProperty("total").GetDecimal();
        var orderId = Guid.Parse(orderJson.GetProperty("id").GetString()!);

        // ── 4. Calculate what Stripe would receive ─────────────────────
        // StripeService.CreatePaymentIntentAsync uses: (long)(amount * 100)
        var expectedStripeCents = (long)(apiTotal * 100);

        // ── 5. Query the database directly ─────────────────────────────

        var dbOptions = new DbContextOptionsBuilder<MannaDbContext>()
            .UseNpgsql(E2EFixture.TestConnectionString)
            .Options;
        await using var db = new MannaDbContext(dbOptions);

        var dbOrder = await db.Orders
            .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
            .Include(o => o.Items).ThenInclude(oi => oi.Variant)
            .Include(o => o.Items).ThenInclude(oi => oi.Ingredients)
                .ThenInclude(oii => oii.Ingredient)
            .FirstAsync(o => o.Id == orderId);

        // ── 6. Generate receipt PDF and extract text ───────────────────

        QuestPDF.Settings.License = LicenseType.Community;
        var receipt = new ReceiptDocument(dbOrder, new Dictionary<string, string>());
        var pdfBytes = receipt.GeneratePdf();

        using var pdfDoc = PdfDocument.Open(pdfBytes);
        var receiptText = string.Join(" ", pdfDoc.GetPages().Select(p => p.Text));

        // ── 7. Wait for order confirmation page and read its totals ────

        await page.WaitForURLAsync("**/order/**", new() { Timeout = 30_000 });
        await page.WaitForSelectorAsync("text=Order Details");

        var confirmTotals = await page.EvaluateAsync<JsonElement>(@"() => {
            const rows = document.querySelectorAll('.flex.justify-between');
            const result = { subtotal: '0', tax: '0', total: '0' };
            for (const row of rows) {
                const text = row.textContent || '';
                const match = text.match(/\$(\d+\.\d{2})/);
                if (!match) continue;
                if (text.includes('Subtotal')) result.subtotal = match[1];
                else if (text.includes('Tax')) result.tax = match[1];
                else if (text.includes('Total') && !text.includes('Subtotal')) result.total = match[1];
            }
            return result;
        }");

        var confirmSubtotal = decimal.Parse(confirmTotals.GetProperty("subtotal").GetString()!);
        var confirmTax = decimal.Parse(confirmTotals.GetProperty("tax").GetString()!);
        var confirmTotal = decimal.Parse(confirmTotals.GetProperty("total").GetString()!);

        // ══════════════════════════════════════════════════════════════
        //  ASSERTIONS — every layer must agree on subtotal, tax, total
        // ══════════════════════════════════════════════════════════════

        // ── Cart UI ↔ API response ──
        cartSubtotal.Should().Be(apiSubtotal, "cart UI subtotal should match API response");
        cartTax.Should().Be(apiTax, "cart UI tax should match API response");
        cartTotal.Should().Be(apiTotal, "cart UI total should match API response");

        // ── API response ↔ Database ──
        apiSubtotal.Should().Be(dbOrder.Subtotal, "API subtotal should match database");
        apiTax.Should().Be(dbOrder.Tax, "API tax should match database");
        apiTotal.Should().Be(dbOrder.Total, "API total should match database");
        apiTaxRate.Should().Be(dbOrder.TaxRate, "API tax rate should match database");

        // ── Database ↔ Stripe amount (cents) ──
        expectedStripeCents.Should().Be((long)(dbOrder.Total * 100),
            "Stripe amount in cents should equal DB total × 100");

        // ── Database ↔ Order confirmation page ──
        dbOrder.Subtotal.Should().Be(confirmSubtotal, "DB subtotal should match confirmation page");
        dbOrder.Tax.Should().Be(confirmTax, "DB tax should match confirmation page");
        dbOrder.Total.Should().Be(confirmTotal, "DB total should match confirmation page");

        // ── Database ↔ Receipt PDF ──
        receiptText.Should().Contain($"${dbOrder.Subtotal:F2}",
            "receipt PDF should show correct subtotal");
        receiptText.Should().Contain($"${dbOrder.Tax:F2}",
            "receipt PDF should show correct tax");
        receiptText.Should().Contain($"${dbOrder.Total:F2}",
            "receipt PDF should show correct total");

        // ── Tax rate consistency ──
        dbOrder.TaxRate.Should().Be(0.0825m, "tax rate should be 8.25%");
        dbOrder.Tax.Should().Be(Math.Round(dbOrder.Subtotal * dbOrder.TaxRate, 2),
            "tax should equal subtotal × tax rate, rounded to 2 decimals");
        dbOrder.Total.Should().Be(dbOrder.Subtotal + dbOrder.Tax,
            "total should equal subtotal + tax");

        // ── Item-level price integrity ──
        dbOrder.Items.Should().HaveCount(1);
        var item = dbOrder.Items.First();
        item.Quantity.Should().Be(1);
        item.TotalPrice.Should().Be(item.UnitPrice * item.Quantity,
            "item total should equal unit price × quantity");
        dbOrder.Subtotal.Should().Be(dbOrder.Items.Sum(i => i.TotalPrice),
            "order subtotal should equal sum of item totals");
    }

    // ── Playwright Expect helper ──

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
