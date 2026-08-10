using SopmineWorkshop.Application.Features.Clients.Dtos;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Clients.Contacts;

namespace SopmineWorkshop.Application.Features.Clients.Mappers;

public static class ClientMapper
{
    public static ClientDto ToDto(this Client entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ClientDto
        {
            ClientId = entity.Id,
            Nom = entity.Nom,
            Type = entity.Type,
            ICE = entity.ICE,
            Adresse = entity.Adresse,
            Ville = entity.Ville,
            Tel = entity.Tel,
            Contacts = entity.Contacts?.Select(c => c.ToDto()).ToList() ?? []
        };
    }

    public static List<ClientDto> ToDtos(this IEnumerable<Client> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }

    public static ContactClientDto ToDto(this ContactClient entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ContactClientDto
        {
            ContactClientId = entity.Id,
            Nom = entity.Nom,
            Tel = entity.Tel,
            Role = entity.Role
        };
    }
}