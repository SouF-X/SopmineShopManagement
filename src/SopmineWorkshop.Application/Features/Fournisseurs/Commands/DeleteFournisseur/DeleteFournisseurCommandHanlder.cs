using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Fournisseurs;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.DeleteFournisseur;

public sealed class RemoveFournisseurCommandHandler(
    ILogger<RemoveFournisseurCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<DeleteFournisseurCommand, Result<Deleted>>
{
    private readonly ILogger<RemoveFournisseurCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Deleted>> Handle(DeleteFournisseurCommand command, CancellationToken ct)
    {
        var fournisseur = await _context.Fournisseurs
            .FirstOrDefaultAsync(f => f.Id == command.FournisseurId, ct);

        if (fournisseur is null)
        {
            _logger.LogWarning("Fournisseur with id {FournisseurId} not found for deletion.", command.FournisseurId);
            return FournisseurErrors.NotFound;
        }

        var isUsedInDocuments = await _context.Invoices
            .AnyAsync(invoice => invoice.FournisseurId == command.FournisseurId, ct);

        if (isUsedInDocuments)
        {
            _logger.LogWarning("Fournisseur {FournisseurId} cannot be deleted because it is used by invoices.", command.FournisseurId);
            return FournisseurErrors.InUseByDocuments;
        }

        var isUsedByProducts = await _context.Produits
            .AnyAsync(produit => produit.FournisseurId == command.FournisseurId, ct);

        if (isUsedByProducts)
        {
            _logger.LogWarning("Fournisseur {FournisseurId} cannot be deleted because it is linked to products.", command.FournisseurId);
            return FournisseurErrors.InUseByProducts;
        }

        _context.Fournisseurs.Remove(fournisseur);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("fournisseurs", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Fournisseur {FournisseurId} deleted successfully.", command.FournisseurId);

        return Result.Deleted;
    }
}
