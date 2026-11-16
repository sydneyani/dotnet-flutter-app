using MediatR;
using Meridian.Application.Contracts;
using Meridian.Application.Sites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sites")]
public sealed class SitesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new ListSitesQuery(), cancellationToken));
    }
}
