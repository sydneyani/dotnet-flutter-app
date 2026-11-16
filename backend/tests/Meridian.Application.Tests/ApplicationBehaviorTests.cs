using FluentValidation;
using Meridian.Application.Abstractions;
using Meridian.Application.Assets;
using Meridian.Application.Auth;
using Meridian.Application.Authorization;
using Meridian.Application.Behaviors;
using Meridian.Application.Common;
using Meridian.Application.Contracts;
using Meridian.Application.WorkOrders;
using Meridian.Domain;
using Meridian.Domain.Common;
using Meridian.Domain.Identity;
using Meridian.Domain.WorkOrders;

namespace Meridian.Application.Tests;

public class ApplicationBehaviorTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 2, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Technicians_cannot_assign_work_to_someone_else()
    {
        var technician = Guid.NewGuid();
        var other = Guid.NewGuid();
        var error = Assert.Throws<ForbiddenException>(() =>
            AccessRules.EnsureCanAssign(UserRole.Technician, technician, null, other));

        Assert.Contains("themselves", error.Message, StringComparison.OrdinalIgnoreCase);
        AccessRules.EnsureCanAssign(UserRole.Supervisor, technician, null, other);
        Assert.True(AccessRules.CanManageAssets(UserRole.Admin));
        Assert.False(AccessRules.CanManageAssets(UserRole.Technician));
    }

    [Fact]
    public void Page_size_is_clamped()
    {
        Assert.Equal((1, 20), PageRequest.Normalize(0, 0));
        Assert.Equal((2, 100), PageRequest.Normalize(2, 500));
    }

    [Fact]
    public async Task Validation_pipeline_rejects_a_blank_login()
    {
        var behavior = new ValidationBehavior<LoginCommand, AuthTokensDto>([new LoginCommandValidator()]);
        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new LoginCommand("", ""), _ => Task.FromResult<AuthTokensDto>(null!), CancellationToken.None));

        Assert.Contains(error.Errors, failure => failure.PropertyName == "Email");
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password_and_issues_tokens_for_the_right_one()
    {
        var user = UserAccount.Create("maya.okonkwo@meridian.demo", "Maya Okonkwo", "Meridian#demo1", UserRole.Technician, Now);
        var users = new FakeUsers([user]);
        var tokens = new FakeTokens();
        var refresh = new FakeRefreshTokens();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new LoginHandler(users, new PlainPasswordHasher(), tokens, new FakeRefreshFactory(), refresh, new FixedClock(Now), unitOfWork);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            handler.Handle(new LoginCommand(user.Email, "wrong-password"), CancellationToken.None));

        var session = await handler.Handle(new LoginCommand("  Maya.Okonkwo@meridian.demo ", "Meridian#demo1"), CancellationToken.None);

        Assert.Equal("access-token", session.AccessToken);
        Assert.Equal("refresh-1", session.RefreshToken);
        Assert.Equal(user.Id, session.User.Id);
        Assert.Equal(1, unitOfWork.Saves);
        Assert.Single(refresh.Tokens);
    }

    [Fact]
    public async Task Technician_cannot_create_an_asset()
    {
        var handler = new CreateAssetHandler(
            new FakeCurrentUser(Guid.NewGuid(), UserRole.Technician),
            new FakeSites(),
            new FakeAssets(),
            new FixedClock(Now),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(SampleAsset(), CancellationToken.None));
    }

    [Fact]
    public async Task Technician_cannot_change_another_crews_work_order()
    {
        var owner = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var workOrder = WorkOrder.Open(
            "WO-00003",
            "Replace mechanical seal",
            "Seal is spraying.",
            Guid.NewGuid(),
            Guid.NewGuid(),
            WorkOrderType.Corrective,
            WorkOrderPriority.Urgent,
            owner,
            owner,
            Now.AddDays(1),
            Now);
        var repository = new FakeWorkOrders(workOrder);
        var handler = new TransitionWorkOrderHandler(
            new FakeCurrentUser(stranger, UserRole.Technician),
            repository,
            new FixedClock(Now),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new TransitionWorkOrderCommand(workOrder.Id, WorkOrderStatus.InProgress, null), CancellationToken.None));
    }

    private static CreateAssetCommand SampleAsset() =>
        new(
            Guid.NewGuid(),
            "TR-2000",
            "Spare transformer",
            AssetCategory.Transformer,
            AssetStatus.InService,
            AssetCriticality.High,
            "Helios Electric",
            "HE-230",
            "HE-1",
            new DateOnly(2020, 1, 1),
            "Spare bank.");
}

internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

internal sealed class PlainPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => password;
    public bool Verify(string password, string passwordHash) => password == passwordHash;
}

internal sealed class FakeTokens : ITokenService
{
    public int RefreshTokenDays => 14;
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(UserAccount user) => ("access-token", DateTimeOffset.UnixEpoch);
}

internal sealed class FakeRefreshFactory : IRefreshTokenFactory
{
    private int _count;
    public string GenerateToken() => $"refresh-{++_count}";
    public string Hash(string token) => $"hash:{token}";
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saves++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeUsers(IEnumerable<UserAccount> users) : IUserRepository
{
    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(users.FirstOrDefault(user => user.Email == email));

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(users.FirstOrDefault(user => user.Id == id));

    public Task<IReadOnlyList<Meridian.Application.Contracts.UserSummaryDto>> ListActiveAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}

internal sealed class FakeCurrentUser(Guid userId, UserRole role) : ICurrentUser
{
    public Guid? UserId { get; } = userId;
    public UserRole? Role { get; } = role;
}

internal sealed class FakeSites : ISiteRepository
{
    public Task<Meridian.Domain.Sites.Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Meridian.Application.Contracts.SiteDto>> ListAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}

internal sealed class FakeAssets : IAssetRepository
{
    public Task<Meridian.Domain.Assets.Asset?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Meridian.Domain.Assets.Asset?>(null);
    public Task<bool> TagExistsAsync(string normalizedTag, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(false);
    public Task AddAsync(Meridian.Domain.Assets.Asset asset, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<Meridian.Application.Contracts.PagedResult<Meridian.Application.Contracts.AssetDto>> SearchAsync(Meridian.Application.Contracts.AssetSearch search, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<Meridian.Application.Contracts.AssetDto?> GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
}

internal sealed class FakeRefreshTokens : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];
    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeWorkOrders(WorkOrder workOrder) : IWorkOrderRepository
{
    public Task<WorkOrder?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(id == workOrder.Id ? workOrder : null);

    public Task AddAsync(WorkOrder entity, CancellationToken cancellationToken) => Task.CompletedTask;
    public void StageNewComment(WorkOrderComment comment)
    {
    }
    public Task<string> AllocateNumberAsync(CancellationToken cancellationToken) => Task.FromResult("WO-00011");
    public Task<Meridian.Application.Contracts.PagedResult<Meridian.Application.Contracts.WorkOrderSummaryDto>> SearchAsync(Meridian.Application.Contracts.WorkOrderSearch search, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<Meridian.Application.Contracts.WorkOrderDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
}
