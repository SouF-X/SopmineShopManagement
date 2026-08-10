using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Produits;

public sealed class UniteMesure : AuditableEntity
{
    public string Libelle { get; private set; } = string.Empty;

    private UniteMesure() { }

    private UniteMesure(Guid id, string libelle) : base(id)
    {
        Libelle = libelle;
    }

    public static Result<UniteMesure> Create(Guid id, string libelle)
    {
        return new UniteMesure(id, NormalizeText(libelle));
    }

    public Result<Updated> Rename(string libelle)
    {
        Libelle = NormalizeText(libelle);

        return Result.Updated;
    }

    private static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;
}
