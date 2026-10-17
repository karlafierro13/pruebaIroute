using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Iroute.Commerce.Api.Auth;

public interface IAuthService
{
    LoginResponse? Login(LoginRequest request);
}

public sealed class AuthService(IOptions<JwtOptions> jwtOptions, IOptions<DemoUserOptions> userOptions) : IAuthService
{
    public LoginResponse? Login(LoginRequest request)
    {
        var user = userOptions.Value;
        if (!SecureEquals(request.Username, user.Username) || !SecureEquals(request.Password, user.Password))
            return null;

        var jwt = jwtOptions.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, user.Username), new Claim(ClaimTypes.Name, user.Username)],
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username, expiresAt);
    }

    // Comparación en tiempo constante para no filtrar información por tiempos de respuesta.
    private static bool SecureEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
