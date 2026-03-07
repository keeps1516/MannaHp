using MannaHp.Shared.Enums;

namespace MannaHp.Shared.Entities;

public class InventoryLog
{
	public Guid Id { get; set; }
	public Guid IngredientId { get; set; }
	public InventoryChangeType ChangeType { get; set; }
	public decimal QuantityChange { get; set; }
	public decimal NewStockQuantity { get; set; }
	public string? Notes { get; set; }
	public string? CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }

	public Ingredient? Ingredient { get; set; }
}
