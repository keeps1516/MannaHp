namespace MannaHp.Shared.Entities;

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid Categoryid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCustomizable { get; set; }
    public bool Active { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MenuItemVariant> Variants { get; set; } = [];
    public List<MenuItemAvailableIngredient> AvailableIngredients { get; set; } = [];
}
