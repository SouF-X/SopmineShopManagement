using SopmineWorkshop.Application.Features.InvoicePayments.Dtos;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Application.Features.InvoicePayments.Mappers;

public static class InvoicePaymentMapper
{
    public static InvoicePaymentDto ToDto(this InvoicePayment payment) => new()
    {
        PaymentId = payment.Id, InvoiceId = payment.InvoiceId, Amount = payment.Amount,
        PaymentDate = payment.PaymentDate, Method = payment.Method, Reference = payment.Reference,
        Note = payment.Note, IsOpeningBalance = payment.IsOpeningBalance,
        CreatedAtUtc = payment.CreatedAtUtc, CancelledAtUtc = payment.CancelledAtUtc,
        CancellationReason = payment.CancellationReason
    };
}
