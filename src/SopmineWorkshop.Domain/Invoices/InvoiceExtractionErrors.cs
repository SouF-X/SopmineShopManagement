using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Invoices;

public static class InvoiceExtractionErrors
{
    public static Error ImageRequired =>
        Error.Validation("InvoiceExtraction.Image.Required", "L'image de la facture est obligatoire.");

    public static Error ImageTypeInvalid =>
        Error.Validation("InvoiceExtraction.Image.TypeInvalid", "Le fichier doit etre une image valide.");

    public static Error ImageTooLarge =>
        Error.Validation("InvoiceExtraction.Image.TooLarge", "L'image depasse la taille autorisee par le service d'extraction configure.");

    public static Error ServiceNotConfigured =>
        Error.Unexpected("InvoiceExtraction.Service.NotConfigured", "Le service d'extraction des factures n'est pas configure.");

    public static Error ServiceUnavailable =>
        Error.Failure("InvoiceExtraction.Service.Unavailable", "Le service d'extraction des factures est indisponible.");

    public static Error QuotaExceeded =>
        Error.TooManyRequests(
            "InvoiceExtraction.Service.QuotaExceeded",
            "Le quota du service d'extraction est depasse pour la cle API configuree.");

    public static Error InvalidApiKey =>
        Error.Unauthorized(
            "InvoiceExtraction.Service.InvalidApiKey",
            "La cle API du service d'extraction est invalide ou n'a pas acces au modele.");

    public static Error EmptyResponse =>
        Error.Failure("InvoiceExtraction.Response.Empty", "Le service d'extraction n'a retourne aucune donnee.");

    public static Error InvalidResponse =>
        Error.Failure("InvoiceExtraction.Response.Invalid", "Le service d'extraction a retourne une reponse invalide.");

    public static Error NoDataFound =>
        Error.Failure("InvoiceExtraction.Response.NoDataFound", "Aucune donnee de facture n'a ete detectee dans l'image.");
}
