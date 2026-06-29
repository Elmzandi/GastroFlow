using System.ComponentModel.DataAnnotations;

namespace GastroFlow.Infrastructure.Options;

public sealed record JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32)]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 1440)]
    public double ExpirationMinutes { get; init; } = 60;
}
