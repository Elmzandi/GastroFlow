using System.ComponentModel.DataAnnotations;

namespace GastroFlow.Application.DTOs.Auth;

public sealed record LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
