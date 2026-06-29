namespace GastroFlow.Application.DTOs.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string Email,
    string Role,
    Guid RestaurantId
);
