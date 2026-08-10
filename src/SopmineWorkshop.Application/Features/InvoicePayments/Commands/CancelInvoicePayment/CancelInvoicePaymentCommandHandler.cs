using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.InvoicePayments.Dtos;
using SopmineWorkshop.Application.Features.InvoicePayments.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Invoices;
namespace SopmineWorkshop.Application.Features.InvoicePayments.Commands.CancelInvoicePayment;
public sealed class CancelInvoicePaymentCommandHandler(IAppDbContext context, HybridCache cache) : IRequestHandler<CancelInvoicePaymentCommand, Result<InvoicePaymentMutationDto>>
{
    public async Task<Result<InvoicePaymentMutationDto>> Handle(CancelInvoicePaymentCommand command, CancellationToken ct)
    {
        var invoice = await context.Invoices.Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == command.InvoiceId, ct);
        if (invoice is null) return InvoiceErrors.NotFound;
        var payment = invoice.Payments.FirstOrDefault(x => x.Id == command.PaymentId);
        var cancelled = invoice.CancelPayment(command.PaymentId, DateTimeOffset.UtcNow, command.Reason);
        if (cancelled.IsError) return cancelled.Errors;
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Error.Conflict("InvoicePayment.ConcurrentWrite", "Le paiement a ete modifie. Actualisez la facture et recommencez."); }
        await cache.RemoveByTagAsync("invoices", ct);
        var summary = invoice.GetPaymentSummary(DateTime.UtcNow);

        return new InvoicePaymentMutationDto { InvoiceId = invoice.Id, Payment = payment?.ToDto(), TotalPaid = summary.TotalPaid, RemainingAmount = summary.RemainingAmount, PaymentProgress = summary.Progress, Status = invoice.Status };
    }
}
