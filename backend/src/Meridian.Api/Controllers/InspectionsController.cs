using MediatR;
using Meridian.Application.Contracts;
using Meridian.Application.Inspections;
using Meridian.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inspections")]
public sealed class InspectionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InspectionDto>>> Search(
        [FromQuery] Guid? assetId,
        [FromQuery] Guid? workOrderId,
        [FromQuery] InspectionResult? result,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var inspections = await sender.Send(new SearchInspectionsQuery(assetId, workOrderId, result, page, pageSize), cancellationToken);
        return Ok(inspections);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InspectionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetInspectionQuery(id), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<InspectionDto>> Create([FromBody] CreateInspectionCommand command, CancellationToken cancellationToken)
    {
        var created = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
}
