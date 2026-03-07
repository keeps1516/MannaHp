namespace MannaHp.Shared.DTOs;

public record GenerateStoreTokenRequest(int? DurationDays);

public record StoreTokenResponse(
    Guid Id,
    string Token,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    string? CreatedByUserId);

public record StoreTokenValidationResponse(bool Valid, DateTime? ExpiresAt);
