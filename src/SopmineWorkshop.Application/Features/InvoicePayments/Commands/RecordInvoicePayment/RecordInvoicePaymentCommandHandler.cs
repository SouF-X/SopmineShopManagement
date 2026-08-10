using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.InvoicePayments.Dtos;
using SopmineWorkshop.Application.Features.InvoicePayments.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Invoices;
namespace SopmineWorkshop.Application.Features.InvoicePayments.Commands.RecordInvoicePayment;
public sealed class RecordInvoicePaymentCommandHandler(IAppDbContext context, HybridCache cache) : IRequestHandler<RecordInvoicePaymentCommand, Result<InvoicePaymentMutationDto>>
{
    public async Task<Result<InvoicePaymentMutationDto>> Handle(RecordInvoicePaymentCommand command, CancellationToken ct)
    {
        var invoice = await context.Invoices.Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == command.InvoiceId, ct);
        if (invoice is null) return InvoiceErrors.NotFound;
        var recorded = invoice.RecordPayment(Guid.NewGuid(), command.Amount, command.PaymentDate, command.Method, command.Reference, command.Note);
        if (recorded.IsError) return recorded.Errors;
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Error.Conflict("InvoicePayment.ConcurrentWrite", "Le paiement a ete modifie. Actualisez la facture et recommencez."); }
        await cache.RemoveByTagAsync("invoices", ct);
        var summary = invoice.GetPaymentSummary(DateTime.UtcNow);
        return new InvoicePaymentMutationDto { InvoiceId = invoice.Id, Payment = recorded.Value.ToDto(), TotalPaid = summary.TotalPaid, RemainingAmount = summary.RemainingAmount, PaymentProgress = summary.Progress, Status = invoice.Status };
    }
}
