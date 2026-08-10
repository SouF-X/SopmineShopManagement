using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Produits.Dtos;
using SopmineWorkshop.Application.Features.Produits.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Produits.Commands.CreateProduit;

public sealed class CreateProduitCommandHandler(
    ILogger<CreateProduitCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<CreateProduitCommand, Result<ProduitDto>>
{
    private readonly ILogger<CreateProduitCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<ProduitDto>> Handle(CreateProduitCommand command, CancellationToken ct)
    {
        var normalizedReference = Normalize(command.Reference).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedReference) &&
            await _context.Produits.AnyAsync(
                p => p.Reference != null && p.Reference.ToLower() == normalizedReference,
                ct);

        if (exists)
        {
            _logger.LogWarning("Produit creation aborted. Reference already exists.");

            return ProduitErrors.AlreadyExists;
        }

        if (command.FournisseurId.HasValue)
        {
            var fournisseurExists = await _context.Fournisseurs.AnyAsync(
                fournisseur => fournisseur.Id == command.FournisseurId.Value,
                ct);

            if (!fournisseurExists)
            {
                _logger.LogWarning("Produit creation aborted. Fournisseur {FournisseurId} not found.", command.FournisseurId.Value);
                return FournisseurErrors.NotFound;
            }
        }

        var produitId = Guid.NewGuid();

        var createResult = Produit.Create(
            produitId,
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

        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var produit = createResult.Value;

        _context.Produits.Add(produit);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("produits", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Produit created successfully. Id: {ProduitId}", produit.Id);

        return produit.ToDto();
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
