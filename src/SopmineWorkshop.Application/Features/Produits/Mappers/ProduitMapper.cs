using SopmineWorkshop.Application.Features.Produits.Dtos;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Produits.Mappers;

public static class ProduitMapper
{
    public static ProduitDto ToDto(this Produit entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ProduitDto
        {
            ProduitId = entity.Id,
            CreatedAtUtc = entity.CreatedAtUtc,
            Reference = entity.Reference,
            Nom = entity.Nom,
            Famille = entity.Famille,
            Unite = entity.Unite,
            FournisseurId = entity.FournisseurId,
            FournisseurNom = entity.Fournisseur?.Nom,
            ImageUrl = entity.ImageUrl,
            Quantite = entity.Quantite,
            QuantiteMini = entity.QuantiteMini,
            PuAchatHT = entity.PuAchatHT,
            TVA = entity.TVA,
            Marge = entity.Marge,
            PVenteTTC = entity.PVenteTTC
        };
    }

    public static List<ProduitDto> ToDtos(this IEnumerable<Produit> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
