using MediatR;

using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Clients.Dtos;
using SopmineWorkshop.Application.Features.Clients.Mappers;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Clients.Queries.GetClients;

public sealed class GetClientsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetClientsQuery, Result<List<ClientDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<ClientDto>>> Handle(GetClientsQuery query, CancellationToken ct)
    {
        var clients = await _context.Clients
            .Include(c => c.Contacts)
            .AsNoTracking()
            .ToListAsync(ct);

        return clients.ToDtos();
    }
}