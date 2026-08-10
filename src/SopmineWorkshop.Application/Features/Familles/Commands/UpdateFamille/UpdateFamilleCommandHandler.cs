using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Familles.Commands.UpdateFamille;

public sealed class UpdateFamilleCommandHandler(
    ILogger<UpdateFamilleCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<UpdateFamilleCommand, Result<Updated>>
{
    private readonly ILogger<UpdateFamilleCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Updated>> Handle(UpdateFamilleCommand command, CancellationToken ct)
    {
        var famille = await _context.FamillesProduit
            .FirstOrDefaultAsync(f => f.Id == command.FamilleId, ct);

        if (famille is null)
        {
            _logger.LogWarning("Famille {FamilleId} not found for update.", command.FamilleId);
            return FamilleProduitErrors.NotFound;
        }

        var normalizedLibelle = Normalize(command.Libelle).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedLibelle) &&
            await _context.FamillesProduit.AnyAsync(
                f => f.Id != command.FamilleId &&
                     f.Libelle != null &&
                     f.Libelle.ToLower() == normalizedLibelle,
                ct);

        if (exists)
        {
            _logger.LogWarning("Famille update aborted. Libelle already exists.");
            return FamilleProduitErrors.AlreadyExists;
        }

        var oldLibelle = famille.Libelle;
        var renameResult = famille.Rename(Normalize(command.Libelle));

        if (renameResult.IsError)
        {
            return renameResult.Errors;
        }

        var normalizedOldLibelle = Normalize(oldLibelle).ToLower();
        var produits = await _context.Produits
            .Where(p => p.Famille != null && p.Famille.ToLower() == normalizedOldLibelle)
            .ToListAsync(ct);

        foreach (var produit in produits)
        {
            var updateResult = produit.Update(
                produit.Reference,
                produit.Nom,
                famille.Libelle,
                produit.Unite,
                produit.FournisseurId,
                produit.ImageUrl,
                produit.Quantite,
                produit.QuantiteMini,
                produit.PuAchatHT,
                produit.TVA,
                produit.Marge,
                produit.PVenteTTC);

            if (updateResult.IsError)
            {
                return updateResult.Errors;
            }
        }

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("familles", ct);
        await _cache.RemoveByTagAsync("produits", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Famille {FamilleId} updated successfully.", famille.Id);

        return Result.Updated;
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
