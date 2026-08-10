using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Domain.Invoices;

public sealed record InvoicePaymentSummary(
    decimal TotalPaid,
    decimal RemainingAmount,
    InvoicePaymentProgress Progress);
