using MediatR;
using Meridian.Application.Contracts;
using Meridian.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new ListUsersQuery(), cancellationToken));
    }
}
