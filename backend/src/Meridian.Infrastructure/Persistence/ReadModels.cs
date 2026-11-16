using Meridian.Application.Contracts;

namespace Meridian.Infrastructure.Persistence;

internal static class ReadModels
{
    public static bool IsOverdue(WorkOrderStatus status, DateTimeOffset? dueAt, DateTimeOffset utcNow) =>
        dueAt is not null
        && dueAt.Value < utcNow
        && status is not WorkOrderStatus.Completed and not WorkOrderStatus.Cancelled;

    public static SiteDto ToSite(Site site) =>
        new(site.Id, site.Code, site.Name, site.Region, site.Address, site.Latitude, site.Longitude);

    public static UserSummaryDto ToUser(UserAccount user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role);

    public static AssetDto ToAsset(Asset asset, Site site) =>
        new(
            asset.Id,
            asset.SiteId,
            site.Code,
            site.Name,
            asset.AssetTagValue,
            asset.Name,
            asset.Category,
            asset.Status,
            asset.Criticality,
            asset.Manufacturer,
            asset.Model,
            asset.SerialNumber,
            asset.InstalledOn,
            asset.Description,
            asset.LastInspectedAt,
            asset.CreatedAt);

    public static WorkOrderSummaryDto ToSummary(
        WorkOrder workOrder,
        Asset asset,
        Site site,
        UserAccount? assignee,
        DateTimeOffset utcNow) =>
        new(
            workOrder.Id,
            workOrder.Number,
            workOrder.Title,
            workOrder.Type,
            workOrder.Priority,
            workOrder.Status,
            asset.Id,
            asset.AssetTagValue,
            asset.Name,
            site.Id,
            site.Code,
            site.Name,
            assignee?.Id,
            assignee?.DisplayName,
            workOrder.DueAt,
            workOrder.LaborHours,
            workOrder.CreatedAt,
            IsOverdue(workOrder.Status, workOrder.DueAt, utcNow));

    public static WorkOrderDetailDto ToDetail(
        WorkOrder workOrder,
        Asset asset,
        Site site,
        UserAccount? assignee,
        UserAccount creator,
        IReadOnlyList<CommentDto> comments,
        DateTimeOffset utcNow) =>
        new(
            workOrder.Id,
            workOrder.Number,
            workOrder.Title,
            workOrder.Description,
            workOrder.Type,
            workOrder.Priority,
            workOrder.Status,
            asset.Id,
            asset.AssetTagValue,
            asset.Name,
            site.Id,
            site.Code,
            site.Name,
            assignee?.Id,
            assignee?.DisplayName,
            creator.Id,
            creator.DisplayName,
            workOrder.DueAt,
            workOrder.StartedAt,
            workOrder.CompletedAt,
            workOrder.LaborHours,
            workOrder.CreatedAt,
            workOrder.UpdatedAt,
            IsOverdue(workOrder.Status, workOrder.DueAt, utcNow),
            WorkOrderTransitions.AllowedFrom(workOrder.Status).Select(status => status.ToString()).ToArray(),
            comments);
}
