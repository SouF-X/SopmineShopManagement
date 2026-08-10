using MediatR;

using SopmineWorkshop.Application.Features.UnitesMesure.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.UnitesMesure.Commands.CreateUniteMesure;

public sealed record CreateUniteMesureCommand(string Libelle) : IRequest<Result<UniteMesureDto>>;
