using FluentAssertions;
using MannaHp.E2E.Tests.Fixtures;
using Microsoft.Playwright;

namespace MannaHp.E2E.Tests;

[TestFixture]
public class CheckoutTests
{
    // ── Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Navigates to the menu, clicks into the Traditional Drinks category,
    /// opens the Latte item, selects a size, and adds it to the cart.
    /// Returns the page with an item in the cart ready for checkout.
    /// </summary>
    private static async Task<IPage> AddLatteToCartAsync()
    {
        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);

        // Wait for menu categories to load
        await page.WaitForSelectorAsync("text=Traditional Drinks");

        // Click into Traditional Drinks category
        await page.GetByText("Traditional Drinks").ClickAsync();

        // Wait for items to load and click on Latte
        await page.WaitForSelectorAsync("text=Latte");
        await page.GetByText("Latte").First.ClickAsync();

        // Wait for the item detail page to load, pick a variant (size)
        await page.WaitForSelectorAsync("text=12oz");
        await page.GetByText("12oz").ClickAsync();

        // Click "Add to Cart"
        await page.GetByRole(AriaRole.Button, new() { Name = "Add to Cart" }).ClickAsync();

        return page;
    }

    /// <summary>
    /// Opens the cart drawer by clicking the cart button in the header.
    /// </summary>
    private static async Task OpenCartAsync(IPage page)
    {
        // The cart button in the header — look for the shopping cart icon button
        await page.GetByRole(AriaRole.Button, new() { Name = "cart" })
            .Or(page.Locator("header button").Last)
            .ClickAsync();

        // Wait for the cart drawer to appear
        await page.WaitForSelectorAsync("text=Your Order");
    }

    // ── Tests ───────────────────────────────────────────────────────

    [Test]
    public async Task CartDrawer_ShowsPayWithCardAndPayInStore_Buttons()
    {
        var page = await AddLatteToCartAsync();
        await OpenCartAsync(page);

        // Both payment buttons should be visible
        var cardButton = page.GetByRole(AriaRole.Button, new() { Name = "Pay with Card" });
        var inStoreButton = page.GetByRole(AriaRole.Button, new() { Name = "Pay In-Store" });

        await Expect(cardButton).ToBeVisibleAsync();
        await Expect(inStoreButton).ToBeVisibleAsync();
    }

    [Test]
    public async Task CartDrawer_ShowsItemWithCorrectPrice()
    {
        var page = await AddLatteToCartAsync();
        await OpenCartAsync(page);

        // Latte should appear in the cart
        var cartContent = await page.Locator("[data-slot='sheet-content']").TextContentAsync();
        cartContent.Should().Contain("Latte");

        // Price should be visible (Latte 12oz = $4.75)
        cartContent.Should().Contain("4.75");
    }

    [Test]
    public async Task PayWithCard_NavigatesToCheckoutPage()
    {
        var page = await AddLatteToCartAsync();
        await OpenCartAsync(page);

        // Click "Pay with Card"
        await page.GetByRole(AriaRole.Button, new() { Name = "Pay with Card" }).ClickAsync();

        // Should navigate to /checkout
        await page.WaitForURLAsync("**/checkout");
        page.Url.Should().Contain("/checkout");
    }

    [Test]
    public async Task CheckoutPage_ShowsOrderSummary()
    {
        var page = await AddLatteToCartAsync();
        await OpenCartAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Pay with Card" }).ClickAsync();
        await page.WaitForURLAsync("**/checkout");

        // Checkout page should show order summary
        await page.WaitForSelectorAsync("text=Order Summary");
        await page.WaitForSelectorAsync("text=Checkout");

        var content = await page.TextContentAsync("body");
        content.Should().Contain("Order Summary");
        content.Should().Contain("Latte");
        content.Should().Contain("4.75");
        content.Should().Contain("Subtotal");
        content.Should().Contain("Tax");
        content.Should().Contain("Total");
    }

    [Test]
    public async Task CheckoutPage_ShowsPaymentDetailsSection()
    {
        var page = await AddLatteToCartAsync();
        await OpenCartAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Pay with Card" }).ClickAsync();
        await page.WaitForURLAsync("**/checkout");

        // Should show the Payment Details section
        await page.WaitForSelectorAsync("text=Payment Details");
        var content = await page.TextContentAsync("body");
        content.Should().Contain("Payment Details");
        content.Should().Contain("Payments processed securely by Stripe");
    }

    [Test]
    public async Task CheckoutPage_ShowsBackToCartButton()
    {
        var page = await AddLatteToCartAsync();
        await OpenCartAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Pay with Card" }).ClickAsync();
        await page.WaitForURLAsync("**/checkout");

        var backButton = page.GetByRole(AriaRole.Button, new() { Name = "Back to cart" });
        await Expect(backButton).ToBeVisibleAsync();
    }

    [Test]
    public async Task PayInStore_PlacesOrderAndShowsConfirmation()
    {
        var page = await AddLatteToCartAsync();
        await OpenCartAsync(page);

        // Click "Pay In-Store"
        await page.GetByRole(AriaRole.Button, new() { Name = "Pay In-Store" }).ClickAsync();

        // Should show celebration video or navigate to order confirmation
        // The video overlay appears first, then navigates to /order/{id}
        // Wait for either the video or the order confirmation page
        await page.WaitForURLAsync("**/order/**", new() { Timeout = 30_000 });

        // Should be on order confirmation page
        page.Url.Should().MatchRegex(@"/order/[0-9a-f-]+");

        // Confirmation page content
        var content = await page.TextContentAsync("body");
        content.Should().Contain("Order Placed");
        content.Should().Contain("Order Details");
        content.Should().Contain("Latte");
    }

    [Test]
    public async Task PayInStore_RapidDoubleClick_OnlyCreatesOneOrder()
    {
        var page = await AddLatteToCartAsync();
        await OpenCartAsync(page);

        // Set up a request counter for POST /api/orders
        var orderPostCount = 0;
        page.Request += (_, request) =>
        {
            if (request.Method == "POST" && request.Url.Contains("/api/orders") &&
                !request.Url.Contains("confirm-payment"))
            {
                Interlocked.Increment(ref orderPostCount);
            }
        };

        var button = page.GetByRole(AriaRole.Button, new() { Name = "Pay In-Store" });

        // Click twice rapidly
        await button.ClickAsync(new() { Delay = 0 });
        await button.ClickAsync(new() { Delay = 0 });

        // Wait for navigation (order confirmation or video)
        await page.WaitForURLAsync("**/order/**", new() { Timeout = 30_000 });

        // Only one POST to /api/orders should have been made
        orderPostCount.Should().Be(1);
    }

    [Test]
    public async Task CheckoutPage_EmptyCart_RedirectsToHome()
    {
        var page = await E2EFixture.CreatePageAsync();

        // Navigate directly to /checkout without any items in cart
        await page.GotoAsync($"{E2EFixture.NextBaseUrl}/checkout");

        // Should redirect back to the home page
        await page.WaitForURLAsync(E2EFixture.NextBaseUrl + "/", new() { Timeout = 10_000 });
        page.Url.Should().NotContain("/checkout");
    }

    [Test]
    public async Task OrderConfirmation_ShowsOrderAgainButton()
    {
        var page = await AddLatteToCartAsync();
        await OpenCartAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Pay In-Store" }).ClickAsync();
        await page.WaitForURLAsync("**/order/**", new() { Timeout = 30_000 });

        // "Order Again" button should be visible
        var orderAgainButton = page.GetByRole(AriaRole.Button, new() { Name = "Order Again" });
        await Expect(orderAgainButton).ToBeVisibleAsync();

        // Clicking it should navigate back to home
        await orderAgainButton.ClickAsync();
        await page.WaitForURLAsync(E2EFixture.NextBaseUrl + "/", new() { Timeout = 10_000 });
    }

    // ── Playwright Expect helper ──

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
