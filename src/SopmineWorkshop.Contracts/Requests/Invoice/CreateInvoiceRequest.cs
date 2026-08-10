using SopmineWorkshop.Contracts.Common.Invoice;

namespace SopmineWorkshop.Contracts.Requests.Invoices;

public sealed class CreateInvoiceRequest
{
    public string? Reference { get; set; }
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
    public CreateInvoiceSupplierRequest? NewSupplier { get; set; }
    public List<CreateInvoiceLineRequest> Lines { get; set; } = [];
    public bool CatalogueMode { get; set; } = true;
}

public sealed class CreateInvoiceSupplierRequest
{
    public string? Name { get; set; }
    public string? ICE { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
}
