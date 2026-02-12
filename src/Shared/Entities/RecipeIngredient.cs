namespace MannahHp.Shared.Entities;

public class RecipeIngredient
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public MenuItemVariant? Variant { get; set; }
    public Ingredient? Ingredient { get; set; }
}
