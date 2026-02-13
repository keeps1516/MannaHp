namespace MannaHp.Shared.Entities;

public class OrderItem
{
	public Guid Id { get; set; }
	public Guid OrderId { get; set; }
	public Guid MenuItemId { get; set; }
	public Guid? VariantId { get; set; }
	public int Quantity { get; set; }
	public decimal UnitPrice { get; set; }
	public decimal TotalPrice { get; set; }
	public string? Notes { get; set; }

	public Order? Order { get; set; }
	public MenuItem? MenuItem { get; set; }
	public MenuItemVariant? Variant { get; set; }
	public List<OrderItemIngredient> Ingredients { get; set; } = [];
}
