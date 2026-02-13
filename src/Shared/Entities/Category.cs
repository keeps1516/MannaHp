namespace MannaHp.Shared.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<MenuItem> MenuItems { get; set; } = [];
}
