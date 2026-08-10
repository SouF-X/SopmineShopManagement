using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Statements.Dtos;

public sealed class StatementMovementDto
{
    public Guid MovementId { get; init; }
    public Guid InvoiceId { get; init; }
    public Guid? PaymentId { get; init; }
    public DateTime MovementDate { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public string Reference { get; init; } = string.Empty;
    public InvoicePaymentMethod? Method { get; init; }
    public InvoiceType? DocumentType { get; init; }
    public string MovementType { get; init; } = string.Empty;
    public decimal DocumentAmount { get; init; }
    public decimal BalanceImpact { get; init; }
    public InvoicePaymentProgress? PaymentProgress { get; init; }
    public bool IsInformational { get; init; }
    public decimal InvoicedAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RunningBalance { get; init; }
    public bool IsCancelled { get; init; }
}
