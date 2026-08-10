using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.UnitesMesure.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.UnitesMesure.Queries.GetUnitesMesure;

public sealed record GetUnitesMesureQuery : ICachedQuery<Result<List<UniteMesureDto>>>
{
    public string CacheKey => "unites-mesure";

    public string[] Tags => ["unites-mesure"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
