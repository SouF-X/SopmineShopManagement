using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Clients.Dtos;

public class ClientDto
{
    public Guid ClientId { get; set; }
    public string? Nom { get; set; }
    public ClientType Type { get; set; }
    public string? ICE { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Tel { get; set; }
    public List<ContactClientDto> Contacts { get; set; } = [];
}