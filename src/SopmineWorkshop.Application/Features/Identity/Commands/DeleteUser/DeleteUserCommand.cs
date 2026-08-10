using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Commands.DeleteUser;

public sealed record DeleteUserCommand(
    string UserId,
    string CurrentUserId) : IRequest<Result<Deleted>>;
