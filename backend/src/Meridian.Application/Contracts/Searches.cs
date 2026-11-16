namespace Meridian.Application.Contracts;

public sealed record AssetSearch(
    string? Search,
    Guid? SiteId,
    AssetStatus? Status,
    AssetCriticality? Criticality,
    AssetCategory? Category,
    int Page,
    int PageSize);

public sealed record WorkOrderSearch(
    string? Search,
    WorkOrderStatus? Status,
    WorkOrderPriority? Priority,
    WorkOrderType? Type,
    Guid? AssetId,
    Guid? SiteId,
    Guid? AssignedToUserId,
    bool? Overdue,
    int Page,
    int PageSize);

public sealed record InspectionSearch(
    Guid? AssetId,
    Guid? WorkOrderId,
    InspectionResult? Result,
    int Page,
    int PageSize);
