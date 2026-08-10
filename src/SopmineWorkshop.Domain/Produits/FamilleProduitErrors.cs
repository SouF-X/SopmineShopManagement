using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Produits;

public static class FamilleProduitErrors
{
    public static Error LibelleRequired =>
        Error.Validation("FamilleProduit.Libelle.Required", "La famille est obligatoire.");

    public static Error AlreadyExists =>
        Error.Conflict("FamilleProduit.AlreadyExists", "Cette famille existe deja.");

    public static Error NotFound =>
        Error.NotFound("FamilleProduit.NotFound", "Famille introuvable.");

    public static Error InUseByProducts =>
        Error.Conflict("FamilleProduit.InUseByProducts", "Cette famille est utilisee par des produits. Elle ne peut pas etre supprimee.");
}
