using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Domain.Fournisseurs;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Mappers;

public static class FournisseurMapper
{
    public static FournisseurDto ToDto(this Fournisseur entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new FournisseurDto
        {
            FournisseurId = entity.Id,
            CreatedAtUtc = entity.CreatedAtUtc,
            Nom = entity.Nom,
            ICE = entity.ICE,
            Adresse = entity.Adresse,
            Ville = entity.Ville,
            TelFix = entity.TelFix,
            SiteWeb = entity.SiteWeb,
            Email = entity.Email,
            Contacts = entity.Contacts?.Select(c => c.ToDto()).ToList() ?? []
        };
    }

    public static List<FournisseurDto> ToDtos(this IEnumerable<Fournisseur> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
