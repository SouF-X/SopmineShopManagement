using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Produits;

public static class ProduitErrors
{
    public static Error ReferenceRequired =>
        Error.Validation("Produit.Reference.Required", "La reference du produit est obligatoire.");

    public static Error NomRequired =>
        Error.Validation("Produit.Nom.Required", "Le nom du produit est obligatoire.");

    public static Error FamilleRequired =>
        Error.Validation("Produit.Famille.Required", "La famille du produit est obligatoire.");

    public static Error FamilleUnknown =>
        Error.Validation("Produit.Famille.Unknown", "La famille selectionnee est introuvable.");

    public static Error UniteRequired =>
        Error.Validation("Produit.Unite.Required", "L'unite du produit est obligatoire.");

    public static Error UniteUnknown =>
        Error.Validation("Produit.Unite.Unknown", "L'unite de mesure selectionnee est introuvable.");

    public static Error FournisseurIdInvalid =>
        Error.Validation("Produit.FournisseurId.Invalid", "Le fournisseur selectionne est invalide.");

    public static Error QuantityInvalid =>
        Error.Validation("Produit.Quantity.Invalid", "La quantite ne peut pas etre negative.");

    public static Error InsufficientStock =>
        Error.Conflict("Produit.Stock.Insufficient", "Le stock disponible est insuffisant pour valider ce document.");

    public static Error QuantiteMiniInvalid =>
        Error.Validation("Produit.QuantiteMini.Invalid", "La quantite minimale ne peut pas etre negative.");

    public static Error PuAchatHTInvalid =>
        Error.Validation("Produit.PuAchatHT.Invalid", "Le prix d'achat ne peut pas etre negatif.");

    public static Error TVAInvalid =>
        Error.Validation("Produit.TVA.Invalid", "La TVA ne peut pas etre negative.");

    public static Error MargeInvalid =>
        Error.Validation("Produit.Marge.Invalid", "La marge ne peut pas etre negative.");

    public static Error PVenteTTCInvalid =>
        Error.Validation("Produit.PVenteTTC.Invalid", "Le prix de vente ne peut pas etre negatif.");

    public static Error NotFound =>
        Error.NotFound("Produit.NotFound", "Produit introuvable.");

    public static Error InUseByDocuments =>
        Error.Conflict("Produit.InUseByDocuments", "Ce produit est deja utilise dans un document. Il ne peut pas etre supprime.");

    public static Error AlreadyExists =>
        Error.Conflict("Produit.AlreadyExists", "Un produit avec cette reference existe deja.");
}
