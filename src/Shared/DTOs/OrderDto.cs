using MannaHp.Shared.Enums;

namespace MannaHp.Shared.DTOs;

// What the client sends to place an order
public record CreateOrderRequest(
	PaymentMethod PaymentMethod,
	string? Notes,
	List<CreateOrderItemRequest> Items);

public record CreateOrderItemRequest(
	Guid MenuItemId,
	Guid? VariantId,
	int Quantity,
	string? Notes,
	List<Guid>? SelectedIngredientIds);  // for customizable items only

// What the API returns
public record OrderDto(
	Guid Id,
	int OrderNumber,
	OrderStatus Status,
	PaymentMethod PaymentMethod,
	PaymentStatus PaymentStatus,
	decimal Subtotal,
	decimal TaxRate,
	decimal Tax,
	decimal Total,
	decimal RefundedAmount,
	string? Notes,
	DateTime CreatedAt,
	List<OrderItemDto> Items);

public record OrderItemDto(
	Guid Id,
	string MenuItemName,
	string? VariantName,
	int Quantity,
	decimal UnitPrice,
	decimal TotalPrice,
	string? Notes,
	RestockPolicy RestockPolicy,
	List<OrderItemIngredientDto>? Ingredients);

public record OrderItemIngredientDto(
	Guid IngredientId,
	string IngredientName,
	decimal QuantityUsed,
	UnitOfMeasure IngredientUnit,
	decimal PriceCharged);

// Response for order creation — includes Stripe client secret for card payments
public record CreateOrderResponse(
	OrderDto Order,
	string? ClientSecret,
	string? StripePublishableKey);

// For status updates (kitchen staff)
public record UpdateOrderStatusRequest(OrderStatus Status);

// Cancel request
public record CancelOrderRequest(
	string Reason,
	List<CancelItemRestockRequest>? RestockItems);

public record CancelItemRestockRequest(
	Guid OrderItemId,
	bool Restock);

// Refund request
public record CreateRefundRequest(
	string Reason,
	List<RefundItemRequest> Items);

public record RefundItemRequest(
	Guid OrderItemId,
	bool Restock);

// Refund response
public record RefundDto(
	Guid Id,
	Guid OrderId,
	decimal Amount,
	decimal TaxAmount,
	string Reason,
	string? StripeRefundId,
	string CreatedBy,
	DateTime CreatedAt,
	List<RefundItemDto> Items);

public record RefundItemDto(
	Guid Id,
	Guid OrderItemId,
	string MenuItemName,
	decimal Amount,
	bool Restocked);
