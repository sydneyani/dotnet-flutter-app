using FluentValidation;
using MediatR;
using Meridian.Application.Abstractions;
using Meridian.Application.Common;
using Meridian.Application.Contracts;

namespace Meridian.Application.Inspections;

public sealed record SearchInspectionsQuery(
    Guid? AssetId,
    Guid? WorkOrderId,
    InspectionResult? Result,
    int Page = 1,
    int PageSize = PageRequest.DefaultSize) : IRequest<PagedResult<InspectionDto>>;

public sealed class SearchInspectionsHandler(ICurrentUser current, IInspectionRepository inspections)
    : IRequestHandler<SearchInspectionsQuery, PagedResult<InspectionDto>>
{
    public Task<PagedResult<InspectionDto>> Handle(SearchInspectionsQuery request, CancellationToken cancellationToken)
    {
        current.Require();
        var (page, pageSize) = PageRequest.Normalize(request.Page, request.PageSize);
        return inspections.SearchAsync(new InspectionSearch(request.AssetId, request.WorkOrderId, request.Result, page, pageSize), cancellationToken);
    }
}

public sealed record GetInspectionQuery(Guid Id) : IRequest<InspectionDto>;

public sealed class GetInspectionHandler(ICurrentUser current, IInspectionRepository inspections)
    : IRequestHandler<GetInspectionQuery, InspectionDto>
{
    public async Task<InspectionDto> Handle(GetInspectionQuery request, CancellationToken cancellationToken)
    {
        current.Require();
        return await inspections.GetAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Inspection was not found.");
    }
}

public sealed record CreateInspectionCommand(
    Guid AssetId,
    Guid? WorkOrderId,
    DateTimeOffset InspectedAt,
    InspectionResult Result,
    string Notes,
    string? Findings,
    DateOnly? NextDueOn) : IRequest<InspectionDto>;

public sealed class CreateInspectionCommandValidator : AbstractValidator<CreateInspectionCommand>
{
    public CreateInspectionCommandValidator(IClock clock)
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Result).IsInEnum();
        RuleFor(x => x.Notes).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Findings).MaximumLength(2000);
        RuleFor(x => x.InspectedAt)
            .Must(timestamp => timestamp != default && timestamp <= clock.UtcNow.AddMinutes(5))
            .WithMessage("Inspection time is required and cannot be in the future.");
    }
}

public sealed class CreateInspectionHandler(
    ICurrentUser current,
    IAssetRepository assets,
    IWorkOrderRepository workOrders,
    IInspectionRepository inspections,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateInspectionCommand, InspectionDto>
{
    public async Task<InspectionDto> Handle(CreateInspectionCommand request, CancellationToken cancellationToken)
    {
        var (userId, _) = current.Require();
        var asset = await assets.GetTrackedAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
        asset.EnsureInspectable();

        if (request.WorkOrderId is Guid workOrderId)
        {
            var workOrder = await workOrders.GetTrackedAsync(workOrderId, cancellationToken)
                ?? throw new NotFoundException("Work order was not found.");
            if (workOrder.AssetId != asset.Id)
            {
                throw new DomainException("The work order belongs to a different asset.");
            }
        }

        var inspection = Inspection.Record(
            asset.Id,
            request.WorkOrderId,
            userId,
            request.InspectedAt,
            request.Result,
            request.Notes,
            request.Findings,
            request.NextDueOn,
            clock.UtcNow);

        await inspections.AddAsync(inspection, cancellationToken);
        asset.RecordInspection(inspection.InspectedAt);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await inspections.GetAsync(inspection.Id, cancellationToken)
            ?? throw new NotFoundException("Inspection was not found.");
    }
}
