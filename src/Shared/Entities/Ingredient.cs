using MannaHp.Shared.Enums;

namespace MannaHp.Shared.Entities;

public class Ingredient
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public decimal CostPerUnit { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal LowStockThreshold { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}
