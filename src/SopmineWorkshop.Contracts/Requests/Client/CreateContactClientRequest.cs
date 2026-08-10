using SopmineWorkshop.Contracts.Common.Client;

namespace SopmineWorkshop.Contracts.Requests.Clients;

public sealed class CreateContactClientRequest
{
    public string Nom { get; set; } = string.Empty;

    public string Tel { get; set; } = string.Empty;

    public ContactClientRole Role { get; set; }
}
