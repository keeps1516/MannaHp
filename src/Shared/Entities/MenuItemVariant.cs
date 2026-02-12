namespace MannahHp.Shared.Entities;

public class MenuItemVariant
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Sortorder { get; set; }
    public bool Active { get; set; } = true;
    public MenuItem? MenuItem { get; set; }
    public List<RecipeIngredient> RecipeIngredients { get; set; } = [];
}
