namespace SopmineWorkshop.Application.Features.Invoices.Dtos;

public class InvoiceLineDto
{
    public Guid InvoiceLineId { get; set; }
    public Guid? ProduitId { get; set; }
    public string ProductReference { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductFamily { get; set; } = string.Empty;
    public string ProductUnit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal PriceTTC { get; set; }
    public decimal TVA { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineTax { get; set; }
    public decimal LineTotal { get; set; }
    public int LineOrder { get; set; }
}
