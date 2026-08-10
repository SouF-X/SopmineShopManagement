using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Clients.Contacts;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Clients.Commands.UpdateClient;

public sealed class UpdateClientCommandHandler(
    ILogger<UpdateClientCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<UpdateClientCommand, Result<Updated>>
{
    private readonly ILogger<UpdateClientCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Updated>> Handle(UpdateClientCommand command, CancellationToken ct)
    {
        var client = await _context.Clients
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == command.ClientId, ct);

        if (client is null)
        {
            _logger.LogWarning("Client {ClientId} not found for update", command.ClientId);
            return ClientErrors.NotFound;
        }

        var normalizedName = Normalize(command.Nom).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedName) &&
            await _context.Clients.AnyAsync(
                c => c.Id != command.ClientId &&
                     c.Nom != null &&
                     c.Nom.ToLower() == normalizedName,
                ct);

        if (exists)
        {
            _logger.LogWarning("Client update aborted. Name already exists.");
            return ClientErrors.AlreadyExists;
        }

        List<ContactClient> validatedContacts = [];

        foreach (var contactCommand in command.Contacts ?? [])
        {
            var contactId = contactCommand.ContactClientId;

            var contactResult = ContactClient.Create(
                contactId,
                client.Id,
                Normalize(contactCommand.Nom),
                Normalize(contactCommand.Tel),
                contactCommand.Role);

            if (contactResult.IsError)
                return contactResult.Errors;

            validatedContacts.Add(contactResult.Value);
        }

        var updateClientResult = client.Update(
            Normalize(command.Nom),
            command.Type,
            command.ICE,
            command.Adresse,
            command.Ville,
            Normalize(command.Tel));

        if (updateClientResult.IsError)
            return updateClientResult.Errors;

        var upsertContactsResult = client.UpsertContacts(validatedContacts);

        if (upsertContactsResult.IsError)
            return upsertContactsResult.Errors;

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("clients", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Client {ClientId} updated successfully", client.Id);

        return Result.Updated;
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
