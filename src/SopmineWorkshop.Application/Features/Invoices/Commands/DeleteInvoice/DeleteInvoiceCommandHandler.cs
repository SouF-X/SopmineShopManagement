using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Invoices;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.DeleteInvoice;

public sealed class DeleteInvoiceCommandHandler(
    ILogger<DeleteInvoiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<DeleteInvoiceCommand, Result<Deleted>>
{
    private readonly ILogger<DeleteInvoiceCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Deleted>> Handle(DeleteInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .Include(document => document.Payments)
            .Include(document => document.Lines)
            .FirstOrDefaultAsync(document => document.Id == command.InvoiceId, ct);

        if (invoice is null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found for deletion", command.InvoiceId);
            return InvoiceErrors.NotFound;
        }

        if (invoice.ConvertedToInvoiceId.HasValue)
            return InvoiceErrors.ConvertedSourceLocked;

        if (invoice.Type == InvoiceType.Facture &&
            invoice.GetPaymentSummary(DateTime.UtcNow).Progress == InvoicePaymentProgress.Paid)
        {
            return InvoiceErrors.PaidInvoiceLocked;
        }

        if (invoice.Payments.Count > 0)
            return Error.Conflict("Invoice.PaymentHistory.Exists", "Une facture avec un historique de paiements ne peut pas etre supprimee.");

        var convertedSources = await _context.Invoices
            .Where(document => document.ConvertedToInvoiceId == invoice.Id)
            .ToListAsync(ct);

        foreach (var sourceInvoice in convertedSources)
        {
            sourceInvoice.ClearConvertedTo();
        }

        var stockMovementResult = await InvoiceStockMovement.ApplyDeltaAsync(
            _context,
            InvoiceStockMovement.Capture(invoice),
            new Dictionary<Guid, decimal>(),
            ct);

        if (stockMovementResult.IsError)
            return stockMovementResult.Errors;

        _context.Invoices.Remove(invoice);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("invoices", ct);
        await _cache.RemoveByTagAsync("produits", ct);

        _logger.LogInformation(
            "Invoice {InvoiceId} deleted successfully and unlocked {SourceCount} converted source documents.",
            invoice.Id,
            convertedSources.Count);

        return Result.Deleted;
    }
}
