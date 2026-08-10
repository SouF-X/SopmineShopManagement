using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Familles.Commands.UpdateFamille;

public sealed record UpdateFamilleCommand(Guid FamilleId, string Libelle) : IRequest<Result<Updated>>;
