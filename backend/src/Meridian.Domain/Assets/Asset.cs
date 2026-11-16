using Meridian.Domain.Common;

namespace Meridian.Domain.Assets;

public sealed class Asset : Entity
{
    private Asset()
    {
    }

    public Guid SiteId { get; private set; }
    public string AssetTagValue { get; private set; } = "";
    public string Name { get; private set; } = "";
    public AssetCategory Category { get; private set; }
    public AssetStatus Status { get; private set; }
    public AssetCriticality Criticality { get; private set; }
    public string Manufacturer { get; private set; } = "";
    public string Model { get; private set; } = "";
    public string SerialNumber { get; private set; } = "";
    public DateOnly InstalledOn { get; private set; }
    public string Description { get; private set; } = "";
    public DateTimeOffset? LastInspectedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Asset Register(
        Guid siteId,
        string assetTag,
        string name,
        AssetCategory category,
        AssetStatus status,
        AssetCriticality criticality,
        string manufacturer,
        string model,
        string serialNumber,
        DateOnly installedOn,
        string description,
        DateOnly today,
        DateTimeOffset utcNow,
        Guid? id = null)
    {
        if (siteId == Guid.Empty)
        {
            throw new DomainException("Site is required.");
        }

        var asset = new Asset
        {
            Id = id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            CreatedAt = utcNow
        };

        asset.Retag(assetTag);
        asset.Revise(
            siteId,
            name,
            category,
            status,
            criticality,
            manufacturer,
            model,
            serialNumber,
            installedOn,
            description,
            today);

        return asset;
    }

    public void Retag(string assetTag)
    {
        AssetTagValue = AssetTag.Normalize(assetTag);
    }

    public void Revise(
        Guid siteId,
        string name,
        AssetCategory category,
        AssetStatus status,
        AssetCriticality criticality,
        string manufacturer,
        string model,
        string serialNumber,
        DateOnly installedOn,
        string description,
        DateOnly today)
    {
        if (siteId == Guid.Empty)
        {
            throw new DomainException("Site is required.");
        }

        if (!Enum.IsDefined(category) || !Enum.IsDefined(status) || !Enum.IsDefined(criticality))
        {
            throw new DomainException("Asset classification is invalid.");
        }

        if (installedOn > today)
        {
            throw new DomainException("Installed date cannot be in the future.");
        }

        SiteId = siteId;
        Name = DomainText.Required(name, "Name", 140);
        Category = category;
        Status = status;
        Criticality = criticality;
        Manufacturer = DomainText.Required(manufacturer, "Manufacturer", 80);
        Model = DomainText.Required(model, "Model", 80);
        SerialNumber = DomainText.Required(serialNumber, "Serial number", 80);
        InstalledOn = installedOn;
        Description = DomainText.Required(description, "Description", 2000);
    }

    public void EnsureInspectable()
    {
        if (Status == AssetStatus.Retired)
        {
            throw new DomainException("Retired assets cannot be inspected.");
        }
    }

    public void RecordInspection(DateTimeOffset inspectedAt)
    {
        EnsureInspectable();
        if (LastInspectedAt is null || inspectedAt > LastInspectedAt)
        {
            LastInspectedAt = inspectedAt;
        }
    }
}
