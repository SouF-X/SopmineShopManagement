using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Domain.Invoices;

public class InvoiceExtractionDto
{
    public Guid? FournisseurId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierICE { get; set; }
    public string? SupplierAddress { get; set; }
    public string? SupplierCity { get; set; }
    public string? SupplierPhone { get; set; }
    public string? SupplierEmail { get; set; }
    public string? SupplierWebsite { get; set; }
    public InvoiceType Type { get; set; } = InvoiceType.Facture;
    public InvoiceNature Nature { get; set; } = InvoiceNature.Achat;
    public DateTime? Date { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal TotalHT { get; set; }
    public decimal TotalTVA { get; set; }
    public decimal TotalTTC { get; set; }
    public decimal Total { get; set; }
    public List<InvoiceExtractionLineDto> Lines { get; set; } = [];
}

public class InvoiceExtractionLineDto
{
    public Guid? ProduitId { get; set; }
    public string ProductReference { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string ProductFamily { get; set; } = string.Empty;
    public string ProductUnit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitPriceHT { get; set; }
    public decimal UnitPriceTTC { get; set; }
    public decimal TVA { get; set; }
    public decimal AmountHT { get; set; }
    public decimal AmountTTC { get; set; }
    public bool PriceIncludesTax { get; set; }
}
