using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Queries.GetFournisseurById;

public sealed record GetFournisseurByIdQuery(Guid FournisseurId) : ICachedQuery<Result<FournisseurDto>>
{
    public string CacheKey => $"fournisseurs:{FournisseurId}";

    public string[] Tags => ["fournisseurs"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
