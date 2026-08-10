using SopmineWorkshop.Contracts.Common.Client;

namespace SopmineWorkshop.Contracts.Requests.Clients;

public sealed class UpdateContactClientRequest
{
    public Guid? ContactClientId { get; set; }

    public string Nom { get; set; } = string.Empty;

    public string Tel { get; set; } = string.Empty;

    public ContactClientRole Role { get; set; }
}
