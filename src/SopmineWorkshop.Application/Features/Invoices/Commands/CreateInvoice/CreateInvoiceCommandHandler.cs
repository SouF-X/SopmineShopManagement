using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Application.Features.Invoices.Mappers;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;
using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.CreateInvoice;

public sealed class CreateInvoiceCommandHandler(
    ILogger<CreateInvoiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IDocumentReferenceGenerator referenceGenerator)
    : IRequestHandler<CreateInvoiceCommand, Result<InvoiceDto>>
{
    private const string DefaultRequiredValue = "A COMPLETER";
    private const string DefaultSupplierPhone = "0000000";
    private const string DefaultProductFamily = "Import facture";
    private const string DefaultProductUnit = "U";

    private readonly ILogger<CreateInvoiceCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;
    private readonly IDocumentReferenceGenerator _referenceGenerator = referenceGenerator;

    public async Task<Result<InvoiceDto>> Handle(CreateInvoiceCommand command, CancellationToken ct)
    {
        var normalizedReference = NormalizeText(command.Reference);
        var catalogueEnabled = command.CatalogueMode || command.Nature != InvoiceNature.Achat;
        var fournisseurId = command.FournisseurId;

        if (fournisseurId.HasValue)
        {
            var fournisseurExists = await _context.Fournisseurs.AnyAsync(
                fournisseur => fournisseur.Id == fournisseurId.Value,
                ct);

            if (!fournisseurExists)
            {
                _logger.LogWarning("Invoice creation aborted. Fournisseur {FournisseurId} not found.", fournisseurId.Value);
                return FournisseurErrors.NotFound;
            }
        }
        else if (catalogueEnabled && command.NewSupplier is not null)
        {
            var fournisseurResult = await ResolveOrCreateFournisseurAsync(command.NewSupplier, ct);
            if (fournisseurResult.IsError)
            {
                return fournisseurResult.Errors;
            }

            fournisseurId = fournisseurResult.Value.Id;
        }

        if (!catalogueEnabled &&
            command.Nature == InvoiceNature.Achat &&
            command.Type == InvoiceType.Facture &&
            !fournisseurId.HasValue)
        {
            return InvoiceErrors.FournisseurRequiredForAchat;
        }

        if (command.ClientId.HasValue)
        {
            var clientExists = await _context.Clients.AnyAsync(
                client => client.Id == command.ClientId.Value,
                ct);

            if (!clientExists)
            {
                _logger.LogWarning("Invoice creation aborted. Client {ClientId} not found.", command.ClientId.Value);
                return ClientErrors.NotFound;
            }
        }

        var invoiceLines = command.Lines ?? [];
        var produitIds = catalogueEnabled
            ? invoiceLines
                .Select(line => line.ProduitId)
                .Where(produitId => produitId.HasValue)
                .Select(produitId => produitId!.Value)
                .Distinct()
                .ToList()
            : [];

        var produitsById = produitIds.Count == 0
            ? new Dictionary<Guid, Produit>()
            : await _context.Produits
                .Where(produit => produitIds.Contains(produit.Id))
                .ToDictionaryAsync(produit => produit.Id, ct);

        if (produitsById.Count != produitIds.Count)
        {
            _logger.LogWarning("Invoice creation aborted. One or more produits were not found.");
            return ProduitErrors.NotFound;
        }

        var productKeys = catalogueEnabled
            ? invoiceLines
                .SelectMany(line => new[] { line.ProductReference, line.ProductName })
                .Select(NormalizeKey)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct()
                .ToList()
            : new List<string>();

        var knownProduits = productKeys.Count == 0
            ? []
            : await _context.Produits
                .Where(produit => productKeys.Contains(produit.Reference.ToLower()) || productKeys.Contains(produit.Nom.ToLower()))
                .ToListAsync(ct);

        foreach (var produit in produitsById.Values)
        {
            if (knownProduits.All(item => item.Id != produit.Id))
            {
                knownProduits.Add(produit);
            }
        }

        var usedReferences = catalogueEnabled
            ? new HashSet<string>(
                await _context.Produits.Select(produit => produit.Reference).ToListAsync(ct),
                StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var invoiceId = Guid.NewGuid();
        List<InvoiceLine> lines = [];

        for (var lineIndex = 0; lineIndex < invoiceLines.Count; lineIndex++)
        {
            var lineCommand = invoiceLines[lineIndex];
            var produit = catalogueEnabled && lineCommand.ProduitId.HasValue
                ? produitsById[lineCommand.ProduitId.Value]
                : catalogueEnabled
                    ? MatchProduit(knownProduits, lineCommand)
                    : null;

            if (produit is null
                && command.Nature == InvoiceNature.Achat
                && catalogueEnabled)
            {
                var produitResult = CreateProduitFromInvoiceLine(
                    lineCommand,
                    lineIndex,
                    normalizedReference,
                    fournisseurId,
                    usedReferences);

                if (produitResult.IsError)
                {
                    return produitResult.Errors;
                }

                produit = produitResult.Value;
                _context.Produits.Add(produit);
                knownProduits.Add(produit);
            }

            var productFamily = string.IsNullOrWhiteSpace(lineCommand.ProductFamily)
                ? produit?.Famille ?? string.Empty
                : lineCommand.ProductFamily!.Trim();
            var createLineResult = command.Nature == InvoiceNature.Vente && lineCommand.PriceTTC.HasValue
                ? InvoiceLine.CreateFromTtc(
                    Guid.NewGuid(),
                    invoiceId,
                    produit?.Id,
                    produit?.Reference ?? lineCommand.ProductReference ?? string.Empty,
                    produit?.Nom ?? lineCommand.ProductName ?? string.Empty,
                    productFamily,
                    produit?.Unite ?? lineCommand.ProductUnit ?? string.Empty,
                    lineCommand.Quantity,
                    lineCommand.PriceTTC.Value,
                    lineCommand.TVA,
                    lineIndex + 1)
                : InvoiceLine.Create(
                    Guid.NewGuid(),
                    invoiceId,
                    produit?.Id,
                    produit?.Reference ?? lineCommand.ProductReference ?? string.Empty,
                    produit?.Nom ?? lineCommand.ProductName ?? string.Empty,
                    productFamily,
                    produit?.Unite ?? lineCommand.ProductUnit ?? string.Empty,
                    lineCommand.Quantity,
                    lineCommand.Price,
                    lineCommand.TVA,
                    lineIndex + 1);

            if (createLineResult.IsError)
            {
                return createLineResult.Errors;
            }

            lines.Add(createLineResult.Value);
        }

        var createInvoiceResult = Invoice.Create(
            invoiceId,
            normalizedReference,
            command.Type,
            command.Nature,
            command.Date,
            fournisseurId,
            command.ClientId,
            command.Total,
            lines,
            command.Status ?? InvoiceStatus.Draft,
            null,
            null,
            null,
            command.DueDate,
            command.Notes);

        if (createInvoiceResult.IsError)
        {
            return createInvoiceResult.Errors;
        }

        var invoice = createInvoiceResult.Value;
        if (string.IsNullOrWhiteSpace(normalizedReference))
        {
            normalizedReference = await _referenceGenerator.GenerateAsync(command.Nature, command.Type, command.Date, ct);
            var assignReferenceResult = invoice.AssignReference(normalizedReference);
            if (assignReferenceResult.IsError) return assignReferenceResult.Errors;
        }
        else if (await _context.Invoices.AnyAsync(item => item.Reference == normalizedReference, ct))
        {
            normalizedReference = await _referenceGenerator.GenerateAsync(command.Nature, command.Type, command.Date, ct);
            var assignReferenceResult = invoice.AssignReference(normalizedReference);
            if (assignReferenceResult.IsError) return assignReferenceResult.Errors;
        }

        _context.Invoices.Add(invoice);

        var stockMovementResult = await InvoiceStockMovement.ApplyDeltaAsync(
            _context,
            new Dictionary<Guid, decimal>(),
            InvoiceStockMovement.Capture(invoice),
            ct);

        if (stockMovementResult.IsError)
        {
            return stockMovementResult.Errors;
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _context.Invoices.AnyAsync(item => item.Reference == normalizedReference, ct))
            {
                return InvoiceErrors.ReferenceAlreadyExists;
            }
            throw;
        }

        await _cache.RemoveByTagAsync("invoices", ct);
        if (catalogueEnabled)
        {
            await _cache.RemoveByTagAsync("produits", ct);
            await _cache.RemoveByTagAsync("fournisseurs", ct);
        }

        _logger.LogInformation(
            "Invoice created successfully. Id: {InvoiceId}, CatalogueEnabled: {CatalogueEnabled}",
            invoice.Id,
            catalogueEnabled);
        return invoice.ToDto();
    }

    private async Task<Result<Fournisseur>> ResolveOrCreateFournisseurAsync(
        CreateInvoiceSupplierCommand supplier,
        CancellationToken ct)
    {
        var normalizedIce = NormalizeKey(supplier.ICE);
        var normalizedName = NormalizeKey(supplier.Name);
        Fournisseur? existing = null;

        if (!string.IsNullOrWhiteSpace(normalizedIce))
        {
            existing = await _context.Fournisseurs.FirstOrDefaultAsync(
                item => item.ICE != null && item.ICE.ToLower() == normalizedIce,
                ct);
        }

        if (existing is null && !string.IsNullOrWhiteSpace(normalizedName))
        {
            existing = await _context.Fournisseurs.FirstOrDefaultAsync(
                item => item.Nom != null && item.Nom.ToLower() == normalizedName,
                ct);
        }

        if (existing is not null)
        {
            return existing;
        }

        var fournisseurId = Guid.NewGuid();
        var name = Truncate(string.IsNullOrWhiteSpace(supplier.Name) ? "Fournisseur extrait" : supplier.Name.Trim(), 150);
        var phone = Required(supplier.Phone, DefaultSupplierPhone, 20);
        var contactResult = ContactFournisseur.Create(
            Guid.NewGuid(),
            fournisseurId,
            Truncate(name, 100),
            phone,
            ContactRole.Commercial);

        if (contactResult.IsError)
        {
            return contactResult.Errors;
        }

        var fournisseurResult = Fournisseur.Create(
            fournisseurId,
            name,
            Required(supplier.ICE, DefaultRequiredValue, 50),
            Required(supplier.Address, DefaultRequiredValue, 250),
            Required(supplier.City, DefaultRequiredValue, 100),
            phone,
            Optional(supplier.Website, 200),
            Optional(supplier.Email, 100),
            [contactResult.Value]);

        if (fournisseurResult.IsError)
        {
            return fournisseurResult.Errors;
        }

        _context.Fournisseurs.Add(fournisseurResult.Value);
        return fournisseurResult.Value;
    }

    private static Produit? MatchProduit(List<Produit> produits, CreateInvoiceLineCommand line)
    {
        var reference = NormalizeKey(line.ProductReference);
        var name = NormalizeKey(line.ProductName);
        return produits.FirstOrDefault(item => !string.IsNullOrWhiteSpace(reference) && NormalizeKey(item.Reference) == reference)
            ?? produits.FirstOrDefault(item => !string.IsNullOrWhiteSpace(name) && NormalizeKey(item.Nom) == name);
    }

    private static Result<Produit> CreateProduitFromInvoiceLine(
        CreateInvoiceLineCommand line,
        int lineIndex,
        string invoiceReference,
        Guid? fournisseurId,
        HashSet<string> usedReferences)
    {
        var baseReference = string.IsNullOrWhiteSpace(line.ProductReference)
            ? $"AI-{(string.IsNullOrWhiteSpace(invoiceReference) ? DateTime.Now.ToString("yyyyMMddHHmm") : invoiceReference)}-{lineIndex + 1}"
            : line.ProductReference.Trim();
        var reference = UniqueReference(baseReference, usedReferences);
        var name = Truncate(string.IsNullOrWhiteSpace(line.ProductName) ? $"Produit extrait {lineIndex + 1}" : line.ProductName.Trim(), 150);
        var family = string.IsNullOrWhiteSpace(line.ProductFamily) ? DefaultProductFamily : line.ProductFamily.Trim();
        var unit = string.IsNullOrWhiteSpace(line.ProductUnit) ? DefaultProductUnit : line.ProductUnit.Trim();
        var purchasePrice = Math.Max(0, line.Price);
        var tva = Math.Max(0, line.TVA);
        var saleTtc = line.PriceTTC.HasValue && line.PriceTTC.Value >= 0
            ? line.PriceTTC.Value
            : decimal.Round(purchasePrice * (1 + tva / 100), 2);

        return Produit.Create(
            Guid.NewGuid(),
            reference,
            name,
            family,
            unit,
            fournisseurId,
            null,
            0,
            0,
            purchasePrice,
            tva,
            0,
            saleTtc);
    }

    private static string UniqueReference(string baseReference, HashSet<string> usedReferences)
    {
        baseReference = Truncate(baseReference, 50);
        var candidate = baseReference;
        var suffix = 2;
        while (usedReferences.Contains(candidate))
        {
            var suffixText = $"-{suffix++}";
            candidate = $"{Truncate(baseReference, 50 - suffixText.Length)}{suffixText}";
        }
        usedReferences.Add(candidate);
        return candidate;
    }

    private static string Required(string? value, string fallback, int maxLength)
        => Truncate(string.IsNullOrWhiteSpace(value) ? fallback : value.Trim(), maxLength);

    private static string? Optional(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Truncate(value.Trim(), maxLength);

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;

    private static string NormalizeKey(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
