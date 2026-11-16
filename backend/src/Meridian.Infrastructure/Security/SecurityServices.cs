using System.Security.Cryptography;
using System.Text;
using Meridian.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Meridian.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<UserAccount> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) is not PasswordVerificationResult.Failed;
}

public sealed class RefreshTokenFactory : IRefreshTokenFactory
{
    public string GenerateToken()
    {
        var encoded = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return encoded.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
