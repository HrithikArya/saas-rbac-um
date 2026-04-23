namespace Application.Auth.Dtos;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    bool EmailVerified,
    bool IsSuperAdmin = false
);
