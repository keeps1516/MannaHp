using FluentAssertions;
using MannaHp.Client.Models;
using MannaHp.Client.Services;
using MannaHp.Shared.DTOs;

namespace MannaHp.Client.Tests.Services;

public class CartServiceTests
{
    private readonly CartService _cart = new();

    // ── helpers ──────────────────────────────────────────────────────

    private static CartItem FixedItem(decimal price = 4.75m, int qty = 1) => new()
    {
        MenuItem = new MenuItemDto(Guid.NewGuid(), Guid.NewGuid(), "Latte", null,
            ImageUrl: null, ImageApproximate: false, IsCustomizable: false, Active: true, SortOrder: 0, Variants: [], AvailableIngredients: null),
        Variant = new MenuItemVariantDto(Guid.NewGuid(), "12oz", price, SortOrder: 0, Active: true),
        Quantity = qty
    };

    private static CartItem CustomizableItem(decimal totalIngredientPrice = 7.50m, int qty = 1) => new()
    {
        MenuItem = new MenuItemDto(Guid.NewGuid(), Guid.NewGuid(), "Burrito Bowl", null,
            ImageUrl: null, ImageApproximate: false, IsCustomizable: true, Active: true, SortOrder: 0, Variants: [], AvailableIngredients: []),
        SelectedIngredients =
        [
            new AvailableIngredientDto(Guid.NewGuid(), Guid.NewGuid(), "Chicken",
                totalIngredientPrice, QuantityUsed: 8m, IsDefault: false, GroupName: "Proteins", SortOrder: 0, Active: true, IngredientUnit: 0)
        ],
        Quantity = qty
    };

    // ── AddItem ─────────────────────────────────────────────────────

    [Fact]
    public void AddItem_AddsToItems()
    {
        var item = FixedItem();
        _cart.AddItem(item);

        _cart.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    public void AddItem_MultipleCalls_AddsMultipleItems()
    {
        _cart.AddItem(FixedItem());
        _cart.AddItem(FixedItem());

        _cart.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddItem_FiresOnChange()
    {
        var fired = false;
        _cart.OnChange += () => fired = true;

        _cart.AddItem(FixedItem());

        fired.Should().BeTrue();
    }

    // ── RemoveItem ──────────────────────────────────────────────────

    [Fact]
    public void RemoveItem_ExistingItem_RemovesFromCart()
    {
        var item = FixedItem();
        _cart.AddItem(item);

        _cart.RemoveItem(item.Id);

        _cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_NonexistentId_DoesNotThrow()
    {
        _cart.AddItem(FixedItem());

        var act = () => _cart.RemoveItem(Guid.NewGuid());

        act.Should().NotThrow();
        _cart.Items.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveItem_FiresOnChange()
    {
        var item = FixedItem();
        _cart.AddItem(item);

        var fired = false;
        _cart.OnChange += () => fired = true;

        _cart.RemoveItem(item.Id);

        fired.Should().BeTrue();
    }

    [Fact]
    public void RemoveItem_NonexistentId_DoesNotFireOnChange()
    {
        var fired = false;
        _cart.OnChange += () => fired = true;

        _cart.RemoveItem(Guid.NewGuid());

        fired.Should().BeFalse();
    }

    // ── UpdateQuantity ──────────────────────────────────────────────

    [Fact]
    public void UpdateQuantity_SetsNewQuantity()
    {
        var item = FixedItem();
        _cart.AddItem(item);

        _cart.UpdateQuantity(item.Id, 5);

        _cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateQuantity_ZeroQuantity_RemovesItem()
    {
        var item = FixedItem();
        _cart.AddItem(item);

        _cart.UpdateQuantity(item.Id, 0);

        _cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void UpdateQuantity_NegativeQuantity_RemovesItem()
    {
        var item = FixedItem();
        _cart.AddItem(item);

        _cart.UpdateQuantity(item.Id, -1);

        _cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void UpdateQuantity_FiresOnChange()
    {
        var item = FixedItem();
        _cart.AddItem(item);

        var fired = false;
        _cart.OnChange += () => fired = true;

        _cart.UpdateQuantity(item.Id, 3);

        fired.Should().BeTrue();
    }

    [Fact]
    public void UpdateQuantity_NonexistentId_DoesNotFireOnChange()
    {
        var fired = false;
        _cart.OnChange += () => fired = true;

        _cart.UpdateQuantity(Guid.NewGuid(), 5);

        fired.Should().BeFalse();
    }

    // ── Clear ───────────────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllItems()
    {
        _cart.AddItem(FixedItem());
        _cart.AddItem(FixedItem());
        _cart.AddItem(CustomizableItem());

        _cart.Clear();

        _cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clear_FiresOnChange()
    {
        _cart.AddItem(FixedItem());

        var fired = false;
        _cart.OnChange += () => fired = true;

        _cart.Clear();

        fired.Should().BeTrue();
    }

    // ── ItemCount ───────────────────────────────────────────────────

    [Fact]
    public void ItemCount_EmptyCart_ReturnsZero()
    {
        _cart.ItemCount.Should().Be(0);
    }

    [Fact]
    public void ItemCount_SumsQuantitiesAcrossItems()
    {
        _cart.AddItem(FixedItem(qty: 2));
        _cart.AddItem(FixedItem(qty: 3));

        _cart.ItemCount.Should().Be(5);
    }

    // ── Subtotal ────────────────────────────────────────────────────

    [Fact]
    public void Subtotal_EmptyCart_ReturnsZero()
    {
        _cart.Subtotal.Should().Be(0m);
    }

    [Fact]
    public void Subtotal_SingleFixedItem_ReturnsLineTotal()
    {
        _cart.AddItem(FixedItem(price: 4.75m, qty: 2));

        _cart.Subtotal.Should().Be(9.50m);
    }

    [Fact]
    public void Subtotal_MixedItems_ReturnsCombinedTotal()
    {
        _cart.AddItem(FixedItem(price: 4.75m, qty: 1));       // 4.75
        _cart.AddItem(CustomizableItem(totalIngredientPrice: 7.50m, qty: 1)); // 7.50

        _cart.Subtotal.Should().Be(12.25m);
    }

    // ── Tax ─────────────────────────────────────────────────────────

    [Fact]
    public void Tax_EmptyCart_ReturnsZero()
    {
        _cart.Tax.Should().Be(0m);
    }

    [Fact]
    public void Tax_CalculatesAt8Point25Percent_Rounded()
    {
        // Subtotal = 10.00 → Tax = 10.00 * 0.0825 = 0.825 → Math.Round uses banker's rounding → 0.82
        _cart.AddItem(FixedItem(price: 10.00m, qty: 1));

        _cart.Tax.Should().Be(0.82m);
    }

    [Fact]
    public void Tax_RoundsToTwoDecimalPlaces()
    {
        // Subtotal = 13.75 → Tax = 13.75 * 0.0825 = 1.134375 → rounded to 1.13
        _cart.AddItem(FixedItem(price: 13.75m, qty: 1));

        _cart.Tax.Should().Be(1.13m);
    }

    // ── Total ───────────────────────────────────────────────────────

    [Fact]
    public void Total_EmptyCart_ReturnsZero()
    {
        _cart.Total.Should().Be(0m);
    }

    [Fact]
    public void Total_IsSubtotalPlusTax()
    {
        // Subtotal = 13.75 → Tax = 1.13 → Total = 14.88
        _cart.AddItem(FixedItem(price: 13.75m, qty: 1));

        _cart.Total.Should().Be(14.88m);
    }
}
