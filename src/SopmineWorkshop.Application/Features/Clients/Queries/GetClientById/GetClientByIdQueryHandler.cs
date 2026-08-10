using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Clients.Dtos;
using SopmineWorkshop.Application.Features.Clients.Mappers;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Clients.Queries.GetClientById;

public sealed class GetClientByIdQueryHandler(
    ILogger<GetClientByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetClientByIdQuery, Result<ClientDto>>
{
    private readonly ILogger<GetClientByIdQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<ClientDto>> Handle(GetClientByIdQuery query, CancellationToken ct)
    {
        var client = await _context.Clients
            .AsNoTracking()
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == query.ClientId, ct);

        if (client is null)
        {
            _logger.LogWarning("Client with id {ClientId} was not found", query.ClientId);
            return ClientErrors.NotFound;
        }

        return client.ToDto();
    }
}