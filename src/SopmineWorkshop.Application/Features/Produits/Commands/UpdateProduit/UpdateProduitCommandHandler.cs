using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Produits.Commands.UpdateProduit;

public sealed class UpdateProduitCommandHandler(
    ILogger<UpdateProduitCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<UpdateProduitCommand, Result<Updated>>
{
    private readonly ILogger<UpdateProduitCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Updated>> Handle(UpdateProduitCommand command, CancellationToken ct)
    {
        var produit = await _context.Produits
            .FirstOrDefaultAsync(p => p.Id == command.ProduitId, ct);

        if (produit is null)
        {
            _logger.LogWarning("Produit {ProduitId} not found for update", command.ProduitId);
            return ProduitErrors.NotFound;
        }

        var normalizedReference = Normalize(command.Reference).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedReference) &&
            await _context.Produits.AnyAsync(
                p => p.Id != command.ProduitId &&
                     p.Reference != null &&
                     p.Reference.ToLower() == normalizedReference,
                ct);

        if (exists)
        {
            _logger.LogWarning("Produit update aborted. Reference already exists.");
            return ProduitErrors.AlreadyExists;
        }

        if (command.FournisseurId.HasValue)
        {
            var fournisseurExists = await _context.Fournisseurs.AnyAsync(
                fournisseur => fournisseur.Id == command.FournisseurId.Value,
                ct);

            if (!fournisseurExists)
            {
                _logger.LogWarning("Produit update aborted. Fournisseur {FournisseurId} not found.", command.FournisseurId.Value);
                return FournisseurErrors.NotFound;
            }
        }

        var updateResult = produit.Update(
            Normalize(command.Reference),
            Normalize(command.Nom),
            Normalize(command.Famille),
            Normalize(command.Unite),
            command.FournisseurId,
            command.ImageUrl,
            command.Quantite,
            command.QuantiteMini,
            command.PuAchatHT,
            command.TVA,
            command.Marge,
            command.PVenteTTC);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("produits", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Produit {ProduitId} updated successfully", produit.Id);

        return Result.Updated;
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
