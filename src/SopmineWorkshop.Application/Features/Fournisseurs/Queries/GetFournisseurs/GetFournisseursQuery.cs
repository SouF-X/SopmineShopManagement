using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Queries.GetFournisseurs;

public sealed record GetFournisseursQuery : ICachedQuery<Result<List<FournisseurDto>>>
{
    public string CacheKey => "fournisseurs";

    public string[] Tags => ["fournisseurs"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
