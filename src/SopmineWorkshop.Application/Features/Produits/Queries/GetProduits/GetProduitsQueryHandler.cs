using MediatR;

using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Produits.Dtos;
using SopmineWorkshop.Application.Features.Produits.Mappers;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Produits.Queries.GetProduits;

public sealed class GetProduitsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetProduitsQuery, Result<List<ProduitDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<ProduitDto>>> Handle(GetProduitsQuery query, CancellationToken ct)
    {
        var produits = await _context.Produits
            .AsNoTracking()
            .Include(produit => produit.Fournisseur)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);

        return produits.ToDtos();
    }
}
