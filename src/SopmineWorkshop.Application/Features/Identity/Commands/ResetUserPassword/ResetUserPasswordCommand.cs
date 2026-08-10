using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.ResetUserPassword;

public sealed record ResetUserPasswordCommand(
    string UserId,
    string NewPassword) : IRequest<Result<Updated>>;
