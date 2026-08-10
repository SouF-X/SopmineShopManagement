using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Clients.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Clients.Queries.GetClientById;

public sealed record GetClientByIdQuery(Guid ClientId) : ICachedQuery<Result<ClientDto>>
{
    public string CacheKey => $"clients:{ClientId}";

    public string[] Tags => ["clients"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
