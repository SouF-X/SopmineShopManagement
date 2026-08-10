using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Clients.Commands.DeleteClient;

public sealed record DeleteClientCommand(Guid ClientId) : IRequest<Result<Deleted>>;
