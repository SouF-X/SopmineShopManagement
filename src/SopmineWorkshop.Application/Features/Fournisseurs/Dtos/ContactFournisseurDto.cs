using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Dtos;

public class ContactFournisseurDto
{
    public Guid ContactFournisseurId { get; set; }
    public string? Nom { get; set; }
    public string? Tel { get; set; }
    public ContactRole Role { get; set; }
}
