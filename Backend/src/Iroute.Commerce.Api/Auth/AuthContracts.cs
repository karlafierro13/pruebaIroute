using System.ComponentModel.DataAnnotations;

namespace Iroute.Commerce.Api.Auth;

public sealed record LoginRequest([Required] string Username, [Required] string Password);

public sealed record LoginResponse(string Token, string Username, DateTime ExpiresAt);

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}

/// <summary>
/// Usuario de demostración para el login opcional (configurado en appsettings).
/// </summary>
public sealed class DemoUserOptions
{
    public const string SectionName = "DemoUser";

    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
