using SopmineWorkshop.Application.Features.UnitesMesure.Dtos;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.UnitesMesure.Mappers;

public static class UniteMesureMapper
{
    public static UniteMesureDto ToDto(this UniteMesure entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new UniteMesureDto
        {
            Id = entity.Id,
            Libelle = entity.Libelle
        };
    }

    public static List<UniteMesureDto> ToDtos(this IEnumerable<UniteMesure> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
