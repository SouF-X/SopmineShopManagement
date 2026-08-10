using SopmineWorkshop.Contracts.Common.Invoice;
namespace SopmineWorkshop.Contracts.Requests.Invoice;
public sealed class RecordInvoicePaymentRequest { public decimal Amount { get; set; } public DateTime PaymentDate { get; set; } public InvoicePaymentMethod Method { get; set; } public string? Reference { get; set; } public string? Note { get; set; } }
