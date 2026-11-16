using Meridian.Domain.Common;

namespace Meridian.Domain.Assets;

public static class AssetTag
{
    public const int MinLength = 2;
    public const int MaxLength = 16;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Asset tag is required.");
        }

        var tag = value.Trim().ToUpperInvariant();
        if (tag.Length < MinLength || tag.Length > MaxLength)
        {
            throw new DomainException($"Asset tag must be {MinLength}–{MaxLength} characters.");
        }

        if (!char.IsLetter(tag[0]) || tag[^1] == '-')
        {
            throw new DomainException("Asset tag must start with a letter and cannot end with a hyphen.");
        }

        var previousWasHyphen = false;
        foreach (var character in tag)
        {
            var allowed = char.IsLetterOrDigit(character) || character == '-';
            if (!allowed || (character == '-' && previousWasHyphen))
            {
                throw new DomainException("Asset tag must contain only letters, digits, and single hyphens.");
            }

            previousWasHyphen = character == '-';
        }

        return tag;
    }
}
