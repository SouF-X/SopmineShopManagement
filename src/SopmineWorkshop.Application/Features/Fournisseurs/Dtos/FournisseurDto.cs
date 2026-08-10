namespace SopmineWorkshop.Application.Features.Fournisseurs.Dtos;

public class FournisseurDto
{
    public Guid FournisseurId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? Nom { get; set; }
    public string? ICE { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? TelFix { get; set; }
    public string? SiteWeb { get; set; }
    public string? Email { get; set; }
    public List<ContactFournisseurDto> Contacts { get; set; } = [];
}
