namespace MannaHp.Shared.DTOs;

public record CategoryDto(Guid Id, string Name, int SortOrder, bool Active);
public record CreateCategoryRequest(string Name, int SortOrder);
public record UpdateCategoryRequest(string Name, int SortOrder, bool Active);

