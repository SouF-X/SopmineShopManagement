using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;


namespace SopmineWorkshop.Application.Features.Fournisseurs.Mappers;

public static class ContactFournisseurMapper
{
    public static ContactFournisseurDto ToDto(this ContactFournisseur entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ContactFournisseurDto
        {
            ContactFournisseurId = entity.Id,
            Nom = entity.Nom,
            Tel = entity.Tel,
            Role = entity.Role
        };
    }

    public static List<ContactFournisseurDto> ToDtos(this IEnumerable<ContactFournisseur> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
