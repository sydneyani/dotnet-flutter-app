using Meridian.Application.Abstractions;

namespace Meridian.Application.Common;

public static class CurrentUserGuard
{
    public static (Guid UserId, UserRole Role) Require(this ICurrentUser current)
    {
        if (current.UserId is not Guid userId || current.Role is not UserRole role)
        {
            throw new ForbiddenException("Sign in is required.");
        }

        return (userId, role);
    }
}
