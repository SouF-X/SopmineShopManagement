namespace SopmineWorkshop.Contracts.Requests.Fournisseurs;

public sealed class UpdateFournisseurRequest
{
    public string Nom { get; set; } = string.Empty;

    public string ICE { get; set; } = string.Empty;

    public string Adresse { get; set; } = string.Empty;

    public string Ville { get; set; } = string.Empty;

    public string TelFix { get; set; } = string.Empty;

    public string? SiteWeb { get; set; }

    public string? Email { get; set; }

    public List<UpdateContactFournisseurRequest> Contacts { get; set; } = [];
}
