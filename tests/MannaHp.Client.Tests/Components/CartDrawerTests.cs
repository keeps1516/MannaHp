using Bunit;
using FluentAssertions;
using MannaHp.Client.Components;
using MannaHp.Client.Models;
using MannaHp.Client.Services;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace MannaHp.Client.Tests.Components;

public class CartDrawerTests : BunitContext
{
    private readonly CartService _cart = new();

    public CartDrawerTests()
    {
        Services.AddSingleton(_cart);
        Services.AddSingleton(new OrderService(new HttpClient()));
        Services.AddMudServices();
    }

    // ── helpers ──────────────────────────────────────────────────────

    private static CartItem FixedItem(string name = "Latte", decimal price = 4.75m, int qty = 1) => new()
    {
        MenuItem = new MenuItemDto(Guid.NewGuid(), Guid.NewGuid(), name, null,
            IsCustomizable: false, Active: true, SortOrder: 0, Variants: [], AvailableIngredients: null),
        Variant = new MenuItemVariantDto(Guid.NewGuid(), "12oz", price, SortOrder: 0, Active: true),
        Quantity = qty
    };

    private static CartItem CustomizableItem(string name = "Burrito Bowl", decimal ingredientPrice = 3.00m) => new()
    {
        MenuItem = new MenuItemDto(Guid.NewGuid(), Guid.NewGuid(), name, null,
            IsCustomizable: true, Active: true, SortOrder: 0, Variants: [], AvailableIngredients: []),
        SelectedIngredients =
        [
            new AvailableIngredientDto(Guid.NewGuid(), Guid.NewGuid(), "Chicken",
                ingredientPrice, QuantityUsed: 8m, IsDefault: false, GroupName: "Proteins", SortOrder: 0, Active: true)
        ],
        Quantity = 1
    };

    // ── Empty state ─────────────────────────────────────────────────

    [Fact]
    public void EmptyCart_ShowsEmptyMessage()
    {
        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));

        cut.Markup.Should().Contain("Your cart is empty");
    }

    [Fact]
    public void EmptyCart_DoesNotShowPlaceOrderButton()
    {
        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));

        cut.Markup.Should().NotContain("Place Order");
    }

    // ── Renders items ───────────────────────────────────────────────

    [Fact]
    public void WithItems_ShowsItemNames()
    {
        _cart.AddItem(FixedItem(name: "Latte"));
        _cart.AddItem(CustomizableItem(name: "Burrito Bowl"));

        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));

        cut.Markup.Should().Contain("Latte (12oz)");
        cut.Markup.Should().Contain("Burrito Bowl");
    }

    [Fact]
    public void WithItems_ShowsSubtotalAndTax()
    {
        _cart.AddItem(FixedItem(price: 10.00m));

        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));

        cut.Markup.Should().Contain("$10.00");  // subtotal
        cut.Markup.Should().Contain("$0.82");   // tax (banker's rounding)
        cut.Markup.Should().Contain("$10.82");  // total
    }

    [Fact]
    public void WithItems_ShowsPlaceOrderButton()
    {
        _cart.AddItem(FixedItem());

        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));

        cut.Markup.Should().Contain("Place Order");
    }

    // ── Live re-render on CartService.OnChange ──────────────────────
    // These tests verify the fix: CartDrawer subscribes to Cart.OnChange
    // and re-renders automatically without needing to close/reopen the drawer.

    [Fact]
    public void AddItemAfterRender_UpdatesDrawerAutomatically()
    {
        // Render with empty cart
        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));
        cut.Markup.Should().Contain("Your cart is empty");

        // Add item externally (simulates adding from a dialog while drawer is open)
        _cart.AddItem(FixedItem(name: "Espresso"));

        // Drawer should show the new item without close/reopen
        cut.Markup.Should().NotContain("Your cart is empty");
        cut.Markup.Should().Contain("Espresso");
    }

    [Fact]
    public void AddMultipleItemsAfterRender_AllAppearAutomatically()
    {
        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));

        _cart.AddItem(FixedItem(name: "Latte", price: 4.75m));
        _cart.AddItem(CustomizableItem(name: "Bowl", ingredientPrice: 7.50m));

        cut.Markup.Should().Contain("Latte");
        cut.Markup.Should().Contain("Bowl");
        cut.Markup.Should().Contain("$12.25"); // subtotal: 4.75 + 7.50
    }

    [Fact]
    public void RemoveItemAfterRender_UpdatesDrawerAutomatically()
    {
        var item = FixedItem(name: "Cappuccino");
        _cart.AddItem(item);

        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));
        cut.Markup.Should().Contain("Cappuccino");

        // Remove externally
        _cart.RemoveItem(item.Id);

        cut.Markup.Should().NotContain("Cappuccino");
        cut.Markup.Should().Contain("Your cart is empty");
    }

    [Fact]
    public void ClearCartAfterRender_ShowsEmptyStateAutomatically()
    {
        _cart.AddItem(FixedItem(name: "Mocha"));
        _cart.AddItem(FixedItem(name: "Drip Coffee"));

        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));
        cut.Markup.Should().Contain("Mocha");
        cut.Markup.Should().Contain("Drip Coffee");

        _cart.Clear();

        cut.Markup.Should().Contain("Your cart is empty");
        cut.Markup.Should().NotContain("Place Order");
    }

    [Fact]
    public void UpdateQuantityAfterRender_UpdatesTotalAutomatically()
    {
        var item = FixedItem(price: 5.00m, qty: 1);
        _cart.AddItem(item);

        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));
        cut.Markup.Should().Contain("$5.00"); // line total at qty 1

        _cart.UpdateQuantity(item.Id, 3);

        cut.Markup.Should().Contain("$15.00"); // line total at qty 3
    }

    // ── Dispose unsubscribes ────────────────────────────────────────

    [Fact]
    public void Dispose_UnsubscribesFromCartOnChange()
    {
        var cut = Render<CartDrawer>(p => p.Add(x => x.OnClose, () => { }));

        // Dispose the rendered component (triggers IDisposable.Dispose)
        cut.Dispose();

        // Adding an item after dispose should not throw
        var act = () => _cart.AddItem(FixedItem());
        act.Should().NotThrow();
    }
}
