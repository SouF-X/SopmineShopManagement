using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Settings;

public static class DocumentNominationErrors
{
    public static Error NotFound =>
        Error.NotFound("DocumentNomination.NotFound", "Configuration de numerotation introuvable.");

    public static Error Forbidden =>
        Error.Forbidden("DocumentNomination.Forbidden", "La numerotation des achats est reservee aux administrateurs.");

    public static Error RootTooLong =>
        Error.Validation("Root", "La racine ne doit pas depasser 30 caracteres.");

    public static Error DateFormatInvalid =>
        Error.Validation("DateFormat", "Le format de date est invalide.");
}
