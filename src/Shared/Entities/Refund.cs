namespace MannaHp.Shared.Entities;

public class Refund
{
	public Guid Id { get; set; }
	public Guid OrderId { get; set; }
	public decimal Amount { get; set; }
	public decimal TaxAmount { get; set; }
	public string Reason { get; set; } = string.Empty;
	public string? StripeRefundId { get; set; }
	public string? CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }

	public Order? Order { get; set; }
	public List<RefundItem> Items { get; set; } = [];
}
