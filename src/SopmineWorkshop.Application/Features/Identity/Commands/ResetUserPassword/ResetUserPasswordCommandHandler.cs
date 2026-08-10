using MediatR;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.ResetUserPassword;

public sealed class ResetUserPasswordCommandHandler(IIdentityService identityService)
    : IRequestHandler<ResetUserPasswordCommand, Result<Updated>>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<Result<Updated>> Handle(ResetUserPasswordCommand command, CancellationToken ct)
        => _identityService.ResetPasswordAsync(command.UserId, command.NewPassword);
}
