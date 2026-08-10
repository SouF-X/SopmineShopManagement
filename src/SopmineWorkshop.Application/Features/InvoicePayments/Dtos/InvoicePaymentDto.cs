using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.InvoicePayments.Dtos;

public sealed class InvoicePaymentDto
{
    public Guid PaymentId { get; init; }
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public DateTime PaymentDate { get; init; }
    public InvoicePaymentMethod? Method { get; init; }
    public string? Reference { get; init; }
    public string? Note { get; init; }
    public bool IsOpeningBalance { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? CancelledAtUtc { get; init; }
    public string? CancellationReason { get; init; }
}
