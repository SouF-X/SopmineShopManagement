using MediatR;

using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string Role) : IRequest<Result<UserAccountDto>>;
