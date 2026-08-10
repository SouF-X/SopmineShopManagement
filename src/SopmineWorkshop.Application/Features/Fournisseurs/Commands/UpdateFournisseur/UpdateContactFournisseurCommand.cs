using SopmineWorkshop.Domain.Enums;

using MediatR;
using SopmineWorkshop.Domain.Common.Results.Abstractions;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.UpdateFournisseur;

public sealed record UpdateContactFournisseurCommand(
    Guid? ContactFournisseurId,
    string Nom,
    string Tel,
    ContactRole Role
) : IRequest<IResult<Updated>>;
