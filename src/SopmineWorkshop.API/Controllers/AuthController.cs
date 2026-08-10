using Asp.Versioning;
using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Application.Features.Identity;
using SopmineWorkshop.Application.Features.Identity.Queries.GenerateTokens;
using SopmineWorkshop.Contracts.Requests.Auth;

namespace SopmineWorkshop.API.Controllers;

[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public sealed class AuthController(ISender sender) : ApiController
{
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitPolicies.Login)]
    [HttpPost("login")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GenerateTokenQuery(request.Email, request.Password), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}
