using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.ExtractInvoiceFromImage;

public sealed class ExtractInvoiceFromImageCommandHandler(
    ILogger<ExtractInvoiceFromImageCommandHandler> logger,
    IInvoiceExtractionService invoiceExtractionService,
    IAppDbContext context)
    : IRequestHandler<ExtractInvoiceFromImageCommand, Result<InvoiceExtractionDto>>
{
    private readonly ILogger<ExtractInvoiceFromImageCommandHandler> _logger = logger;
    private readonly IInvoiceExtractionService _invoiceExtractionService = invoiceExtractionService;
    private readonly IAppDbContext _context = context;

    public async Task<Result<InvoiceExtractionDto>> Handle(ExtractInvoiceFromImageCommand command, CancellationToken ct)
    {
        var extractionResult = await _invoiceExtractionService.ExtractFromImageAsync(
            command.ImageBytes,
            command.ContentType,
            command.FileName,
            ct);

        if (extractionResult.IsError)
        {
            return extractionResult.Errors;
        }

        var extraction = extractionResult.Value;
        extraction.Type = command.Type;
        extraction.Nature = InvoiceNature.Achat;
        EnsureReference(extraction);

        if (command.Type == InvoiceType.BonReception)
        {
            await MatchExistingRecordsAsync(extraction, ct);
        }
        else if (command.Type == InvoiceType.Facture)
        {
            await MatchExistingSupplierAsync(extraction, ct);
        }

        _logger.LogInformation(
            "Invoice image extracted for review without database writes. Reference: {Reference}, FournisseurId: {FournisseurId}, Lines: {LineCount}",
            extraction.Reference,
            extraction.FournisseurId,
            extraction.Lines.Count);

        return extraction;
    }

    private async Task MatchExistingRecordsAsync(InvoiceExtractionDto extraction, CancellationToken ct)
    {
        await MatchExistingSupplierAsync(extraction, ct);

        var productKeys = extraction.Lines
            .SelectMany(line => new[] { line.ProductReference, line.Product })
            .Select(Normalize)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct()
            .ToList();

        if (productKeys.Count == 0)
        {
            return;
        }

        var produits = await _context.Produits
            .AsNoTracking()
            .Where(produit => productKeys.Contains(produit.Reference.ToLower()) || productKeys.Contains(produit.Nom.ToLower()))
            .ToListAsync(ct);

        foreach (var line in extraction.Lines)
        {
            var productReference = Normalize(line.ProductReference);
            var productName = Normalize(line.Product);
            var produit = produits.FirstOrDefault(item =>
                    !string.IsNullOrWhiteSpace(productReference)
                    && Normalize(item.Reference) == productReference)
                ?? produits.FirstOrDefault(item =>
                    !string.IsNullOrWhiteSpace(productName)
                    && Normalize(item.Nom) == productName);

            if (produit is not null)
            {
                ApplyProduit(line, produit);
            }
        }
    }

    private async Task MatchExistingSupplierAsync(InvoiceExtractionDto extraction, CancellationToken ct)
    {
        var fournisseur = await FindFournisseurAsync(extraction, ct);
        if (fournisseur is not null)
        {
            ApplyFournisseur(extraction, fournisseur);
        }
    }

    private async Task<Fournisseur?> FindFournisseurAsync(InvoiceExtractionDto extraction, CancellationToken ct)
    {
        var normalizedIce = Normalize(extraction.SupplierICE);
        var normalizedName = Normalize(extraction.SupplierName);

        if (!string.IsNullOrWhiteSpace(normalizedIce))
        {
            var byIce = await _context.Fournisseurs
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ICE != null && item.ICE.ToLower() == normalizedIce, ct);
            if (byIce is not null)
            {
                return byIce;
            }
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        return await _context.Fournisseurs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Nom != null && item.Nom.ToLower() == normalizedName, ct);
    }

    private static void ApplyFournisseur(InvoiceExtractionDto extraction, Fournisseur fournisseur)
    {
        extraction.FournisseurId = fournisseur.Id;
        extraction.SupplierName = fournisseur.Nom ?? extraction.SupplierName;
        extraction.SupplierICE = fournisseur.ICE;
        extraction.SupplierAddress = fournisseur.Adresse;
        extraction.SupplierCity = fournisseur.Ville;
        extraction.SupplierPhone = fournisseur.TelFix;
        extraction.SupplierEmail = fournisseur.Email;
        extraction.SupplierWebsite = fournisseur.SiteWeb;
    }

    private static void ApplyProduit(InvoiceExtractionLineDto line, Produit produit)
    {
        line.ProduitId = produit.Id;
        line.ProductReference = string.IsNullOrWhiteSpace(line.ProductReference) ? produit.Reference : line.ProductReference.Trim();
        line.Product = string.IsNullOrWhiteSpace(line.Product) ? produit.Nom : line.Product.Trim();
        line.ProductFamily = produit.Famille;
        line.ProductUnit = produit.Unite;

        if (line.TVA == 0 && produit.TVA > 0) line.TVA = produit.TVA;
        if (line.UnitPriceHT <= 0 && produit.PuAchatHT > 0) line.UnitPriceHT = produit.PuAchatHT;
        if (line.UnitPrice <= 0 && line.UnitPriceHT > 0) line.UnitPrice = line.UnitPriceHT;
        if (line.Price <= 0 && line.UnitPriceHT > 0) line.Price = line.UnitPriceHT;
    }

    private static void EnsureReference(InvoiceExtractionDto extraction)
    {
        if (!string.IsNullOrWhiteSpace(extraction.Reference))
        {
            extraction.Reference = extraction.Reference.Trim();
            return;
        }

        var prefix = GetReferencePrefix(extraction.Nature, extraction.Type);
        extraction.Reference = $"{prefix}-{DateTime.Now:yyyyMMdd-HHmm}";
    }

    private static string GetReferencePrefix(InvoiceNature nature, InvoiceType type)
        => (nature, type) switch
        {
            (InvoiceNature.Achat, InvoiceType.BonCommande) => "ACH-BC",
            (InvoiceNature.Achat, InvoiceType.BonReception) => "ACH-BR",
            (InvoiceNature.Achat, InvoiceType.Facture) => "ACH-FA",
            (InvoiceNature.Achat, InvoiceType.Avoir) => "ACH-AV",
            (InvoiceNature.Vente, InvoiceType.Devis) => "VEN-DV",
            (InvoiceNature.Vente, InvoiceType.BonLivraison) => "VEN-BL",
            (InvoiceNature.Vente, InvoiceType.Facture) => "VEN-FA",
            (InvoiceNature.Vente, InvoiceType.Avoir) => "VEN-AV",
            _ => "DOC"
        };

    private static string Normalize(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
