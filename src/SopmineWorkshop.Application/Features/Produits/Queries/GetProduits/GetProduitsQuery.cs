using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Produits.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Produits.Queries.GetProduits;

public sealed record GetProduitsQuery : ICachedQuery<Result<List<ProduitDto>>>
{
    public string CacheKey => "produits";

    public string[] Tags => ["produits"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
