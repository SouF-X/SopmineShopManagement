using SopmineWorkshop.Domain.Clients.Contacts;
using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Domain.Clients;

public sealed class Client : AuditableEntity
{
    private readonly List<ContactClient> _contacts = [];

    public string? Nom { get; private set; }
    public ClientType Type { get; private set; }
    public string? ICE { get; private set; }
    public string? Adresse { get; private set; }
    public string? Ville { get; private set; }
    public string? Tel { get; private set; }
    public IReadOnlyCollection<ContactClient> Contacts => _contacts.AsReadOnly();

    private Client() { }

    private Client(
        Guid id,
        string nom,
        ClientType type,
        string? ice,
        string? adresse,
        string? ville,
        string tel,
        List<ContactClient> contacts)
        : base(id)
    {
        Nom = nom;
        Type = type;
        ICE = ice;
        Adresse = adresse;
        Ville = ville;
        Tel = tel;
        _contacts = contacts;
    }

    public static Result<Client> Create(
        Guid id,
        string nom,
        ClientType type,
        string? ice,
        string? adresse,
        string? ville,
        string tel,
        List<ContactClient>? contacts = null)
    {
        if (!Enum.IsDefined(type))
            return ClientErrors.TypeInvalid;

        return new Client(
            id,
            NormalizeRequired(nom),
            type,
            NormalizeNullable(ice),
            NormalizeNullable(adresse),
            NormalizeNullable(ville),
            NormalizeRequired(tel),
            contacts ?? []);
    }

    public Result<Updated> Update(
        string nom,
        ClientType type,
        string? ice,
        string? adresse,
        string? ville,
        string tel)
    {
        if (!Enum.IsDefined(type))
            return ClientErrors.TypeInvalid;

        Nom = NormalizeRequired(nom);
        Type = type;
        ICE = NormalizeNullable(ice);
        Adresse = NormalizeNullable(adresse);
        Ville = NormalizeNullable(ville);
        Tel = NormalizeRequired(tel);

        return Result.Updated;
    }

    public Result<Updated> UpsertContacts(List<ContactClient> incomingContacts)
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

    private static string NormalizeRequired(string? value)
        => value?.Trim() ?? string.Empty;

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
