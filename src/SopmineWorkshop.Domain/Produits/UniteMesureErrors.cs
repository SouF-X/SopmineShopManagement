using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Produits;

public static class UniteMesureErrors
{
    public static Error LibelleRequired =>
        Error.Validation("UniteMesure.Libelle.Required", "L'unite de mesure est obligatoire.");

    public static Error AlreadyExists =>
        Error.Conflict("UniteMesure.AlreadyExists", "Cette unite de mesure existe deja.");

    public static Error NotFound =>
        Error.NotFound("UniteMesure.NotFound", "Unite de mesure introuvable.");

    public static Error InUseByProducts =>
        Error.Conflict("UniteMesure.InUseByProducts", "Cette unite de mesure est utilisee par des produits. Elle ne peut pas etre supprimee.");
}
