using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Clients;

public static class ClientErrors
{
    public static Error NomRequired =>
        Error.Validation("Client.NomRequired", "Le nom du client est obligatoire.");

    public static Error TypeInvalid =>
        Error.Validation("Client.TypeInvalid", "Le type de client est invalide.");

    public static Error IceRequired =>
        Error.Validation("Client.IceRequired", "L'ICE est obligatoire pour un client professionnel.");

    public static Error TelRequired =>
        Error.Validation("Client.TelRequired", "Le telephone du client est obligatoire.");

    public static Error TelInvalid =>
        Error.Validation("Client.TelInvalid", "Le telephone du client est invalide.");

    public static Error ContactRequired =>
        Error.Validation("Client.ContactRequired", "Au moins un contact client est obligatoire.");

    public static Error NotFound =>
        Error.NotFound("Client.NotFound", "Client introuvable.");

    public static Error InUseByDocuments =>
        Error.Conflict("Client.InUseByDocuments", "Ce client est deja utilise dans un document. Il ne peut pas etre supprime.");

    public static Error AlreadyExists =>
        Error.Conflict("Client.AlreadyExists", "Un client avec ce nom existe deja.");
}
