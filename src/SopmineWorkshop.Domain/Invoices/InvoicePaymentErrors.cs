using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Invoices;

public static class InvoicePaymentErrors
{
    public static Error InvoiceIdRequired =>
        Error.Validation("InvoicePayment.InvoiceId.Required", "L'identifiant de la facture est obligatoire.");

    public static Error AmountInvalid =>
        Error.Validation("InvoicePayment.Amount.Invalid", "Le montant du paiement doit etre superieur a zero.");

    public static Error MethodInvalid =>
        Error.Validation("InvoicePayment.Method.Invalid", "Le mode de paiement est invalide.");

    public static Error AlreadyCancelled =>
        Error.Conflict("InvoicePayment.AlreadyCancelled", "Ce paiement est deja annule.");
}
