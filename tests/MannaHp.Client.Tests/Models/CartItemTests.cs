using FluentAssertions;
using MannaHp.Client.Models;
using MannaHp.Shared.DTOs;

namespace MannaHp.Client.Tests.Models;

public class CartItemTests
{
    // ── helpers ──────────────────────────────────────────────────────

    private static MenuItemDto CustomizableMenuItem(string name = "Burrito Bowl") =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, null, ImageUrl: null, ImageApproximate: false, IsCustomizable: true, Active: true, SortOrder: 0,
            Variants: [], AvailableIngredients: []);

    private static MenuItemDto FixedMenuItem(string name = "Latte") =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, null, ImageUrl: null, ImageApproximate: false, IsCustomizable: false, Active: true, SortOrder: 0,
            Variants: [], AvailableIngredients: null);

    private static MenuItemVariantDto Variant(string name = "12oz", decimal price = 4.75m) =>
        new(Guid.NewGuid(), name, price, SortOrder: 0, Active: true);

    private static AvailableIngredientDto Ingredient(string name = "Chicken", decimal price = 3.00m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, price, QuantityUsed: 8m,
            IsDefault: false, GroupName: "Proteins", SortOrder: 0, Active: true, IngredientUnit: 0);

    // ── UnitPrice ───────────────────────────────────────────────────

    [Fact]
    public void UnitPrice_CustomizableItem_ReturnsSumOfIngredientPrices()
    {
        var item = new CartItem
        {
            MenuItem = CustomizableMenuItem(),
            SelectedIngredients = [Ingredient("Chicken", 3.00m), Ingredient("Rice", 1.50m), Ingredient("Salsa", 0.50m)]
        };

        item.UnitPrice.Should().Be(5.00m);
    }

    [Fact]
    public void UnitPrice_CustomizableItem_NoIngredients_ReturnsZero()
    {
        var item = new CartItem
        {
            MenuItem = CustomizableMenuItem(),
            SelectedIngredients = []
        };

        item.UnitPrice.Should().Be(0m);
    }

    [Fact]
    public void UnitPrice_CustomizableItem_NullIngredients_ReturnsZero()
    {
        var item = new CartItem
        {
            MenuItem = CustomizableMenuItem(),
            SelectedIngredients = null
        };

        item.UnitPrice.Should().Be(0m);
    }

    [Fact]
    public void UnitPrice_FixedItem_ReturnsVariantPrice()
    {
        var item = new CartItem
        {
            MenuItem = FixedMenuItem(),
            Variant = Variant("16oz", 5.25m)
        };

        item.UnitPrice.Should().Be(5.25m);
    }

    [Fact]
    public void UnitPrice_FixedItem_NullVariant_ReturnsZero()
    {
        var item = new CartItem
        {
            MenuItem = FixedMenuItem(),
            Variant = null
        };

        item.UnitPrice.Should().Be(0m);
    }

    // ── LineTotal ────────────────────────────────────────────────────

    [Fact]
    public void LineTotal_SingleQuantity_EqualsUnitPrice()
    {
        var item = new CartItem
        {
            MenuItem = FixedMenuItem(),
            Variant = Variant("12oz", 4.75m),
            Quantity = 1
        };

        item.LineTotal.Should().Be(4.75m);
    }

    [Fact]
    public void LineTotal_MultipleQuantity_MultipliesCorrectly()
    {
        var item = new CartItem
        {
            MenuItem = FixedMenuItem(),
            Variant = Variant("12oz", 4.75m),
            Quantity = 3
        };

        item.LineTotal.Should().Be(14.25m);
    }

    [Fact]
    public void LineTotal_CustomizableItem_MultipliesIngredientSumByQuantity()
    {
        var item = new CartItem
        {
            MenuItem = CustomizableMenuItem(),
            SelectedIngredients = [Ingredient("Chicken", 3.00m), Ingredient("Rice", 2.00m)],
            Quantity = 2
        };

        item.LineTotal.Should().Be(10.00m);
    }

    // ── DisplayName ─────────────────────────────────────────────────

    [Fact]
    public void DisplayName_CustomizableItem_ReturnsMenuItemName()
    {
        var item = new CartItem
        {
            MenuItem = CustomizableMenuItem("Burrito Bowl"),
            Variant = Variant("Large", 10.00m) // variant should be ignored for customizable
        };

        item.DisplayName.Should().Be("Burrito Bowl");
    }

    [Fact]
    public void DisplayName_FixedItemWithVariant_ReturnsNameWithVariant()
    {
        var item = new CartItem
        {
            MenuItem = FixedMenuItem("Latte"),
            Variant = Variant("16oz", 5.25m)
        };

        item.DisplayName.Should().Be("Latte (16oz)");
    }

    [Fact]
    public void DisplayName_FixedItemWithoutVariant_ReturnsMenuItemName()
    {
        var item = new CartItem
        {
            MenuItem = FixedMenuItem("Espresso"),
            Variant = null
        };

        item.DisplayName.Should().Be("Espresso");
    }
}
