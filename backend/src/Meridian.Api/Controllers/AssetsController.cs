using MediatR;
using Meridian.Api;
using Meridian.Application.Assets;
using Meridian.Application.Contracts;
using Meridian.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assets")]
public sealed class AssetsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AssetDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] Guid? siteId,
        [FromQuery] AssetStatus? status,
        [FromQuery] AssetCriticality? criticality,
        [FromQuery] AssetCategory? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchAssetsQuery(search, siteId, status, criticality, category, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetAssetQuery(id), cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageAssets)]
    public async Task<ActionResult<AssetDto>> Create([FromBody] CreateAssetCommand command, CancellationToken cancellationToken)
    {
        var created = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageAssets)]
    public async Task<ActionResult<AssetDto>> Update(Guid id, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(
            new UpdateAssetCommand(
                id,
                request.SiteId,
                request.AssetTag,
                request.Name,
                request.Category,
                request.Status,
                request.Criticality,
                request.Manufacturer,
                request.Model,
                request.SerialNumber,
                request.InstalledOn,
                request.Description),
            cancellationToken);
        return Ok(updated);
    }
}

public sealed record UpdateAssetRequest(
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
    string Description);
