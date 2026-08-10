using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Domain.Clients.Contacts;

public sealed class ContactClient : AuditableEntity
{
    public Guid ClientId { get; private set; }
    public string? Nom { get; private set; }
    public string? Tel { get; private set; }
    public ContactClientRole Role { get; private set; }
    public Client? Client { get; set; }

    private ContactClient() { }

    private ContactClient(Guid id, Guid clientId, string nom, string tel, ContactClientRole role)
        : base(id)
    {
        ClientId = clientId;
        Nom = nom;
        Tel = tel;
        Role = role;
    }

    public static Result<ContactClient> Create(Guid id, Guid clientId, string nom, string tel, ContactClientRole role)
    {
        if (clientId == Guid.Empty)
            return ContactClientErrors.ClientIdRequired;

        if (!Enum.IsDefined(role))
            return ContactClientErrors.RoleInvalid;

        return new ContactClient(id, clientId, NormalizeText(nom), NormalizeText(tel), role);
    }

    public Result<Updated> Update(string nom, string tel, ContactClientRole role)
    {
        if (!Enum.IsDefined(role))
            return ContactClientErrors.RoleInvalid;

        Nom = NormalizeText(nom);
        Tel = NormalizeText(tel);
        Role = role;

        return Result.Updated;
    }

    private static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;
}
