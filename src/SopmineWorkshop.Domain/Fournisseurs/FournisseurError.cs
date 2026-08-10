using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Fournisseurs;

public static class FournisseurErrors
{
    public static Error NomRequired =>
        Error.Validation("Fournisseur.NomRequired", "Le nom du fournisseur est obligatoire.");

    public static Error IceRequired =>
        Error.Validation("Fournisseur.IceRequired", "L'ICE du fournisseur est obligatoire.");

    public static Error AdresseRequired =>
        Error.Validation("Fournisseur.AdresseRequired", "L'adresse du fournisseur est obligatoire.");

    public static Error VilleRequired =>
        Error.Validation("Fournisseur.VilleRequired", "La ville du fournisseur est obligatoire.");

    public static Error TelFixRequired =>
        Error.Validation("Fournisseur.TelFixRequired", "Le telephone fixe du fournisseur est obligatoire.");

    public static Error TelFixInvalid =>
        Error.Validation("Fournisseur.TelFixInvalid", "Le telephone fixe du fournisseur est invalide.");

    public static Error SiteWebInvalid =>
        Error.Validation("Fournisseur.SiteWebInvalid", "Le site web du fournisseur est invalide.");

    public static Error EmailInvalid =>
        Error.Validation("Fournisseur.EmailInvalid", "L'email du fournisseur est invalide.");

    public static Error ContactRequired =>
        Error.Validation("Fournisseur.ContactRequired", "Au moins un contact fournisseur est obligatoire.");

    public static Error NotFound =>
        Error.NotFound("Fournisseur.NotFound", "Fournisseur introuvable.");

    public static Error InUseByDocuments =>
        Error.Conflict("Fournisseur.InUseByDocuments", "Ce fournisseur est deja utilise dans un document. Il ne peut pas etre supprime.");

    public static Error InUseByProducts =>
        Error.Conflict("Fournisseur.InUseByProducts", "Ce fournisseur est lie a un ou plusieurs produits. Il ne peut pas etre supprime.");

    public static Error AlreadyExists =>
        Error.Conflict("Fournisseur.AlreadyExists", "Un fournisseur avec ce nom existe deja.");
}
