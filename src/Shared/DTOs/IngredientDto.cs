using MannaHp.Shared.Enums;

namespace MannaHp.Shared.DTOs;

public record IngredientDto(Guid Id, string Name, UnitOfMeasure Unit, decimal CostPerUnit,
	decimal StockQuantity, decimal LowStockThreshold, bool Active);
public record CreateIngredientRequest(string Name, UnitOfMeasure Unit, decimal CostPerUnit,
	decimal StockQuantity, decimal LowStockThreshold);
public record UpdateIngredientRequest(string Name, UnitOfMeasure Unit, decimal CostPerUnit,
	decimal StockQuantity, decimal LowStockThreshold, bool Active);