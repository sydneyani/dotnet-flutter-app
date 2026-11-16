namespace Meridian.Domain.Common;

public static class DomainText
{
    public static string Required(string? value, string field, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new DomainException($"{field} must be {max} characters or fewer.");
        }

        return trimmed;
    }

    public static string? Optional(string? value, string field, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new DomainException($"{field} must be {max} characters or fewer.");
        }

        return trimmed;
    }
}
