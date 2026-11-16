namespace Meridian.Application.Contracts;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record SiteDto(
    Guid Id,
    string Code,
    string Name,
    string Region,
    string Address,
    double Latitude,
    double Longitude);

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role);

public sealed record AuthTokensDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserSummaryDto User);

public sealed record AssetDto(
    Guid Id,
    Guid SiteId,
    string SiteCode,
    string SiteName,
    string AssetTag,
    string Name,
    AssetCategory Category,
    AssetStatus Status,
    AssetCriticality Criticality,
    string Manufacturer,
    string Model,
    string SerialNumber,
    DateOnly InstalledOn,
    string Description,
    DateTimeOffset? LastInspectedAt,
    DateTimeOffset CreatedAt);

public sealed record CommentDto(
    Guid Id,
    Guid AuthorUserId,
    string AuthorName,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record WorkOrderSummaryDto(
    Guid Id,
    string Number,
    string Title,
    WorkOrderType Type,
    WorkOrderPriority Priority,
    WorkOrderStatus Status,
    Guid AssetId,
    string AssetTag,
    string AssetName,
    Guid SiteId,
    string SiteCode,
    string SiteName,
    Guid? AssignedToUserId,
    string? AssigneeName,
    DateTimeOffset? DueAt,
    decimal LaborHours,
    DateTimeOffset CreatedAt,
    bool IsOverdue);

public sealed record WorkOrderDetailDto(
    Guid Id,
    string Number,
    string Title,
    string Description,
    WorkOrderType Type,
    WorkOrderPriority Priority,
    WorkOrderStatus Status,
    Guid AssetId,
    string AssetTag,
    string AssetName,
    Guid SiteId,
    string SiteCode,
    string SiteName,
    Guid? AssignedToUserId,
    string? AssigneeName,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTimeOffset? DueAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    decimal LaborHours,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsOverdue,
    IReadOnlyList<string> AllowedTransitions,
    IReadOnlyList<CommentDto> Comments);

public sealed record InspectionDto(
    Guid Id,
    Guid AssetId,
    string AssetTag,
    string AssetName,
    Guid? WorkOrderId,
    string? WorkOrderNumber,
    Guid InspectorUserId,
    string InspectorName,
    DateTimeOffset InspectedAt,
    InspectionResult Result,
    string Notes,
    string? Findings,
    DateOnly? NextDueOn);

public sealed record CountByLabel(string Label, int Count);

public sealed record DashboardSummaryDto(
    int OpenWorkOrders,
    int InProgressWorkOrders,
    int WaitingPartsWorkOrders,
    int OverdueWorkOrders,
    int CompletedLast30Days,
    decimal AverageLaborHoursCompleted,
    int AssetsInService,
    int AssetsOutOfService,
    int CriticalAssets,
    int FailedInspectionsLast30Days,
    IReadOnlyList<CountByLabel> ActiveWorkOrdersByPriority,
    IReadOnlyList<CountByLabel> ActiveWorkOrdersByType,
    IReadOnlyList<CountByLabel> AssetsByCategory);
