using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;

public sealed class ContactFournisseur : AuditableEntity
{
    public Guid FournisseurId { get; private set; }
    public string? Nom { get; private set; }
    public string? Tel { get; private set; }
    public ContactRole Role { get; private set; }
    public Fournisseur? Fournisseur { get; set; }


    private ContactFournisseur()
    {
    }

    private ContactFournisseur(Guid id, Guid fournisseurId, string nom, string tel, ContactRole role)
        : base(id)
    {
        FournisseurId = fournisseurId;
        Nom = nom;
        Tel = tel;
        Role = role;
    }

    public static Result<ContactFournisseur> Create(Guid id, Guid fournisseurId, string nom, string tel, ContactRole role)
    {
        if (!Enum.IsDefined(role))
            return ContactFournisseurErrors.RoleInvalid;

        return new ContactFournisseur(id, fournisseurId, NormalizeText(nom), NormalizeText(tel), role);
    }

    public Result<Updated> Update(string nom, string tel, ContactRole role)
    {
        if (!Enum.IsDefined(role))
            return ContactFournisseurErrors.RoleInvalid;

        Nom = NormalizeText(nom);
        Tel = NormalizeText(tel);
        Role = role;

        return Result.Updated;
    }

    private static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;
}
