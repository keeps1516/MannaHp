namespace MannaHp.Shared.Entities;

public class OrderItemIngredient
{
	public Guid Id { get; set; }
	public Guid OrderItemId { get; set; }
	public Guid IngredientId { get; set; }
	public decimal QuantityUsed { get; set; }
	public decimal PriceCharged { get; set; }

	public OrderItem? OrderItem { get; set; }
	public Ingredient? Ingredient { get; set; }
}