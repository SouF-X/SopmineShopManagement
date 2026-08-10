using SopmineWorkshop.Application.Features.Familles.Dtos;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Familles.Mappers;

public static class FamilleProduitMapper
{
    public static FamilleProduitDto ToDto(this FamilleProduit entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new FamilleProduitDto
        {
            Id = entity.Id,
            Libelle = entity.Libelle
        };
    }

    public static List<FamilleProduitDto> ToDtos(this IEnumerable<FamilleProduit> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
