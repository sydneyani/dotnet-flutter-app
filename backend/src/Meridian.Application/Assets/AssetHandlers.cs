using FluentValidation;
using MediatR;
using Meridian.Application.Abstractions;
using Meridian.Application.Authorization;
using Meridian.Application.Common;
using Meridian.Application.Contracts;

namespace Meridian.Application.Assets;

public sealed record SearchAssetsQuery(
    string? Search,
    Guid? SiteId,
    AssetStatus? Status,
    AssetCriticality? Criticality,
    AssetCategory? Category,
    int Page = 1,
    int PageSize = PageRequest.DefaultSize) : IRequest<PagedResult<AssetDto>>;

public sealed class SearchAssetsHandler(ICurrentUser current, IAssetRepository assets)
    : IRequestHandler<SearchAssetsQuery, PagedResult<AssetDto>>
{
    public Task<PagedResult<AssetDto>> Handle(SearchAssetsQuery request, CancellationToken cancellationToken)
    {
        current.Require();
        var (page, pageSize) = PageRequest.Normalize(request.Page, request.PageSize);
        return assets.SearchAsync(
            new AssetSearch(request.Search, request.SiteId, request.Status, request.Criticality, request.Category, page, pageSize),
            cancellationToken);
    }
}

public sealed record GetAssetQuery(Guid Id) : IRequest<AssetDto>;

public sealed class GetAssetHandler(ICurrentUser current, IAssetRepository assets) : IRequestHandler<GetAssetQuery, AssetDto>
{
    public async Task<AssetDto> Handle(GetAssetQuery request, CancellationToken cancellationToken)
    {
        current.Require();
        return await assets.GetAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
    }
}

public sealed record CreateAssetCommand(
    Guid SiteId,
    string AssetTag,
    string Name,
    AssetCategory Category,
    AssetStatus Status,
    AssetCriticality Criticality,
    string Manufacturer,
    string Model,
    string SerialNumber,
    DateOnly InstalledOn,
    string Description) : IRequest<AssetDto>;

public sealed class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommand>
{
    public CreateAssetCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.AssetTag).NotEmpty().MaximumLength(AssetTag.MaxLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(140);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Criticality).IsInEnum();
        RuleFor(x => x.Manufacturer).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(80);
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.InstalledOn).Must(date => date != default).WithMessage("Installed date is required.");
    }
}

public sealed class CreateAssetHandler(
    ICurrentUser current,
    ISiteRepository sites,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateAssetCommand, AssetDto>
{
    public async Task<AssetDto> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        var (_, role) = current.Require();
        EnsureCanManage(role);

        _ = await sites.GetByIdAsync(request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Site was not found.");

        var tag = AssetTag.Normalize(request.AssetTag);
        if (await assets.TagExistsAsync(tag, null, cancellationToken))
        {
            throw new ConflictException("An asset with this tag already exists.");
        }

        var asset = Asset.Register(
            request.SiteId,
            tag,
            request.Name,
            request.Category,
            request.Status,
            request.Criticality,
            request.Manufacturer,
            request.Model,
            request.SerialNumber,
            request.InstalledOn,
            request.Description,
            DateOnly.FromDateTime(clock.UtcNow.UtcDateTime),
            clock.UtcNow);

        await assets.AddAsync(asset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await assets.GetAsync(asset.Id, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
    }

    internal static void EnsureCanManage(UserRole role)
    {
        if (!AccessRules.CanManageAssets(role))
        {
            throw new ForbiddenException("Only supervisors and admins can change the asset register.");
        }
    }
}

public sealed record UpdateAssetCommand(
    Guid Id,
    Guid SiteId,
    string AssetTag,
    string Name,
    AssetCategory Category,
    AssetStatus Status,
    AssetCriticality Criticality,
    string Manufacturer,
    string Model,
    string SerialNumber,
    DateOnly InstalledOn,
    string Description) : IRequest<AssetDto>;

public sealed class UpdateAssetCommandValidator : AbstractValidator<UpdateAssetCommand>
{
    public UpdateAssetCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.AssetTag).NotEmpty().MaximumLength(AssetTag.MaxLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(140);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Criticality).IsInEnum();
        RuleFor(x => x.Manufacturer).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(80);
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.InstalledOn).Must(date => date != default).WithMessage("Installed date is required.");
    }
}

public sealed class UpdateAssetHandler(
    ICurrentUser current,
    ISiteRepository sites,
    IAssetRepository assets,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAssetCommand, AssetDto>
{
    public async Task<AssetDto> Handle(UpdateAssetCommand request, CancellationToken cancellationToken)
    {
        var (_, role) = current.Require();
        CreateAssetHandler.EnsureCanManage(role);

        var asset = await assets.GetTrackedAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
        _ = await sites.GetByIdAsync(request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Site was not found.");

        var tag = AssetTag.Normalize(request.AssetTag);
        if (await assets.TagExistsAsync(tag, asset.Id, cancellationToken))
        {
            throw new ConflictException("An asset with this tag already exists.");
        }

        asset.Retag(tag);
        asset.Revise(
            request.SiteId,
            request.Name,
            request.Category,
            request.Status,
            request.Criticality,
            request.Manufacturer,
            request.Model,
            request.SerialNumber,
            request.InstalledOn,
            request.Description,
            DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await assets.GetAsync(asset.Id, cancellationToken)
            ?? throw new NotFoundException("Asset was not found.");
    }
}
