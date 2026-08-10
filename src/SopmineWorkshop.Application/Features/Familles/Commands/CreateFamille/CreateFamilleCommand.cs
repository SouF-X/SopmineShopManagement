using MediatR;

using SopmineWorkshop.Application.Features.Familles.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Familles.Commands.CreateFamille;

public sealed record CreateFamilleCommand(string Libelle) : IRequest<Result<FamilleProduitDto>>;
