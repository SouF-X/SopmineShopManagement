using MediatR;

using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.CreateFournisseur;

public sealed record CreateFournisseurCommand(
    string Nom,
    string ICE,
    string Adresse,
    string Ville,
    string TelFix,
    string? SiteWeb,
    string? Email,
    List<CreateContactFournisseurCommand> Contacts
) : IRequest<Result<FournisseurDto>>;
