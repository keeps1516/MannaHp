namespace MannaHp.Shared.Entities;

public class RefundItem
{
	public Guid Id { get; set; }
	public Guid RefundId { get; set; }
	public Guid OrderItemId { get; set; }
	public decimal Amount { get; set; }
	public bool Restocked { get; set; }

	public Refund? Refund { get; set; }
	public OrderItem? OrderItem { get; set; }
}
