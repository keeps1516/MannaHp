using MannaHp.Shared.Enums;

namespace MannaHp.Shared.Entities;

public class Order
{
	public Guid Id { get; set; }
	public int OrderNumber { get; set; }
	public string? UserId { get; set; }
	public OrderStatus Status { get; set; }
	public PaymentMethod PaymentMethod { get; set; }
	public PaymentStatus PaymentStatus { get; set; }
	public string? StripePaymentId { get; set; }
	public string? CardBrand { get; set; }
	public string? CardLast4 { get; set; }
	public decimal Subtotal { get; set; }
	public decimal TaxRate { get; set; }
	public decimal Tax { get; set; }
	public decimal Total { get; set; }
	public bool Printed { get; set; }
	public string? Notes { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	public List<OrderItem> Items { get; set; } = [];
}
