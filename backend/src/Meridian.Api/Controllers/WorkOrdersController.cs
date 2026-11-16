using MediatR;
using Meridian.Application.Contracts;
using Meridian.Application.WorkOrders;
using Meridian.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/work-orders")]
public sealed class WorkOrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkOrderSummaryDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] WorkOrderStatus? status,
        [FromQuery] WorkOrderPriority? priority,
        [FromQuery] WorkOrderType? type,
        [FromQuery] Guid? assetId,
        [FromQuery] Guid? siteId,
        [FromQuery] Guid? assignedToUserId,
        [FromQuery] bool? overdue,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new SearchWorkOrdersQuery(search, status, priority, type, assetId, siteId, assignedToUserId, overdue, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkOrderDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetWorkOrderQuery(id), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrderDetailDto>> Create([FromBody] CreateWorkOrderCommand command, CancellationToken cancellationToken)
    {
        var created = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkOrderDetailDto>> Update(Guid id, [FromBody] UpdateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(
            new UpdateWorkOrderCommand(
                id,
                request.Title,
                request.Description,
                request.Type,
                request.Priority,
                request.AssignedToUserId,
                request.DueAt,
                request.LaborHours),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/transitions")]
    public async Task<ActionResult<WorkOrderDetailDto>> Transition(Guid id, [FromBody] TransitionWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new TransitionWorkOrderCommand(id, request.Status, request.LaborHours), cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<WorkOrderDetailDto>> Comment(Guid id, [FromBody] AddCommentRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new AddWorkOrderCommentCommand(id, request.Body), cancellationToken);
        return Ok(updated);
    }
}

public sealed record UpdateWorkOrderRequest(
    string Title,
    string Description,
    WorkOrderType Type,
    WorkOrderPriority Priority,
    Guid? AssignedToUserId,
    DateTimeOffset? DueAt,
    decimal? LaborHours);

public sealed record TransitionWorkOrderRequest(WorkOrderStatus Status, decimal? LaborHours);

public sealed record AddCommentRequest(string Body);
