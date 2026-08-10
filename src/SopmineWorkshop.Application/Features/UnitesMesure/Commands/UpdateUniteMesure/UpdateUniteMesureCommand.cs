using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.UnitesMesure.Commands.UpdateUniteMesure;

public sealed record UpdateUniteMesureCommand(Guid UniteMesureId, string Libelle) : IRequest<Result<Updated>>;
