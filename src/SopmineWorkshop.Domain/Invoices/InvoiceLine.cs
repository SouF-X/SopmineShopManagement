using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Invoices;

public sealed class InvoiceLine : AuditableEntity
{
    public Guid InvoiceId { get; private set; }
    public Guid? ProduitId { get; private set; }
    public string ProductReference { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public string ProductFamily { get; private set; } = string.Empty;
    public string ProductUnit { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal Price { get; private set; }
    public decimal TVA { get; private set; }
    public decimal LineSubtotal { get; private set; }
    public decimal LineTax { get; private set; }
    public decimal LineTotal { get; private set; }
    public int LineOrder { get; private set; }
    public Invoice? Invoice { get; set; }

    private InvoiceLine()
    {
    }

    private InvoiceLine(
        Guid id,
        Guid invoiceId,
        Guid? produitId,
        string productReference,
        string productName,
        string productFamily,
        string productUnit,
        decimal quantity,
        decimal price,
        decimal tva,
        decimal lineSubtotal,
        decimal lineTax,
        decimal lineTotal,
        int lineOrder)
        : base(id)
    {
        InvoiceId = invoiceId;
        ProduitId = produitId;
        ProductReference = productReference;
        ProductName = productName;
        ProductFamily = productFamily;
        ProductUnit = productUnit;
        Quantity = quantity;
        Price = price;
        TVA = tva;
        LineSubtotal = lineSubtotal;
        LineTax = lineTax;
        LineTotal = lineTotal;
        LineOrder = NormalizeLineOrder(lineOrder);
    }

    public static Result<InvoiceLine> Create(
        Guid id,
        Guid invoiceId,
        Guid? produitId,
        string productReference,
        string productName,
        string productFamily,
        string productUnit,
        decimal quantity,
        decimal price,
        decimal tva,
        int lineOrder = 0)
    {
        if (invoiceId == Guid.Empty)
            return InvoiceLineErrors.InvoiceIdRequired;

        if (string.IsNullOrWhiteSpace(productName))
            return InvoiceLineErrors.ProductNameRequired;

        if (produitId.HasValue && produitId.Value == Guid.Empty)
            return InvoiceLineErrors.ProduitIdRequired;

        if (quantity < 0)
            return InvoiceLineErrors.QuantityInvalid;

        if (price < 0)
            return InvoiceLineErrors.PriceInvalid;

        if (tva < 0)
            return InvoiceLineErrors.TVAInvalid;

        var (lineSubtotal, lineTax, lineTotal) = CalculateAmounts(quantity, price, tva);

        return new InvoiceLine(
            id,
            invoiceId,
            produitId,
            NormalizeText(productReference),
            NormalizeText(productName),
            NormalizeOptional(productFamily),
            NormalizeText(productUnit),
            quantity,
            price,
            tva,
            lineSubtotal,
            lineTax,
            lineTotal,
            lineOrder);
    }

    public static Result<InvoiceLine> CreateFromTtc(
        Guid id,
        Guid invoiceId,
        Guid? produitId,
        string productReference,
        string productName,
        string productFamily,
        string productUnit,
        decimal quantity,
        decimal priceTtc,
        decimal tva,
        int lineOrder = 0)
    {
        if (invoiceId == Guid.Empty)
            return InvoiceLineErrors.InvoiceIdRequired;

        if (string.IsNullOrWhiteSpace(productName))
            return InvoiceLineErrors.ProductNameRequired;

        if (produitId.HasValue && produitId.Value == Guid.Empty)
            return InvoiceLineErrors.ProduitIdRequired;

        if (quantity < 0)
            return InvoiceLineErrors.QuantityInvalid;

        if (priceTtc < 0)
            return InvoiceLineErrors.PriceInvalid;

        if (tva < 0)
            return InvoiceLineErrors.TVAInvalid;

        var (lineSubtotal, lineTax, lineTotal, priceHt) = CalculateAmountsFromTtc(quantity, priceTtc, tva);

        return new InvoiceLine(
            id,
            invoiceId,
            produitId,
            NormalizeText(productReference),
            NormalizeText(productName),
            NormalizeOptional(productFamily),
            NormalizeText(productUnit),
            quantity,
            priceHt,
            tva,
            lineSubtotal,
            lineTax,
            lineTotal,
            lineOrder);
    }

    public Result<Updated> Update(
        Guid? produitId,
        string productReference,
        string productName,
        string productFamily,
        string productUnit,
        decimal quantity,
        decimal price,
        decimal tva,
        decimal? lineSubtotal = null,
        decimal? lineTax = null,
        decimal? lineTotal = null,
        int lineOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return InvoiceLineErrors.ProductNameRequired;

        if (produitId.HasValue && produitId.Value == Guid.Empty)
            return InvoiceLineErrors.ProduitIdRequired;

        if (quantity < 0)
            return InvoiceLineErrors.QuantityInvalid;

        if (price < 0)
            return InvoiceLineErrors.PriceInvalid;

        if (tva < 0)
            return InvoiceLineErrors.TVAInvalid;

        decimal lineSubtotalValue;
        decimal lineTaxValue;
        decimal lineTotalValue;

        if (lineSubtotal.HasValue && lineTax.HasValue && lineTotal.HasValue)
        {
            lineSubtotalValue = lineSubtotal.Value;
            lineTaxValue = lineTax.Value;
            lineTotalValue = lineTotal.Value;
        }
        else
        {
            (lineSubtotalValue, lineTaxValue, lineTotalValue) = CalculateAmounts(quantity, price, tva);
        }

        ProduitId = produitId;
        ProductReference = NormalizeText(productReference);
        ProductName = NormalizeText(productName);
        ProductFamily = NormalizeOptional(productFamily);
        ProductUnit = NormalizeText(productUnit);
        Quantity = quantity;
        Price = price;
        TVA = tva;
        LineSubtotal = lineSubtotalValue;
        LineTax = lineTaxValue;
        LineTotal = lineTotalValue;
        LineOrder = NormalizeLineOrder(lineOrder);

        return Result.Updated;
    }

    private static (decimal Subtotal, decimal Tax, decimal Total) CalculateAmounts(decimal quantity, decimal price, decimal tva)
    {
        var subtotal = Math.Round(quantity * price, 2, MidpointRounding.AwayFromZero);
        var tax = Math.Round(subtotal * (tva / 100), 2, MidpointRounding.AwayFromZero);
        var total = subtotal + tax;

        return (subtotal, tax, total);
    }

    private static (decimal Subtotal, decimal Tax, decimal Total, decimal PriceHt) CalculateAmountsFromTtc(decimal quantity, decimal priceTtc, decimal tva)
    {
        var total = Math.Round(quantity * priceTtc, 2, MidpointRounding.AwayFromZero);
        var divisor = 1 + tva / 100;
        var subtotal = divisor > 0
            ? Math.Round(total / divisor, 2, MidpointRounding.AwayFromZero)
            : total;
        var tax = Math.Round(total - subtotal, 2, MidpointRounding.AwayFromZero);
        var priceHt = quantity > 0
            ? Math.Round(subtotal / quantity, 2, MidpointRounding.AwayFromZero)
            : 0;

        return (subtotal, tax, total, priceHt);
    }

    private static string NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? string.Empty : trimmed;
    }

    private static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;

    private static int NormalizeLineOrder(int value)
        => Math.Max(value, 0);
}
