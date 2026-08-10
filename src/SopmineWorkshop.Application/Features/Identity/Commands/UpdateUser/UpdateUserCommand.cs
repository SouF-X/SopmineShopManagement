using MediatR;

using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    string UserId,
    string Email,
    string Role) : IRequest<Result<UserAccountDto>>;
