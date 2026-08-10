using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.UnitesMesure.Commands.UpdateUniteMesure;

public sealed class UpdateUniteMesureCommandHandler(
    ILogger<UpdateUniteMesureCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<UpdateUniteMesureCommand, Result<Updated>>
{
    private readonly ILogger<UpdateUniteMesureCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Updated>> Handle(UpdateUniteMesureCommand command, CancellationToken ct)
    {
        var unite = await _context.UnitesMesure
            .FirstOrDefaultAsync(u => u.Id == command.UniteMesureId, ct);

        if (unite is null)
        {
            _logger.LogWarning("Unite de mesure {UniteMesureId} not found for update.", command.UniteMesureId);
            return UniteMesureErrors.NotFound;
        }

        var normalizedLibelle = Normalize(command.Libelle).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedLibelle) &&
            await _context.UnitesMesure.AnyAsync(
                u => u.Id != command.UniteMesureId &&
                     u.Libelle != null &&
                     u.Libelle.ToLower() == normalizedLibelle,
                ct);

        if (exists)
        {
            _logger.LogWarning("Unite de mesure update aborted. Libelle already exists.");
            return UniteMesureErrors.AlreadyExists;
        }

        var oldLibelle = unite.Libelle;
        var renameResult = unite.Rename(Normalize(command.Libelle));

        if (renameResult.IsError)
        {
            return renameResult.Errors;
        }

        var normalizedOldLibelle = Normalize(oldLibelle).ToLower();
        var produits = await _context.Produits
            .Where(p => p.Unite != null && p.Unite.ToLower() == normalizedOldLibelle)
            .ToListAsync(ct);

        foreach (var produit in produits)
        {
            var updateResult = produit.Update(
                produit.Reference,
                produit.Nom,
                produit.Famille,
                unite.Libelle,
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

        await _cache.RemoveByTagAsync("unites-mesure", ct);
        await _cache.RemoveByTagAsync("produits", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Unite de mesure {UniteMesureId} updated successfully.", unite.Id);

        return Result.Updated;
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
