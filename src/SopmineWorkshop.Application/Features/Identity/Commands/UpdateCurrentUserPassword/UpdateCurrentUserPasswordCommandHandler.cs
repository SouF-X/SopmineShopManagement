using MediatR;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.UpdateCurrentUserPassword;

public sealed class UpdateCurrentUserPasswordCommandHandler(IIdentityService identityService)
    : IRequestHandler<UpdateCurrentUserPasswordCommand, Result<Updated>>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<Result<Updated>> Handle(UpdateCurrentUserPasswordCommand command, CancellationToken ct)
        => _identityService.UpdatePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword);
}
