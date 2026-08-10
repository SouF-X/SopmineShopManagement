using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Asp.Versioning;
using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SopmineWorkshop.Application.Features.Identity.Commands.CreateUser;
using SopmineWorkshop.Application.Features.Identity.Commands.DeleteUser;
using SopmineWorkshop.Application.Features.Identity.Commands.ResetUserPassword;
using SopmineWorkshop.Application.Features.Identity.Commands.UpdateCurrentUserPassword;
using SopmineWorkshop.Application.Features.Identity.Commands.UpdateUser;
using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Application.Features.Identity.Queries.GetCurrentUser;
using SopmineWorkshop.Application.Features.Identity.Queries.GetUsers;
using SopmineWorkshop.Contracts.Requests.Identity;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Identity;

namespace SopmineWorkshop.API.Controllers;

[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
public sealed class UsersController(ISender sender) : ApiController
{
    [HttpGet("me")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(UserAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Problem([Error.Unauthorized("Users.Unauthorized", "User session is invalid.")]);
        }

        var result = await sender.Send(new GetCurrentUserQuery(currentUserId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPut("me/password")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCurrentPassword(
        [FromBody] UpdateCurrentUserPasswordRequest request,
        CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Problem([Error.Unauthorized("Users.Unauthorized", "User session is invalid.")]);
        }

        var result = await sender.Send(
            new UpdateCurrentUserPasswordCommand(
                currentUserId,
                request.CurrentPassword,
                request.NewPassword),
            ct);

        return result.Match(
            _ => Ok(),
            Problem);
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(List<UserAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetUsersQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(UserAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateUserCommand(request.Email, request.Password, request.Role),
            ct);

        return result.Match(
            response => Created("/api/v1/users", response),
            Problem);
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPut("{userId}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(UserAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        string userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateUserCommand(userId, request.Email, request.Role),
            ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPut("{userId}/password")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        string userId,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new ResetUserPasswordCommand(userId, request.NewPassword),
            ct);

        return result.Match(
            _ => Ok(),
            Problem);
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpDelete("{userId}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string userId, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Problem([Error.Unauthorized("Users.Unauthorized", "User session is invalid.")]);
        }

        var result = await sender.Send(new DeleteUserCommand(userId, currentUserId), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    private string? GetCurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue("sub");
}
