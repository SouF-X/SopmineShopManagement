using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;

namespace SopmineWorkshop.Domain.Fournisseurs;

public sealed class Fournisseur : AuditableEntity
{
    public string? Nom { get; private set; }
    public string? ICE { get; private set; }
    public string? Adresse { get; private set; }
    public string? Ville { get; private set; }
    public string? TelFix { get; private set; }
    public string? SiteWeb { get; private set; }
    public string? Email { get; private set; }

    private readonly List<ContactFournisseur> _contacts = [];
    public IEnumerable<ContactFournisseur> Contacts => _contacts.AsReadOnly();

    private Fournisseur()
    {
    }

    private Fournisseur(
        Guid id,
        string nom,
        string ice,
        string adresse,
        string ville,
        string telFix,
        string? siteWeb,
        string? email,
        List<ContactFournisseur> contacts)
        : base(id)
    {
        Nom = nom;
        ICE = ice;
        Adresse = adresse;
        Ville = ville;
        TelFix = telFix;
        SiteWeb = siteWeb;
        Email = email;
        _contacts = contacts;
    }

    public static Result<Fournisseur> Create(
        Guid id,
        string nom,
        string ice,
        string adresse,
        string ville,
        string telFix,
        string? siteWeb,
        string? email,
        List<ContactFournisseur> contacts)
    {
        return new Fournisseur(
            id,
            NormalizeRequiredText(nom),
            NormalizeRequiredText(ice),
            NormalizeRequiredText(adresse),
            NormalizeRequiredText(ville),
            NormalizeRequiredText(telFix),
            NormalizeOptionalText(siteWeb),
            NormalizeOptionalText(email),
            contacts ?? []);
    }

    public Result<Updated> Update(
        string nom,
        string ice,
        string adresse,
        string ville,
        string telFix,
        string? siteWeb,
        string? email)
    {
        Nom = NormalizeRequiredText(nom);
        ICE = NormalizeRequiredText(ice);
        Adresse = NormalizeRequiredText(adresse);
        Ville = NormalizeRequiredText(ville);
        TelFix = NormalizeRequiredText(telFix);
        SiteWeb = NormalizeOptionalText(siteWeb);
        Email = NormalizeOptionalText(email);

        return Result.Updated;
    }

    public Result<Updated> UpsertContacts(List<ContactFournisseur> incomingContacts)
    {
        incomingContacts ??= [];

        _contacts.RemoveAll(existing => incomingContacts.All(c => c.Id != existing.Id));

        foreach (var incoming in incomingContacts)
        {
            var existing = _contacts.FirstOrDefault(c => c.Id == incoming.Id);

            if (existing is null)
            {
                _contacts.Add(incoming);
            }
            else
            {
                var updateResult = existing.Update(incoming.Nom ?? string.Empty, incoming.Tel ?? string.Empty, incoming.Role);

                if (updateResult.IsError)
                    return updateResult.Errors;
            }
        }

        return Result.Updated;
    }

    private static string NormalizeRequiredText(string? value)
        => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptionalText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
