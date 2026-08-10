using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Familles.Commands.DeleteFamille;

public sealed record DeleteFamilleCommand(Guid FamilleId) : IRequest<Result<Deleted>>;
