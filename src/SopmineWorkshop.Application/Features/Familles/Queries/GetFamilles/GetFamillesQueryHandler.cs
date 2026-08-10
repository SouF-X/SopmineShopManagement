using MediatR;

using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Familles.Dtos;
using SopmineWorkshop.Application.Features.Familles.Mappers;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Familles.Queries.GetFamilles;

public sealed class GetFamillesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetFamillesQuery, Result<List<FamilleProduitDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<FamilleProduitDto>>> Handle(GetFamillesQuery query, CancellationToken ct)
    {
        var familles = await _context.FamillesProduit
            .AsNoTracking()
            .OrderBy(f => f.Libelle)
            .ToListAsync(ct);

        return familles.ToDtos();
    }
}
