using Meridian.Domain.Common;

namespace Meridian.Domain.Identity;

public sealed class UserAccount : Entity
{
    private UserAccount()
    {
    }

    public string Email { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static UserAccount Create(
        string email,
        string displayName,
        string passwordHash,
        UserRole role,
        DateTimeOffset utcNow,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainException("User role is invalid.");
        }

        var normalizedEmail = NormalizeEmail(email);
        return new UserAccount
        {
            Id = id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            Email = normalizedEmail,
            DisplayName = DomainText.Required(displayName, "Display name", 80),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = utcNow
        };
    }

    public static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        var at = normalized.IndexOf('@');
        if (at <= 0 || at != normalized.LastIndexOf('@') || at == normalized.Length - 1 || normalized.Contains(' '))
        {
            throw new DomainException("Email is invalid.");
        }

        return normalized;
    }
}
