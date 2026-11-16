using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Meridian.Application.Abstractions;
using Meridian.Domain.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Meridian.Api.Security;

public sealed class JwtTokenService(IConfiguration configuration, IClock clock) : ITokenService
{
    public int RefreshTokenDays => Math.Clamp(configuration.GetValue("Jwt:RefreshTokenDays", 14), 1, 90);

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(UserAccount user)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
        }

        var minutes = Math.Clamp(configuration.GetValue("Jwt:AccessTokenMinutes", 60), 5, 24 * 60);
        var expiresAt = clock.UtcNow.AddMinutes(minutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            ],
            notBefore: clock.UtcNow.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
