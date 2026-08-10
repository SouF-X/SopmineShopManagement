using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Invoices.Dtos;

public class InvoiceDto
{
    public Guid InvoiceId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string Reference { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public InvoiceNature Nature { get; set; }
    public DateTime Date { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? FournisseurId { get; set; }
    public Guid? ClientId { get; set; }
    public InvoiceStatus Status { get; set; }
    public InvoicePaymentStatus? PaymentStatus { get; set; }
    public InvoicePaymentMethod? PaymentMethod { get; set; }
    public Guid? ConvertedToInvoiceId { get; set; }
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public InvoicePaymentProgress PaymentProgress { get; set; }
    public List<InvoiceLineDto> Lines { get; set; } = [];
}
