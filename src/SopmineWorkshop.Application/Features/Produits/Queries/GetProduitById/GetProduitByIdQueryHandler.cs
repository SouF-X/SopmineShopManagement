using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Produits.Dtos;
using SopmineWorkshop.Application.Features.Produits.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Produits.Queries.GetProduitById;

public sealed class GetProduitByIdQueryHandler(
    ILogger<GetProduitByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetProduitByIdQuery, Result<ProduitDto>>
{
    private readonly ILogger<GetProduitByIdQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<ProduitDto>> Handle(GetProduitByIdQuery query, CancellationToken ct)
    {
        var produit = await _context.Produits
            .AsNoTracking()
            .Include(p => p.Fournisseur)
            .FirstOrDefaultAsync(p => p.Id == query.ProduitId, ct);

        if (produit is null)
        {
            _logger.LogWarning("Produit {ProduitId} not found", query.ProduitId);
            return ProduitErrors.NotFound;
        }

        return produit.ToDto();
    }
}
