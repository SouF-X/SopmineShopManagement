namespace SopmineWorkshop.Contracts.Requests.Invoices;

public sealed class ConvertBonLivraisonsRequest
{
    public List<Guid> InvoiceIds { get; set; } = [];
}
