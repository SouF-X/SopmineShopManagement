using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Clients.Commands.DeleteClient;

public sealed class DeleteClientCommandHandler(
    ILogger<DeleteClientCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<DeleteClientCommand, Result<Deleted>>
{
    private readonly ILogger<DeleteClientCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Deleted>> Handle(DeleteClientCommand command, CancellationToken ct)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == command.ClientId, ct);

        if (client is null)
        {
            _logger.LogWarning("Client with id {ClientId} not found for deletion.", command.ClientId);
            return ClientErrors.NotFound;
        }

        var isUsedInDocuments = await _context.Invoices
            .AnyAsync(invoice => invoice.ClientId == command.ClientId, ct);

        if (isUsedInDocuments)
        {
            _logger.LogWarning("Client {ClientId} cannot be deleted because it is used by invoices.", command.ClientId);
            return ClientErrors.InUseByDocuments;
        }

        _context.Clients.Remove(client);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("clients", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Client {ClientId} deleted successfully.", command.ClientId);

        return Result.Deleted;
    }
}
