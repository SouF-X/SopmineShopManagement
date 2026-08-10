using MediatR;

using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.UnitesMesure.Dtos;
using SopmineWorkshop.Application.Features.UnitesMesure.Mappers;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.UnitesMesure.Queries.GetUnitesMesure;

public sealed class GetUnitesMesureQueryHandler(IAppDbContext context)
    : IRequestHandler<GetUnitesMesureQuery, Result<List<UniteMesureDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<UniteMesureDto>>> Handle(GetUnitesMesureQuery query, CancellationToken ct)
    {
        var unites = await _context.UnitesMesure
            .AsNoTracking()
            .OrderBy(u => u.Libelle)
            .ToListAsync(ct);

        return unites.ToDtos();
    }
}
