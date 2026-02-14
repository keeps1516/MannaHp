namespace MannaHp.Shared.DTOs;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string? DisplayName);
public record AuthResponse(string Token, string Email, string DisplayName, string Role, DateTime ExpiresAt);
public record UserDto(string Id, string Email, string? DisplayName, string Role);
