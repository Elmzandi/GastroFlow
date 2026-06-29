using System.ComponentModel.DataAnnotations;

namespace GastroFlow.Application.DTOs.Auth;

public sealed record RegisterRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(200)]
    public string RestaurantName { get; init; } = string.Empty;
}
