using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Invoices;
using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Application.Features.Invoices.Mappers;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.UpdateInvoice;

public sealed class UpdateInvoiceCommandHandler(
    ILogger<UpdateInvoiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<UpdateInvoiceCommand, Result<InvoiceDto>>
{
    private readonly ILogger<UpdateInvoiceCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<InvoiceDto>> Handle(UpdateInvoiceCommand command, CancellationToken ct)
    {
        var normalizedReference = Normalize(command.Reference);

        var invoice = await _context.Invoices
            .Include(document => document.Lines)
            .Include(document => document.Payments)
            .FirstOrDefaultAsync(document => document.Id == command.InvoiceId, ct);

        if (invoice is null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for update", command.InvoiceId);
            return InvoiceErrors.NotFound;
        }

        if (invoice.ConvertedToInvoiceId.HasValue)
            return InvoiceErrors.ConvertedSourceLocked;

        var stockBefore = InvoiceStockMovement.Capture(invoice);

        if (invoice.Type == InvoiceType.Facture &&
            invoice.GetPaymentSummary(DateTime.UtcNow).Progress == InvoicePaymentProgress.Paid)
        {
            return InvoiceErrors.PaidInvoiceLocked;
        }

        var referenceExists = !string.IsNullOrWhiteSpace(normalizedReference) &&
            await _context.Invoices.AnyAsync(
                document => document.Id != command.InvoiceId && document.Reference == normalizedReference,
                ct);

        if (referenceExists)
        {
            _logger.LogWarning(
                "Invoice update aborted. Reference {Reference} already exists on another document.",
                normalizedReference);
            return InvoiceErrors.ReferenceAlreadyExists;
        }

        if (command.FournisseurId.HasValue)
        {
            var fournisseurExists = await _context.Fournisseurs.AnyAsync(
                fournisseur => fournisseur.Id == command.FournisseurId.Value,
                ct);

            if (!fournisseurExists)
            {
                _logger.LogWarning("Invoice update aborted. Fournisseur {FournisseurId} not found.", command.FournisseurId.Value);
                return FournisseurErrors.NotFound;
            }
        }

        if (command.ClientId.HasValue)
        {
            var clientExists = await _context.Clients.AnyAsync(
                client => client.Id == command.ClientId.Value,
                ct);

            if (!clientExists)
            {
                _logger.LogWarning("Invoice update aborted. Client {ClientId} not found.", command.ClientId.Value);
                return ClientErrors.NotFound;
            }
        }

        var invoiceLines = command.Lines ?? [];
        var produitIds = invoiceLines
            .Select(line => line.ProduitId)
            .Where(produitId => produitId.HasValue)
            .Select(produitId => produitId!.Value)
            .Distinct()
            .ToList();

        var produitsById = await _context.Produits
            .Where(produit => produitIds.Contains(produit.Id))
            .ToDictionaryAsync(produit => produit.Id, ct);

        if (produitIds.Count > 0)
        {
            if (produitsById.Count != produitIds.Count)
            {
                _logger.LogWarning("Invoice update aborted. One or more produits were not found.");
                return ProduitErrors.NotFound;
            }
        }

        List<InvoiceLine> validatedLines = [];

        for (var lineIndex = 0; lineIndex < invoiceLines.Count; lineIndex++)
        {
            var lineCommand = invoiceLines[lineIndex];
            var lineId = lineCommand.InvoiceLineId ?? Guid.NewGuid();
            var lineOrder = lineIndex + 1;
            var produit = lineCommand.ProduitId.HasValue
                ? produitsById[lineCommand.ProduitId.Value]
                : null;

            // For sales, the TTC price is the authoritative unit price: lines are
            // rebuilt from rounded TTC so settled totals keep their exact amounts.
            // Purchases remain HT-first and keep their existing calculation path.
            var productFamily = string.IsNullOrWhiteSpace(lineCommand.ProductFamily)
                ? produit?.Famille ?? string.Empty
                : lineCommand.ProductFamily!.Trim();
            var createLineResult = command.Nature == InvoiceNature.Vente && lineCommand.PriceTTC.HasValue
                ? InvoiceLine.CreateFromTtc(
                    lineId,
                    invoice.Id,
                    lineCommand.ProduitId,
                    produit?.Reference ?? lineCommand.ProductReference ?? string.Empty,
                    produit?.Nom ?? lineCommand.ProductName ?? string.Empty,
                    productFamily,
                    produit?.Unite ?? lineCommand.ProductUnit ?? string.Empty,
                    lineCommand.Quantity,
                    lineCommand.PriceTTC.Value,
                    lineCommand.TVA,
                    lineOrder)
                : InvoiceLine.Create(
                    lineId,
                    invoice.Id,
                    lineCommand.ProduitId,
                    produit?.Reference ?? lineCommand.ProductReference ?? string.Empty,
                    produit?.Nom ?? lineCommand.ProductName ?? string.Empty,
                    productFamily,
                    produit?.Unite ?? lineCommand.ProductUnit ?? string.Empty,
                    lineCommand.Quantity,
                    lineCommand.Price,
                    lineCommand.TVA,
                    lineOrder);

            if (createLineResult.IsError)
                return createLineResult.Errors;

            validatedLines.Add(createLineResult.Value);
        }

        var updateInvoiceResult = invoice.Update(
            normalizedReference,
            command.Type,
            command.Nature,
            command.Date,
            command.FournisseurId,
            command.ClientId,
            command.Total,
            command.Status,
            null,
            null,
            command.DueDate,
            command.Notes);

        if (updateInvoiceResult.IsError)
            return updateInvoiceResult.Errors;

        var updateLinesResult = invoice.UpsertLines(validatedLines);

        if (updateLinesResult.IsError)
            return updateLinesResult.Errors;

        var stockMovementResult = await InvoiceStockMovement.ApplyDeltaAsync(
            _context,
            stockBefore,
            InvoiceStockMovement.Capture(invoice),
            ct);

        if (stockMovementResult.IsError)
            return stockMovementResult.Errors;

        if (Math.Abs(invoice.Total - command.Total) > 0.01m)
        {
            _logger.LogInformation(
                "Invoice total was recalculated by the server. InvoiceId: {InvoiceId}, ClientTotal: {ClientTotal}, ServerTotal: {ServerTotal}",
                invoice.Id,
                command.Total,
                invoice.Total);
        }

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("invoices", ct);
        await _cache.RemoveByTagAsync("produits", ct);

        _logger.LogInformation("Invoice {InvoiceId} updated successfully to status {Status}", invoice.Id, invoice.Status);

        return invoice.ToDto();
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
