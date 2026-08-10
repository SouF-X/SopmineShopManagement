using SopmineWorkshop.Contracts.Common;

namespace SopmineWorkshop.Contracts.Requests.Fournisseurs;

public sealed class UpdateContactFournisseurRequest
{
    public Guid? ContactFournisseurId { get; set; }

    public string Nom { get; set; } = string.Empty;

    public string Tel { get; set; } = string.Empty;

    public ContactRole Role { get; set; }
}
