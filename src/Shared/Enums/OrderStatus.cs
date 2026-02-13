namespace MannaHp.Shared.Enums;

public enum OrderStatus
{
	Received,
	Preparing,
	Ready,
	Completed,
	Cancelled
}

public enum PaymentMethod
{
	Card,
	InStore
}

public enum PaymentStatus
{
	Pending,
	Paid,
	Failed,
	Refunded
}
