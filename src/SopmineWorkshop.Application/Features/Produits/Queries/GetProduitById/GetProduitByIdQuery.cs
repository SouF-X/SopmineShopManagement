using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Produits.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Produits.Queries.GetProduitById;

public sealed record GetProduitByIdQuery(Guid ProduitId) : ICachedQuery<Result<ProduitDto>>
{
    public string CacheKey => $"produits:{ProduitId}";

    public string[] Tags => ["produits"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
