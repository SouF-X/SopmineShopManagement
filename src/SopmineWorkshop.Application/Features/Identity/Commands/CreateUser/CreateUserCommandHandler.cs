using MediatR;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.CreateUser;

public sealed class CreateUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<CreateUserCommand, Result<UserAccountDto>>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<Result<UserAccountDto>> Handle(CreateUserCommand command, CancellationToken ct)
        => _identityService.CreateUserAsync(command.Email, command.Password, command.Role);
}
