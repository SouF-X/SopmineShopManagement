using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.UpdateFournisseur;

public sealed record UpdateFournisseurCommand(
    Guid FournisseurId,
    string Nom,
    string ICE,
    string Adresse,
    string Ville,
    string TelFix,
    string? SiteWeb,
    string? Email,
    List<UpdateContactFournisseurCommand> Contacts
) : IRequest<Result<Updated>>;
