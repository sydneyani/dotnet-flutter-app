using Meridian.Application.Abstractions;
using Meridian.Application.Common;
using Meridian.Application.Contracts;

namespace Meridian.Infrastructure.Persistence;

internal sealed class UserRepository(MeridianDbContext db) : IUserRepository
{
    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserSummaryDto>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .ToListAsync(cancellationToken);

        return users.Select(ReadModels.ToUser).ToArray();
    }
}

internal sealed class SiteRepository(MeridianDbContext db) : ISiteRepository
{
    public Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Sites.FirstOrDefaultAsync(site => site.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SiteDto>> ListAsync(CancellationToken cancellationToken)
    {
        var sites = await db.Sites.AsNoTracking().OrderBy(site => site.Code).ToListAsync(cancellationToken);
        return sites.Select(ReadModels.ToSite).ToArray();
    }
}

internal sealed class AssetRepository(MeridianDbContext db) : IAssetRepository
{
    public Task<Asset?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Assets.FirstOrDefaultAsync(asset => asset.Id == id, cancellationToken);

    public Task<bool> TagExistsAsync(string normalizedTag, Guid? excludingId, CancellationToken cancellationToken) =>
        db.Assets.AnyAsync(
            asset => asset.AssetTagValue == normalizedTag && (excludingId == null || asset.Id != excludingId),
            cancellationToken);

    public Task AddAsync(Asset asset, CancellationToken cancellationToken) =>
        db.Assets.AddAsync(asset, cancellationToken).AsTask();

    public async Task<AssetDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await (
            from asset in db.Assets.AsNoTracking()
            join site in db.Sites.AsNoTracking() on asset.SiteId equals site.Id
            where asset.Id == id
            select new { asset, site }).FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ReadModels.ToAsset(row.asset, row.site);
    }

    public async Task<PagedResult<AssetDto>> SearchAsync(AssetSearch search, CancellationToken cancellationToken)
    {
        var query =
            from asset in db.Assets.AsNoTracking()
            join site in db.Sites.AsNoTracking() on asset.SiteId equals site.Id
            select new { asset, site };

        if (search.SiteId is Guid siteId)
        {
            query = query.Where(row => row.asset.SiteId == siteId);
        }

        if (search.Status is AssetStatus status)
        {
            query = query.Where(row => row.asset.Status == status);
        }

        if (search.Criticality is AssetCriticality criticality)
        {
            query = query.Where(row => row.asset.Criticality == criticality);
        }

        if (search.Category is AssetCategory category)
        {
            query = query.Where(row => row.asset.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var term = search.Search.Trim().ToLower();
            query = query.Where(row =>
                row.asset.AssetTagValue.ToLower().Contains(term)
                || row.asset.Name.ToLower().Contains(term)
                || row.asset.Manufacturer.ToLower().Contains(term)
                || row.asset.SerialNumber.ToLower().Contains(term)
                || row.site.Code.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(row => row.asset.AssetTagValue)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AssetDto>(rows.Select(row => ReadModels.ToAsset(row.asset, row.site)).ToArray(), search.Page, search.PageSize, total);
    }
}

internal sealed class WorkOrderRepository(MeridianDbContext db, Meridian.Application.Abstractions.IClock clock) : IWorkOrderRepository
{
    public Task<WorkOrder?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.WorkOrders.Include(workOrder => workOrder.Comments).FirstOrDefaultAsync(workOrder => workOrder.Id == id, cancellationToken);

    public Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken) =>
        db.WorkOrders.AddAsync(workOrder, cancellationToken).AsTask();

    public void StageNewComment(WorkOrderComment comment) => db.Entry(comment).State = EntityState.Added;

    public async Task<string> AllocateNumberAsync(CancellationToken cancellationToken)
    {
        var sequence = await db.NumberSequences.FirstOrDefaultAsync(item => item.Name == NumberSequence.WorkOrder, cancellationToken)
            ?? throw new ConflictException("Work order number sequence is not initialized.");

        return $"WO-{sequence.Take():00000}";
    }

    public async Task<WorkOrderDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var workOrder = await db.WorkOrders.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (workOrder is null)
        {
            return null;
        }

        var asset = await db.Assets.AsNoTracking().FirstAsync(item => item.Id == workOrder.AssetId, cancellationToken);
        var site = await db.Sites.AsNoTracking().FirstAsync(item => item.Id == workOrder.SiteId, cancellationToken);
        var creator = await db.Users.AsNoTracking().FirstAsync(item => item.Id == workOrder.CreatedByUserId, cancellationToken);
        UserAccount? assignee = null;
        if (workOrder.AssignedToUserId is Guid assigneeId)
        {
            assignee = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == assigneeId, cancellationToken);
        }

        var comments = await (
            from comment in db.WorkOrderComments.AsNoTracking()
            join author in db.Users.AsNoTracking() on comment.AuthorUserId equals author.Id
            where comment.WorkOrderId == id
            orderby comment.CreatedAt
            select new CommentDto(comment.Id, comment.AuthorUserId, author.DisplayName, comment.Body, comment.CreatedAt)
        ).ToListAsync(cancellationToken);

        return ReadModels.ToDetail(workOrder, asset, site, assignee, creator, comments, clock.UtcNow);
    }

    public async Task<PagedResult<WorkOrderSummaryDto>> SearchAsync(WorkOrderSearch search, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var query =
            from workOrder in db.WorkOrders.AsNoTracking()
            join asset in db.Assets.AsNoTracking() on workOrder.AssetId equals asset.Id
            join site in db.Sites.AsNoTracking() on workOrder.SiteId equals site.Id
            join assignee in db.Users.AsNoTracking() on workOrder.AssignedToUserId equals assignee.Id into assignees
            from assignee in assignees.DefaultIfEmpty()
            select new { workOrder, asset, site, assignee };

        if (search.Status is WorkOrderStatus status)
        {
            query = query.Where(row => row.workOrder.Status == status);
        }

        if (search.Priority is WorkOrderPriority priority)
        {
            query = query.Where(row => row.workOrder.Priority == priority);
        }

        if (search.Type is WorkOrderType type)
        {
            query = query.Where(row => row.workOrder.Type == type);
        }

        if (search.AssetId is Guid assetId)
        {
            query = query.Where(row => row.workOrder.AssetId == assetId);
        }

        if (search.SiteId is Guid siteId)
        {
            query = query.Where(row => row.workOrder.SiteId == siteId);
        }

        if (search.AssignedToUserId is Guid assignedTo)
        {
            query = query.Where(row => row.workOrder.AssignedToUserId == assignedTo);
        }

        if (search.Overdue is true)
        {
            query = query.Where(row =>
                row.workOrder.DueAt != null
                && row.workOrder.DueAt < now
                && row.workOrder.Status != WorkOrderStatus.Completed
                && row.workOrder.Status != WorkOrderStatus.Cancelled);
        }
        else if (search.Overdue is false)
        {
            query = query.Where(row =>
                row.workOrder.DueAt == null
                || row.workOrder.DueAt >= now
                || row.workOrder.Status == WorkOrderStatus.Completed
                || row.workOrder.Status == WorkOrderStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var term = search.Search.Trim().ToLower();
            query = query.Where(row =>
                row.workOrder.Number.ToLower().Contains(term)
                || row.workOrder.Title.ToLower().Contains(term)
                || row.asset.AssetTagValue.ToLower().Contains(term)
                || row.asset.Name.ToLower().Contains(term)
                || row.site.Code.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(row => row.workOrder.CreatedAt)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => ReadModels.ToSummary(row.workOrder, row.asset, row.site, row.assignee, now))
            .ToArray();

        return new PagedResult<WorkOrderSummaryDto>(items, search.Page, search.PageSize, total);
    }
}

internal sealed class InspectionRepository(MeridianDbContext db) : IInspectionRepository
{
    public Task AddAsync(Inspection inspection, CancellationToken cancellationToken) =>
        db.Inspections.AddAsync(inspection, cancellationToken).AsTask();

    public async Task<InspectionDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var inspection = await db.Inspections.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (inspection is null)
        {
            return null;
        }

        var mapped = await MapAsync([inspection], cancellationToken);
        return mapped[0];
    }

    public async Task<PagedResult<InspectionDto>> SearchAsync(InspectionSearch search, CancellationToken cancellationToken)
    {
        var query = db.Inspections.AsNoTracking().AsQueryable();
        if (search.AssetId is Guid assetId)
        {
            query = query.Where(inspection => inspection.AssetId == assetId);
        }

        if (search.WorkOrderId is Guid workOrderId)
        {
            query = query.Where(inspection => inspection.WorkOrderId == workOrderId);
        }

        if (search.Result is InspectionResult result)
        {
            query = query.Where(inspection => inspection.Result == result);
        }

        var total = await query.CountAsync(cancellationToken);
        var page = await query
            .OrderByDescending(inspection => inspection.InspectedAt)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<InspectionDto>(await MapAsync(page, cancellationToken), search.Page, search.PageSize, total);
    }

    private async Task<IReadOnlyList<InspectionDto>> MapAsync(IReadOnlyList<Inspection> inspections, CancellationToken cancellationToken)
    {
        if (inspections.Count == 0)
        {
            return [];
        }

        var assetIds = inspections.Select(inspection => inspection.AssetId).Distinct().ToArray();
        var inspectorIds = inspections.Select(inspection => inspection.InspectorUserId).Distinct().ToArray();
        var workOrderIds = inspections.Where(inspection => inspection.WorkOrderId is not null).Select(inspection => inspection.WorkOrderId!.Value).Distinct().ToArray();
        var assets = await db.Assets.AsNoTracking().Where(asset => assetIds.Contains(asset.Id)).ToDictionaryAsync(asset => asset.Id, cancellationToken);
        var inspectors = await db.Users.AsNoTracking().Where(user => inspectorIds.Contains(user.Id)).ToDictionaryAsync(user => user.Id, cancellationToken);
        var workOrders = workOrderIds.Length == 0
            ? new Dictionary<Guid, WorkOrder>()
            : await db.WorkOrders.AsNoTracking().Where(workOrder => workOrderIds.Contains(workOrder.Id)).ToDictionaryAsync(workOrder => workOrder.Id, cancellationToken);

        return inspections.Select(inspection =>
        {
            var asset = assets[inspection.AssetId];
            var inspector = inspectors[inspection.InspectorUserId];
            workOrders.TryGetValue(inspection.WorkOrderId ?? Guid.Empty, out var workOrder);
            return new InspectionDto(
                inspection.Id,
                asset.Id,
                asset.AssetTagValue,
                asset.Name,
                workOrder?.Id,
                workOrder?.Number,
                inspector.Id,
                inspector.DisplayName,
                inspection.InspectedAt,
                inspection.Result,
                inspection.Notes,
                inspection.Findings,
                inspection.NextDueOn);
        }).ToArray();
    }
}

internal sealed class RefreshTokenRepository(MeridianDbContext db) : IRefreshTokenRepository
{
    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken) =>
        db.RefreshTokens.AddAsync(token, cancellationToken).AsTask();

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeAllForUserAsync(Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var tokens = await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(utcNow, null);
        }
    }
}

internal sealed class DashboardReader(MeridianDbContext db, IClock clock) : IDashboardReader
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var since = now.AddDays(-30);
        var workOrders = await db.WorkOrders.AsNoTracking()
            .Select(workOrder => new WorkOrderFact(workOrder.Status, workOrder.Priority, workOrder.Type, workOrder.DueAt, workOrder.CompletedAt, workOrder.LaborHours))
            .ToListAsync(cancellationToken);

        var assets = await db.Assets.AsNoTracking()
            .Select(asset => new AssetFact(asset.Status, asset.Criticality, asset.Category))
            .ToListAsync(cancellationToken);

        var inspectionFacts = await db.Inspections.AsNoTracking()
            .Select(inspection => new { inspection.Result, inspection.InspectedAt })
            .ToListAsync(cancellationToken);
        var failedInspections = inspectionFacts.Count(inspection =>
            inspection.Result == InspectionResult.Fail && inspection.InspectedAt >= since);

        var active = workOrders.Where(IsActive).ToArray();
        var completedRecently = workOrders
            .Where(workOrder => workOrder.Status == WorkOrderStatus.Completed && workOrder.CompletedAt is not null && workOrder.CompletedAt >= since)
            .ToArray();
        var averageLabor = completedRecently.Length == 0
            ? 0m
            : decimal.Round(completedRecently.Average(workOrder => workOrder.LaborHours), 2, MidpointRounding.AwayFromZero);

        return new DashboardSummaryDto(
            workOrders.Count(workOrder => workOrder.Status == WorkOrderStatus.Open),
            workOrders.Count(workOrder => workOrder.Status == WorkOrderStatus.InProgress),
            workOrders.Count(workOrder => workOrder.Status == WorkOrderStatus.WaitingParts),
            workOrders.Count(workOrder => ReadModels.IsOverdue(workOrder.Status, workOrder.DueAt, now)),
            completedRecently.Length,
            averageLabor,
            assets.Count(asset => asset.Status == AssetStatus.InService),
            assets.Count(asset => asset.Status == AssetStatus.OutOfService),
            assets.Count(asset => asset.Criticality == AssetCriticality.Critical && asset.Status != AssetStatus.Retired),
            failedInspections,
            Count(active.Select(workOrder => workOrder.Priority.ToString()), Enum.GetNames<WorkOrderPriority>()),
            Count(active.Select(workOrder => workOrder.Type.ToString()), Enum.GetNames<WorkOrderType>()),
            Count(assets.Select(asset => asset.Category.ToString()), Enum.GetNames<AssetCategory>()));
    }

    private static bool IsActive(WorkOrderFact workOrder) =>
        workOrder.Status is WorkOrderStatus.Open or WorkOrderStatus.InProgress or WorkOrderStatus.WaitingParts;

    private static IReadOnlyList<CountByLabel> Count(IEnumerable<string> values, IEnumerable<string> labels)
    {
        var counts = values.GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count());
        return labels.Select(label => new CountByLabel(label, counts.GetValueOrDefault(label))).ToArray();
    }

    private sealed record WorkOrderFact(
        WorkOrderStatus Status,
        WorkOrderPriority Priority,
        WorkOrderType Type,
        DateTimeOffset? DueAt,
        DateTimeOffset? CompletedAt,
        decimal LaborHours);

    private sealed record AssetFact(AssetStatus Status, AssetCriticality Criticality, AssetCategory Category);
}
