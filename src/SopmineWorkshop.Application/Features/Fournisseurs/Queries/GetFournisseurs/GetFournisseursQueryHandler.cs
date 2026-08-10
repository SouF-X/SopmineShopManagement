using MediatR;

using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Application.Features.Fournisseurs.Mappers;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Queries.GetFournisseurs;

public sealed class GetFournisseursQueryHandler(IAppDbContext context)
    : IRequestHandler<GetFournisseursQuery, Result<List<FournisseurDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<FournisseurDto>>> Handle(GetFournisseursQuery query, CancellationToken ct)
    {
        var fournisseurs = await _context.Fournisseurs
            .Include(f => f.Contacts)
            .AsNoTracking()
            .OrderByDescending(f => f.CreatedAtUtc)
            .ThenByDescending(f => f.Id)
            .ToListAsync(ct);

        return fournisseurs.ToDtos();
    }
}
