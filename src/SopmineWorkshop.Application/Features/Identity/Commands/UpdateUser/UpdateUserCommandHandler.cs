using MediatR;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<UpdateUserCommand, Result<UserAccountDto>>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<Result<UserAccountDto>> Handle(UpdateUserCommand command, CancellationToken ct)
        => _identityService.UpdateUserAsync(command.UserId, command.Email, command.Role);
}
