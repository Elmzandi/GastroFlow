using System.ComponentModel.DataAnnotations;

namespace GastroFlow.Application.DTOs.Auth;

public sealed record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
