using Meridian.Domain.Common;

namespace Meridian.Domain.Identity;

public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = "";
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByHash { get; private set; }

    public bool IsActive(DateTimeOffset utcNow) => RevokedAt is null && utcNow < ExpiresAt;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Refresh token user is required.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("Refresh token hash is required.");
        }

        if (expiresAt <= createdAt)
        {
            throw new DomainException("Refresh token expiry must be after it is issued.");
        }

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        };
    }

    public void Revoke(DateTimeOffset utcNow, string? replacedByHash)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = utcNow;
        ReplacedByHash = string.IsNullOrWhiteSpace(replacedByHash) ? null : replacedByHash.Trim();
    }
}
