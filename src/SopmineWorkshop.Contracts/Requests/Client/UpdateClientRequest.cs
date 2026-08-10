using SopmineWorkshop.Contracts.Common.Client;

namespace SopmineWorkshop.Contracts.Requests.Clients;

public sealed class UpdateClientRequest
{
    public string Nom { get; set; } = string.Empty;

    public ClientType Type { get; set; }

    public string? ICE { get; set; }

    public string? Adresse { get; set; }

    public string? Ville { get; set; }

    public string Tel { get; set; } = string.Empty;

    public List<UpdateContactClientRequest> Contacts { get; set; } = [];
}
