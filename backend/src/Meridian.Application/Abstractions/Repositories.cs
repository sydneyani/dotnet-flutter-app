using Meridian.Application.Contracts;

namespace Meridian.Application.Abstractions;

public interface IUserRepository
{
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserSummaryDto>> ListActiveAsync(CancellationToken cancellationToken);
}

public interface ISiteRepository
{
    Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SiteDto>> ListAsync(CancellationToken cancellationToken);
}

public interface IAssetRepository
{
    Task<Asset?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> TagExistsAsync(string normalizedTag, Guid? excludingId, CancellationToken cancellationToken);
    Task AddAsync(Asset asset, CancellationToken cancellationToken);
    Task<PagedResult<AssetDto>> SearchAsync(AssetSearch search, CancellationToken cancellationToken);
    Task<AssetDto?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public interface IWorkOrderRepository
{
    Task<WorkOrder?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken);
    void StageNewComment(WorkOrderComment comment);
    Task<string> AllocateNumberAsync(CancellationToken cancellationToken);
    Task<PagedResult<WorkOrderSummaryDto>> SearchAsync(WorkOrderSearch search, CancellationToken cancellationToken);
    Task<WorkOrderDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public interface IInspectionRepository
{
    Task AddAsync(Inspection inspection, CancellationToken cancellationToken);
    Task<PagedResult<InspectionDto>> SearchAsync(InspectionSearch search, CancellationToken cancellationToken);
    Task<InspectionDto?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken);
}

public interface IDashboardReader
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
