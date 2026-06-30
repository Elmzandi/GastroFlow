namespace GastroFlow.Application.DTOs.Auth;

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    string Email,
    string Role,
    Guid RestaurantId
);
