using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.UnitesMesure.Commands.DeleteUniteMesure;

public sealed record DeleteUniteMesureCommand(Guid UniteMesureId) : IRequest<Result<Deleted>>;
