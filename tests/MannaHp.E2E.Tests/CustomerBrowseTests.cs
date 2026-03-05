using FluentAssertions;
using MannaHp.E2E.Tests.Fixtures;
using Microsoft.Playwright;

namespace MannaHp.E2E.Tests;

[TestFixture]
public class CustomerBrowseTests
{
    // ── Helpers ─────────────────────────────────────────────────────

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    // ── Tests ───────────────────────────────────────────────────────

    [Test]
    public async Task HomePage_ShowsAllCategories()
    {
        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);

        // Wait for categories to load
        await page.WaitForSelectorAsync("text=Traditional Drinks");

        var body = await page.TextContentAsync("body");
        body.Should().Contain("Bowls");
        body.Should().Contain("Traditional Drinks");
        body.Should().Contain("Seasonal Specials");
        body.Should().Contain("Sides & Drinks");
    }

    [Test]
    public async Task ClickCategory_ShowsItemsInCategory()
    {
        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);

        await page.WaitForSelectorAsync("text=Traditional Drinks");
        await page.GetByText("Traditional Drinks").ClickAsync();

        // Should show drink items
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var body = await page.TextContentAsync("body");
        body.Should().Contain("Latte");
        body.Should().Contain("Drip Coffee");
    }

    [Test]
    public async Task ClickItem_ShowsDetailPageWithPrice()
    {
        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);

        await page.WaitForSelectorAsync("text=Traditional Drinks");
        await page.GetByText("Traditional Drinks").ClickAsync();

        await page.WaitForSelectorAsync("text=Latte");
        await page.GetByText("Latte").First.ClickAsync();

        // Should be on item detail page
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var body = await page.TextContentAsync("body");
        // Latte should show variant prices
        body.Should().Contain("Latte");
        // Should show at least one variant price (12oz = $4.75 or 16oz = $5.25)
        body.Should().MatchRegex(@"\$[0-9]+\.[0-9]{2}");
    }

    [Test]
    public async Task BowlBuilder_ShowsRunningTotal()
    {
        var page = await E2EFixture.CreatePageAsync();
        await page.GotoAsync(E2EFixture.NextBaseUrl);

        // Navigate to Bowls category
        await page.WaitForSelectorAsync("text=Bowls");
        await page.GetByText("Bowls").ClickAsync();

        // Click on Burrito Bowl
        await page.WaitForSelectorAsync("text=Burrito Bowl");
        await page.GetByText("Burrito Bowl").First.ClickAsync();

        // Should be on the bowl builder page
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var body = await page.TextContentAsync("body");
        body.Should().Contain("Burrito Bowl");

        // Bowl builder should show ingredient groups
        var hasIngredientGroup = body!.Contains("Bases") || body.Contains("Proteins") || body.Contains("Jasmine Rice");
        hasIngredientGroup.Should().BeTrue("bowl builder should show ingredient groups");

        // Click an ingredient to add it (e.g., Jasmine Rice)
        var riceButton = page.GetByText("Jasmine Rice").First;
        if (await riceButton.IsVisibleAsync())
        {
            await riceButton.ClickAsync();
            await page.WaitForTimeoutAsync(300); // brief wait for state update

            // Running total should now show $3.00
            var updatedBody = await page.TextContentAsync("body");
            updatedBody.Should().Contain("3.00");
        }
    }
}
