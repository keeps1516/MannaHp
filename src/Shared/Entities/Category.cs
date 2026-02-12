using MannahHp.Shared.Enums;

namespace MannahHp.Shared.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<MenuItem> MenuItems {get;set;} = [];
}

public class Ingredient
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public decimal CostPerUnit { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal LowStockThreshold { get; set; }
}

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid Categoryid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCustomizable { get; set; }
    public bool Active { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set;} = DateTime.UtcNow;
    public List<MenuItemVariant> Variants { get; set; } = [];
    public List<MenuItemAvailableIngredient> AvailableIngredients { get; set; } = [];
}

public class MenuItemVariant
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Sortorder { get; set; }
    public bool Active { get; set; } = true;
    public MenuItem? MenuItem {  get; set; }
    public List<RecipeIngredient> RecipeIngredients { get; set; } = [];
}

public class RecipeIngredient
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public MenuItemVariant? Variant { get; set; }
    public Ingredient? Ingredient { get; set; }

}

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