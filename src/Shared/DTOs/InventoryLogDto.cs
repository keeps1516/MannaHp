using MannaHp.Shared.Enums;

namespace MannaHp.Shared.DTOs;

public record InventoryLogDto(
	Guid Id,
	Guid IngredientId,
	string IngredientName,
	InventoryChangeType ChangeType,
	decimal QuantityChange,
	decimal NewStockQuantity,
	string? Notes,
	string? CreatedBy,
	DateTime CreatedAt);

public record RestockRequest(decimal Quantity, string? Notes);

public record BulkRestockItem(Guid IngredientId, decimal Quantity, string? Notes);

public record BulkRestockRequest(List<BulkRestockItem> Items);
