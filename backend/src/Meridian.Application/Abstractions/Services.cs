namespace Meridian.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenService
{
    int RefreshTokenDays { get; }
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(UserAccount user);
}

public interface IRefreshTokenFactory
{
    string GenerateToken();
    string Hash(string token);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    UserRole? Role { get; }
}
