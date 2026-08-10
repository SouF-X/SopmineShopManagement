using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Application.Features.Invoices.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.ConvertBonLivraisons;

public sealed class ConvertBonLivraisonsCommandHandler(
    ILogger<ConvertBonLivraisonsCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IDocumentReferenceGenerator referenceGenerator)
    : IRequestHandler<ConvertBonLivraisonsCommand, Result<InvoiceDto>>
{
    private readonly ILogger<ConvertBonLivraisonsCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;
    private readonly IDocumentReferenceGenerator _referenceGenerator = referenceGenerator;

    public async Task<Result<InvoiceDto>> Handle(ConvertBonLivraisonsCommand command, CancellationToken ct)
    {
        var selectedIds = command.InvoiceIds.Distinct().ToList();

        var sourceInvoices = await _context.Invoices
            .Include(invoice => invoice.Lines)
            .Include(invoice => invoice.Payments)
            .Where(invoice => selectedIds.Contains(invoice.Id))
            .ToListAsync(ct);

        if (sourceInvoices.Count != selectedIds.Count)
            return InvoiceErrors.NotFound;

        if (sourceInvoices.Any(invoice =>
                invoice.Nature != InvoiceNature.Vente ||
                invoice.Type != InvoiceType.BonLivraison))
        {
            return InvoiceErrors.TypeNotAllowedForNature;
        }

        if (sourceInvoices.Any(invoice => invoice.ConvertedToInvoiceId.HasValue))
            return InvoiceErrors.AlreadyConverted;

        if (sourceInvoices.Any(invoice => invoice.Status is not (InvoiceStatus.Validated or InvoiceStatus.Paid)))
            return InvoiceErrors.StatusInvalid;

        sourceInvoices = selectedIds
            .Select(invoiceId => sourceInvoices.First(invoice => invoice.Id == invoiceId))
            .ToList();

        var clientIds = sourceInvoices
            .Select(invoice => invoice.ClientId)
            .Distinct()
            .ToList();

        if (clientIds.Count != 1 || !clientIds[0].HasValue)
            return InvoiceErrors.ConversionClientMismatch;

        var invoiceId = Guid.NewGuid();
        var conversionDate = DateTime.Today;
        var reference = await _referenceGenerator.GenerateAsync(InvoiceNature.Vente, InvoiceType.Facture, conversionDate, ct);
        var lines = BuildMergedLines(invoiceId, sourceInvoices);

        var createResult = Invoice.Create(
            invoiceId,
            reference,
            InvoiceType.Facture,
            InvoiceNature.Vente,
            conversionDate,
            null,
            clientIds[0],
            lines.Sum(line => line.LineTotal),
            lines,
            InvoiceStatus.Draft);

        if (createResult.IsError)
            return createResult.Errors;

        var invoice = createResult.Value;
        _context.Invoices.Add(invoice);

        foreach (var sourceInvoice in sourceInvoices)
        {
            sourceInvoice.TransferPaymentsTo(invoice);

            var markResult = sourceInvoice.MarkConvertedTo(invoiceId);

            if (markResult.IsError)
                return markResult.Errors;
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _context.Invoices.AnyAsync(item => item.Reference == reference, ct))
                return InvoiceErrors.ReferenceAlreadyExists;
            throw;
        }

        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation(
            "Converted {Count} bons de livraison into facture {InvoiceId}.",
            sourceInvoices.Count,
            invoiceId);

        return invoice.ToDto();
    }

    private static List<InvoiceLine> BuildMergedLines(Guid invoiceId, List<Invoice> sourceInvoices)
    {
        return sourceInvoices
            .SelectMany(invoice => invoice.Lines
                .OrderBy(line => line.LineOrder)
                .ThenBy(line => line.CreatedAtUtc)
                .ThenBy(line => line.Id))
            .GroupBy(line => new
            {
                line.ProduitId,
                line.ProductReference,
                line.ProductName,
                line.ProductFamily,
                line.ProductUnit,
                line.Price,
                line.TVA
            })
            .Select((group, index) =>
            {
                var first = group.First();
                var lineResult = InvoiceLine.Create(
                    Guid.NewGuid(),
                    invoiceId,
                    first.ProduitId,
                    first.ProductReference,
                    first.ProductName,
                    first.ProductFamily,
                    first.ProductUnit,
                    group.Sum(line => line.Quantity),
                    first.Price,
                    first.TVA,
                    index + 1);

                if (lineResult.IsError)
                    throw new InvalidOperationException("Unable to convert invoice line.");

                return lineResult.Value;
            })
            .ToList();
    }
}
