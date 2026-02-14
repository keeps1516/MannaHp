namespace MannaHp.Shared.DTOs;

public record MenuItemDto(Guid Id, Guid CategoryId, string Name, string? Description,
				bool IsCustomizable, bool Active, int SortOrder, List<MenuItemVariantDto> Variants,
				List<AvailableIngredientDto>? AvailableIngredients);

public record CreateMenuItemRequest(Guid CategoryId, string Name, string? Description,
			   bool IsCustomizable, int SortOrder);
public record UpdateMenuItemRequest(string Name, string? Description, bool IsCustomizable,
				Guid CategoryId, int SortOrder, bool Active);

public record MenuItemVariantDto(Guid Id, string Name, decimal Price, int SortOrder, bool Active);
public record CreateVariantRequest(string Name, decimal Price, int SortOrder);
public record UpdateVariantRequest(string Name, decimal Price, int SortOrder, bool Active);

public record AvailableIngredientDto(Guid Id, Guid IngredientId, string IngredientName,
				decimal CustomerPrice, decimal QuantityUsed, bool IsDefault, string GroupName,
				int SortOrder, bool Active, int IngredientUnit);
public record CreateAvailableIngredientRequest(Guid IngredientId, decimal CustomerPrice,
				decimal QuantityUsed, bool IsDefault, string GroupName, int SortOrder);
public record UpdateAvailableIngredientRequest(decimal CustomerPrice, decimal QuantityUsed,
				bool IsDefault, string GroupName, int SortOrder, bool Active);

public record RecipeIngredientDto(Guid Id, Guid IngredientId, string IngredientName, decimal Quantity);
public record CreateRecipeIngredientRequest(Guid IngredientId, decimal Quantity);
public record UpdateRecipeIngredientRequest(decimal Quantity);
