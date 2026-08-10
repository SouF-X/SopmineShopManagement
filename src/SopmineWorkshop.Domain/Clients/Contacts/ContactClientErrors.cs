using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Clients.Contacts;

public static class ContactClientErrors
{
    public static Error ClientIdRequired =>
        Error.Validation("ContactClient.ClientIdRequired", "L'identifiant du client est obligatoire.");

    public static Error NomRequired =>
        Error.Validation("ContactClient.NomRequired", "Le nom du contact est obligatoire.");

    public static Error TelRequired =>
        Error.Validation("ContactClient.TelRequired", "Le telephone du contact est obligatoire.");

    public static Error TelInvalid =>
        Error.Validation("ContactClient.TelInvalid", "Le telephone du contact est invalide.");

    public static Error RoleInvalid =>
        Error.Validation("ContactClient.RoleInvalid", "Le role du contact est invalide.");

    public static Error NotFound =>
        Error.NotFound("ContactClient.NotFound", "Contact client introuvable.");
}