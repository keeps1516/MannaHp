using MannaHp.Shared.DTOs;

namespace MannaHp.Client.Models;

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MenuItemDto MenuItem { get; set; } = default!;
    public MenuItemVariantDto? Variant { get; set; }
    public List<AvailableIngredientDto>? SelectedIngredients { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }

    public decimal UnitPrice => MenuItem.IsCustomizable
        ? (SelectedIngredients?.Sum(i => i.CustomerPrice) ?? 0m)
        : (Variant?.Price ?? 0m);

    public decimal LineTotal => UnitPrice * Quantity;

    public string DisplayName => MenuItem.IsCustomizable
        ? MenuItem.Name
        : Variant is not null
            ? $"{MenuItem.Name} ({Variant.Name})"
            : MenuItem.Name;
}
