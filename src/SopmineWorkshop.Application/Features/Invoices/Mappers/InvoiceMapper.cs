using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Application.Features.Invoices.Mappers;

public static class InvoiceMapper
{
    public static InvoiceDto ToDto(this Invoice entity, DateTime? asOf = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var paymentSummary = entity.GetPaymentSummary(asOf ?? DateTime.UtcNow);

        return new InvoiceDto
        {
            InvoiceId = entity.Id,
            CreatedAtUtc = entity.CreatedAtUtc,
            Reference = entity.Reference,
            Type = entity.Type,
            Nature = entity.Nature,
            Date = entity.Date,
            DueDate = entity.DueDate,
            FournisseurId = entity.FournisseurId,
            ClientId = entity.ClientId,
            Status = entity.Status,
            PaymentStatus = entity.PaymentStatus,
            PaymentMethod = entity.PaymentMethod,
            ConvertedToInvoiceId = entity.ConvertedToInvoiceId,
            Notes = entity.Notes,
            Subtotal = entity.Subtotal,
            TaxTotal = entity.TaxTotal,
            Total = entity.Total,
            TotalPaid = paymentSummary.TotalPaid,
            RemainingAmount = paymentSummary.RemainingAmount,
            PaymentProgress = paymentSummary.Progress,
            Lines = entity.Lines?
                .OrderBy(line => line.LineOrder)
                .ThenBy(line => line.CreatedAtUtc)
                .ThenBy(line => line.Id)
                .Select(line => line.ToDto())
                .ToList() ?? []
        };
    }

    public static List<InvoiceDto> ToDtos(this IEnumerable<Invoice> entities, DateTime? asOf = null)
    {
        var capturedAsOf = asOf ?? DateTime.UtcNow;
        return [.. entities.Select(entity => entity.ToDto(capturedAsOf))];
    }

    public static InvoiceLineDto ToDto(this InvoiceLine entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new InvoiceLineDto
        {
            InvoiceLineId = entity.Id,
            ProduitId = entity.ProduitId,
            ProductReference = entity.ProductReference,
            ProductName = entity.ProductName,
            ProductFamily = entity.ProductFamily,
            ProductUnit = entity.ProductUnit,
            Quantity = entity.Quantity,
            Price = entity.Price,
            PriceTTC = entity.Quantity > 0
                ? Math.Round(entity.LineTotal / entity.Quantity, 2, MidpointRounding.AwayFromZero)
                : 0,
            TVA = entity.TVA,
            LineSubtotal = entity.LineSubtotal,
            LineTax = entity.LineTax,
            LineTotal = entity.LineTotal,
            LineOrder = entity.LineOrder
        };
    }
}
