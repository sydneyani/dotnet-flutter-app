using Meridian.Domain.Common;

namespace Meridian.Domain.Sites;

public sealed class Site : Entity
{
    private Site()
    {
    }

    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Region { get; private set; } = "";
    public string Address { get; private set; } = "";
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public static Site Establish(
        string code,
        string name,
        string region,
        string address,
        double latitude,
        double longitude,
        Guid? id = null)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new DomainException("Site coordinates are out of range.");
        }

        return new Site
        {
            Id = id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            Code = NormalizeCode(code),
            Name = DomainText.Required(name, "Site name", 120),
            Region = DomainText.Required(region, "Region", 80),
            Address = DomainText.Required(address, "Address", 200),
            Latitude = latitude,
            Longitude = longitude
        };
    }

    public static string NormalizeCode(string? code)
    {
        var normalized = AssetTagStyle(code);
        return normalized;
    }

    private static string AssetTagStyle(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Site code is required.");
        }

        var value = code.Trim().ToUpperInvariant();
        if (value.Length is < 2 or > 16 || !char.IsLetter(value[0]) || value[^1] == '-')
        {
            throw new DomainException("Site code must be 2–16 characters, start with a letter, and not end with a hyphen.");
        }

        var previousWasHyphen = false;
        foreach (var character in value)
        {
            var allowed = char.IsLetterOrDigit(character) || character == '-';
            if (!allowed || (character == '-' && previousWasHyphen))
            {
                throw new DomainException("Site code must contain only letters, digits, and single hyphens.");
            }

            previousWasHyphen = character == '-';
        }

        return value;
    }
}
