using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Clients.Dtos;

public class ContactClientDto
{
    public Guid ContactClientId { get; set; }
    public string? Nom { get; set; }
    public string? Tel { get; set; }
    public ContactClientRole Role { get; set; }
}