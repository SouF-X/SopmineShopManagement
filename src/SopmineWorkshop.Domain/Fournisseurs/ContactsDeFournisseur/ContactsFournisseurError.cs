using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;

public static class ContactFournisseurErrors
{
    public static Error NomRequired =>
        Error.Validation("ContactFournisseur.NomRequired", "Le nom du contact est obligatoire.");

    public static Error TelRequired =>
        Error.Validation("ContactFournisseur.TelRequired", "Le telephone du contact est obligatoire.");

    public static Error TelInvalid =>
        Error.Validation("ContactFournisseur.TelInvalid", "Le telephone du contact est invalide.");

    public static Error RoleInvalid =>
        Error.Validation("ContactFournisseur.RoleInvalid", "Le role du contact est invalide.");

    public static Error NotFound =>
        Error.NotFound("ContactFournisseur.NotFound", "Contact fournisseur introuvable.");
}
