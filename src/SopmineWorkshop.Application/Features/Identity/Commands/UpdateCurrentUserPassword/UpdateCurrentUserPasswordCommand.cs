using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.UpdateCurrentUserPassword;

public sealed record UpdateCurrentUserPasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Result<Updated>>;
