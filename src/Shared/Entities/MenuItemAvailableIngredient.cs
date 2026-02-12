namespace MannahHp.Shared.Entities;

public class MenuItemAvailableIngredient
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid IngredientId { get; set; }
    public decimal CustomerPrice { get; set; }
    public decimal QuantityUsed { get; set; }
    public bool IsDefault { get; set; }
    public string? GroupName { get; set; }
    public int SortOrder { get; set; }
    public bool Active { get; set; }
    public MenuItem? MenuItem { get; set; }
    public Ingredient? Ingredient { get; set; }
}
