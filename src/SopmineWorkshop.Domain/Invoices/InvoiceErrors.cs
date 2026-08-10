using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Invoices;

public static class InvoiceErrors
{
    public static Error ReferenceRequired =>
        Error.Validation("Invoice.Reference.Required", "La reference du document est obligatoire.");

    public static Error ReferenceTooLong =>
        Error.Validation("Invoice.Reference.TooLong", "La reference du document ne peut pas depasser 100 caracteres.");

    public static Error ReferenceAlreadyExists =>
        Error.Validation("Invoice.Reference.AlreadyExists", "Cette reference document existe deja.");

    public static Error TypeInvalid =>
        Error.Validation("Invoice.Type.Invalid", "Le type du document est invalide.");

    public static Error TypeNotAllowedForNature =>
        Error.Validation("Invoice.Type.NotAllowedForNature", "Le type du document n'est pas autorise pour cette nature.");

    public static Error NatureInvalid =>
        Error.Validation("Invoice.Nature.Invalid", "La nature du document est invalide.");

    public static Error DateRequired =>
        Error.Validation("Invoice.Date.Required", "La date du document est obligatoire.");

    public static Error DueDateInvalid =>
        Error.Validation("Invoice.DueDate.Invalid", "La date d'echeance ne peut pas etre avant la date du document.");

    public static Error StatusInvalid =>
        Error.Validation("Invoice.Status.Invalid", "Le statut du document est invalide.");

    public static Error PaymentStatusRequired =>
        Error.Validation("Invoice.PaymentStatus.Required", "Le statut de paiement est obligatoire pour ce document.");

    public static Error PaymentStatusInvalid =>
        Error.Validation("Invoice.PaymentStatus.Invalid", "Le statut de paiement est invalide.");

    public static Error PaymentMethodRequired =>
        Error.Validation("Invoice.PaymentMethod.Required", "Le mode de paiement est obligatoire quand le document est paye.");

    public static Error PaymentMethodInvalid =>
        Error.Validation("Invoice.PaymentMethod.Invalid", "Le mode de paiement est invalide.");

    public static Error PaymentNotAllowed =>
        Error.Validation("Invoice.Payment.NotAllowed", "Le paiement n'est pas configure pour ce type de document.");

    public static Error PaymentInvoiceNotPayable =>
        Error.Validation("Invoice.Payment.InvoiceNotPayable", "Un paiement ne peut pas etre enregistre sur un brouillon ou un document annule.");

    public static Error PaymentInvoiceTypeNotSupported =>
        Error.Validation("Invoice.Payment.InvoiceTypeNotSupported", "Seules les factures peuvent recevoir un paiement.");

    public static Error PaymentExceedsRemainingAmount =>
        Error.Validation("Invoice.Payment.ExceedsRemainingAmount", "Le paiement ne peut pas depasser le solde restant.");

    public static Error TotalBelowActivePayments =>
        Error.Validation("Invoice.Total.BelowActivePayments", "Le total ne peut pas etre inferieur aux paiements actifs.");

    public static Error PaidInvoiceLocked =>
        Error.Conflict("Invoice.PaidFacture.Locked", "Facture réglée — modification et suppression verrouillées.");

    public static Error PaymentNotFound =>
        Error.NotFound("Invoice.Payment.NotFound", "Paiement introuvable.");

    public static Error PaidStatusRequiresSettlement =>
        Error.Validation("Invoice.Status.PaidRequiresSettlement", "Le document ne peut etre marque comme paye que lorsque son solde est regle.");

    public static Error AlreadyConverted =>
        Error.Validation("Invoice.AlreadyConverted", "Ce bon de livraison est deja converti en facture.");

    public static Error ConvertedSourceLocked =>
        Error.Conflict("Invoice.ConvertedSource.Locked", "Bon de livraison facture - les modifications et reglements se font sur la facture.");

    public static Error ConversionClientMismatch =>
        Error.Validation("Invoice.Conversion.ClientMismatch", "Les bons de livraison selectionnes doivent appartenir au meme client.");

    public static Error FournisseurIdInvalid =>
        Error.Validation("Invoice.FournisseurId.Invalid", "L'identifiant du fournisseur est invalide.");

    public static Error ClientIdInvalid =>
        Error.Validation("Invoice.ClientId.Invalid", "L'identifiant du client est invalide.");

    public static Error FournisseurRequiredForAchat =>
        Error.Validation("Invoice.Fournisseur.RequiredForAchat", "Le fournisseur est obligatoire pour un document d'achat.");

    public static Error ClientForbiddenForAchat =>
        Error.Validation("Invoice.Client.ForbiddenForAchat", "Le client n'est pas autorise pour un document d'achat.");

    public static Error ClientRequiredForVente =>
        Error.Validation("Invoice.Client.RequiredForVente", "Le client est obligatoire pour un document de vente.");

    public static Error FournisseurForbiddenForVente =>
        Error.Validation("Invoice.Fournisseur.ForbiddenForVente", "Le fournisseur n'est pas autorise pour un document de vente.");

    public static Error TotalInvalid =>
        Error.Validation("Invoice.Total.Invalid", "Le total ne peut pas etre negatif.");

    public static Error LinesRequired =>
        Error.Validation("Invoice.Lines.Required", "La liste des lignes est obligatoire.");

    public static Error NotFound =>
        Error.NotFound("Invoice.NotFound", "Document introuvable.");
}
