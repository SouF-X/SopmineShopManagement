using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Invoices;

public static class InvoiceLineErrors
{
    public static Error InvoiceIdRequired =>
        Error.Validation("InvoiceLine.InvoiceId.Required", "L'identifiant du document est obligatoire.");

    public static Error ProduitIdRequired =>
        Error.Validation("InvoiceLine.ProduitId.Required", "L'identifiant du produit est obligatoire.");

    public static Error ProductReferenceRequired =>
        Error.Validation("InvoiceLine.ProductReference.Required", "La reference du produit est obligatoire.");

    public static Error ProductNameRequired =>
        Error.Validation("InvoiceLine.ProductName.Required", "Le nom du produit est obligatoire.");

    public static Error ProductUnitRequired =>
        Error.Validation("InvoiceLine.ProductUnit.Required", "L'unite du produit est obligatoire.");

    public static Error QuantityInvalid =>
        Error.Validation("InvoiceLine.Quantity.Invalid", "La quantite doit etre superieure a 0.");

    public static Error PriceInvalid =>
        Error.Validation("InvoiceLine.Price.Invalid", "Le prix ne peut pas etre negatif.");

    public static Error TVAInvalid =>
        Error.Validation("InvoiceLine.TVA.Invalid", "La TVA ne peut pas etre negative.");
}
