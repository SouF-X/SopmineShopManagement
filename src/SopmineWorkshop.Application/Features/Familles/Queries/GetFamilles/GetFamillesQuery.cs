using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Familles.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Familles.Queries.GetFamilles;

public sealed record GetFamillesQuery : ICachedQuery<Result<List<FamilleProduitDto>>>
{
    public string CacheKey => "familles";

    public string[] Tags => ["familles"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
