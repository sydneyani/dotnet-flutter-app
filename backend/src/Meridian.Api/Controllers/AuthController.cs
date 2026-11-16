using MediatR;
using Meridian.Application.Auth;
using Meridian.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Meridian.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokensDto>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(command, cancellationToken));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensDto>> Refresh([FromBody] RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(command, cancellationToken));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> Me(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetMeQuery(), cancellationToken));
    }
}
