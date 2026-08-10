using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Produits.Commands.DeleteProduit;

public sealed class DeleteProduitCommandHandler(
    ILogger<DeleteProduitCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<DeleteProduitCommand, Result<Deleted>>
{
    private readonly ILogger<DeleteProduitCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Deleted>> Handle(DeleteProduitCommand command, CancellationToken ct)
    {
        var produit = await _context.Produits
            .FirstOrDefaultAsync(p => p.Id == command.ProduitId, ct);

        if (produit is null)
        {
            _logger.LogWarning("Produit {ProduitId} not found for deletion", command.ProduitId);
            return ProduitErrors.NotFound;
        }

        var isUsedInDocuments = await _context.InvoiceLines
            .AnyAsync(line => line.ProduitId == command.ProduitId, ct);

        if (isUsedInDocuments)
        {
            _logger.LogWarning("Produit {ProduitId} cannot be deleted because it is used by invoice lines.", command.ProduitId);
            return ProduitErrors.InUseByDocuments;
        }

        _context.Produits.Remove(produit);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("produits", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Produit {ProduitId} deleted successfully", produit.Id);

        return Result.Deleted;
    }
}
