using FluentValidation;
using MediatR;
using Meridian.Application.Abstractions;
using Meridian.Application.Authorization;
using Meridian.Application.Common;
using Meridian.Application.Contracts;

namespace Meridian.Application.WorkOrders;

public sealed record SearchWorkOrdersQuery(
    string? Search,
    WorkOrderStatus? Status,
    WorkOrderPriority? Priority,
    WorkOrderType? Type,
    Guid? AssetId,
    Guid? SiteId,
    Guid? AssignedToUserId,
    bool? Overdue,
    int Page = 1,
    int PageSize = PageRequest.DefaultSize) : IRequest<PagedResult<WorkOrderSummaryDto>>;

public sealed class SearchWorkOrdersHandler(ICurrentUser current, IWorkOrderRepository workOrders)
    : IRequestHandler<SearchWorkOrdersQuery, PagedResult<WorkOrderSummaryDto>>
{
    public Task<PagedResult<WorkOrderSummaryDto>> Handle(SearchWorkOrdersQuery request, CancellationToken cancellationToken)
    {
        current.Require();
        var (page, pageSize) = PageRequest.Normalize(request.Page, request.PageSize);
        return workOrders.SearchAsync(
            new WorkOrderSearch(
                request.Search,
                request.Status,
                request.Priority,
                request.Type,
                request.AssetId,
                request.SiteId,
                request.AssignedToUserId,
                request.Overdue,
                page,
                pageSize),
            cancellationToken);
    }
}

public sealed record GetWorkOrderQuery(Guid Id) : IRequest<WorkOrderDetailDto>;

public sealed class GetWorkOrderHandler(ICurrentUser current, IWorkOrderRepository workOrders)
    : IRequestHandler<GetWorkOrderQuery, WorkOrderDetailDto>
{
    public async Task<WorkOrderDetailDto> Handle(GetWorkOrderQuery request, CancellationToken cancellationToken)
    {
        current.Require();
        return await workOrders.GetAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Work order was not found.");
    }
}

public sealed record CreateWorkOrderCommand(
    string Title,
    string Description,
    Guid AssetId,
    WorkOrderType Type,
    WorkOrderPriority Priority,
    Guid? AssignedToUserId,
    DateTimeOffset? DueAt) : IRequest<WorkOrderDetailDto>;

public sealed class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(140);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public sealed class CreateWorkOrderHandler(
    ICurrentUser current,
    IAssetRepository assets,
    IUserRepository users,
    IWorkOrderRepository workOrders,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateWorkOrderCommand, WorkOrderDetailDto>
{
    public async Task<WorkOrderDetailDto> Handle(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var (userId, role) = current.Require();
        var asset = await assets.GetTrackedAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
        if (asset.Status == AssetStatus.Retired)
        {
            throw new DomainException("Retired assets cannot take new work orders.");
        }

        AccessRules.EnsureCanAssign(role, userId, null, request.AssignedToUserId);
        await EnsureActiveAssigneeAsync(users, request.AssignedToUserId, cancellationToken);

        var number = await workOrders.AllocateNumberAsync(cancellationToken);
        var workOrder = WorkOrder.Open(
            number,
            request.Title,
            request.Description,
            asset.Id,
            asset.SiteId,
            request.Type,
            request.Priority,
            userId,
            request.AssignedToUserId,
            request.DueAt,
            clock.UtcNow);

        await workOrders.AddAsync(workOrder, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await workOrders.GetAsync(workOrder.Id, cancellationToken)
            ?? throw new NotFoundException("Work order was not found.");
    }

    internal static async Task EnsureActiveAssigneeAsync(IUserRepository users, Guid? assigneeId, CancellationToken cancellationToken)
    {
        if (assigneeId is not Guid id)
        {
            return;
        }

        var assignee = await users.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");
        if (!assignee.IsActive)
        {
            throw new DomainException("Assignee is not active.");
        }
    }
}

public sealed record UpdateWorkOrderCommand(
    Guid Id,
    string Title,
    string Description,
    WorkOrderType Type,
    WorkOrderPriority Priority,
    Guid? AssignedToUserId,
    DateTimeOffset? DueAt,
    decimal? LaborHours) : IRequest<WorkOrderDetailDto>;

public sealed class UpdateWorkOrderCommandValidator : AbstractValidator<UpdateWorkOrderCommand>
{
    public UpdateWorkOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(140);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.LaborHours)
            .InclusiveBetween(0m, 999.99m)
            .When(x => x.LaborHours is not null);
    }
}

public sealed class UpdateWorkOrderHandler(
    ICurrentUser current,
    IUserRepository users,
    IWorkOrderRepository workOrders,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateWorkOrderCommand, WorkOrderDetailDto>
{
    public async Task<WorkOrderDetailDto> Handle(UpdateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var (userId, role) = current.Require();
        var workOrder = await workOrders.GetTrackedAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Work order was not found.");
        EnsureCanModify(role, userId, workOrder);
        AccessRules.EnsureCanAssign(role, userId, workOrder.AssignedToUserId, request.AssignedToUserId);
        await CreateWorkOrderHandler.EnsureActiveAssigneeAsync(users, request.AssignedToUserId, cancellationToken);

        workOrder.UpdateDetails(request.Title, request.Description, request.Type, request.Priority, request.DueAt, clock.UtcNow);
        workOrder.Assign(request.AssignedToUserId, clock.UtcNow);
        if (request.LaborHours is decimal hours)
        {
            workOrder.RecordLabor(hours, clock.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await workOrders.GetAsync(workOrder.Id, cancellationToken)
            ?? throw new NotFoundException("Work order was not found.");
    }

    internal static void EnsureCanModify(UserRole role, Guid userId, WorkOrder workOrder)
    {
        if (!AccessRules.CanModifyWorkOrder(role, userId, workOrder))
        {
            throw new ForbiddenException("You can only change work orders you created or are assigned to.");
        }
    }
}

public sealed record TransitionWorkOrderCommand(Guid Id, WorkOrderStatus Status, decimal? LaborHours) : IRequest<WorkOrderDetailDto>;

public sealed class TransitionWorkOrderCommandValidator : AbstractValidator<TransitionWorkOrderCommand>
{
    public TransitionWorkOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.LaborHours)
            .InclusiveBetween(0m, 999.99m)
            .When(x => x.LaborHours is not null);
    }
}

public sealed class TransitionWorkOrderHandler(
    ICurrentUser current,
    IWorkOrderRepository workOrders,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<TransitionWorkOrderCommand, WorkOrderDetailDto>
{
    public async Task<WorkOrderDetailDto> Handle(TransitionWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var (userId, role) = current.Require();
        var workOrder = await workOrders.GetTrackedAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Work order was not found.");
        UpdateWorkOrderHandler.EnsureCanModify(role, userId, workOrder);
        workOrder.TransitionTo(request.Status, clock.UtcNow, request.LaborHours);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await workOrders.GetAsync(workOrder.Id, cancellationToken)
            ?? throw new NotFoundException("Work order was not found.");
    }
}

public sealed record AddWorkOrderCommentCommand(Guid WorkOrderId, string Body) : IRequest<WorkOrderDetailDto>;

public sealed class AddWorkOrderCommentCommandValidator : AbstractValidator<AddWorkOrderCommentCommand>
{
    public AddWorkOrderCommentCommandValidator()
    {
        RuleFor(x => x.WorkOrderId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public sealed class AddWorkOrderCommentHandler(
    ICurrentUser current,
    IWorkOrderRepository workOrders,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<AddWorkOrderCommentCommand, WorkOrderDetailDto>
{
    public async Task<WorkOrderDetailDto> Handle(AddWorkOrderCommentCommand request, CancellationToken cancellationToken)
    {
        var (userId, _) = current.Require();
        var workOrder = await workOrders.GetTrackedAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException("Work order was not found.");

        var comment = workOrder.AddComment(userId, request.Body, clock.UtcNow);
        workOrders.StageNewComment(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await workOrders.GetAsync(workOrder.Id, cancellationToken)
            ?? throw new NotFoundException("Work order was not found.");
    }
}
