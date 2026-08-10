using MediatR;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<DeleteUserCommand, Result<Deleted>>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<Result<Deleted>> Handle(DeleteUserCommand command, CancellationToken ct)
        => _identityService.DeleteUserAsync(command.UserId, command.CurrentUserId);
}
