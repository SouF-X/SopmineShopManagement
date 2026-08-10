using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.InvoicePayments.Dtos;

public sealed class InvoicePaymentMutationDto
{
    public Guid InvoiceId { get; init; }
    public InvoicePaymentDto? Payment { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal RemainingAmount { get; init; }
    public InvoicePaymentProgress PaymentProgress { get; init; }
    public InvoiceStatus Status { get; init; }
}
