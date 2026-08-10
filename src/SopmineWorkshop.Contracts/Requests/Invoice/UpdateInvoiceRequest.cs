using SopmineWorkshop.Contracts.Common.Invoice;

namespace SopmineWorkshop.Contracts.Requests.Invoices;

public sealed class UpdateInvoiceRequest
{
    public string Reference { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public InvoiceNature Nature { get; set; }
    public DateTime Date { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? FournisseurId { get; set; }
    public Guid? ClientId { get; set; }
    public InvoiceStatus? Status { get; set; }
    public InvoicePaymentStatus? PaymentStatus { get; set; }
    public InvoicePaymentMethod? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public decimal Total { get; set; }
    public List<UpdateInvoiceLineRequest> Lines { get; set; } = [];
}
