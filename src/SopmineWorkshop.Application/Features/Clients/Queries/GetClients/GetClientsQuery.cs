using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Clients.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Clients.Queries.GetClients;

public sealed record GetClientsQuery : ICachedQuery<Result<List<ClientDto>>>
{
    public string CacheKey => "clients";

    public string[] Tags => ["clients"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
