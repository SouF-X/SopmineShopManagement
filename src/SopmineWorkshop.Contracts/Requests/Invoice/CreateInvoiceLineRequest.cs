namespace SopmineWorkshop.Contracts.Requests.Invoices;

public sealed class CreateInvoiceLineRequest
{
    public Guid? ProduitId { get; set; }
    public string? ProductReference { get; set; }
    public string? ProductName { get; set; }
    public string? ProductFamily { get; set; }
    public string? ProductUnit { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal? PriceTTC { get; set; }
    public decimal TVA { get; set; }
}
