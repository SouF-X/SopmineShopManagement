using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Clients.Dtos;
using SopmineWorkshop.Application.Features.Clients.Mappers;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Clients.Contacts;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Clients.Commands.CreateClient;

public sealed class CreateClientCommandHandler(
    ILogger<CreateClientCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<CreateClientCommand, Result<ClientDto>>
{
    private readonly ILogger<CreateClientCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<ClientDto>> Handle(CreateClientCommand command, CancellationToken ct)
    {
        var normalizedName = Normalize(command.Nom).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedName) &&
            await _context.Clients.AnyAsync(
                c => c.Nom != null && c.Nom.ToLower() == normalizedName,
                ct);

        if (exists)
        {
            _logger.LogWarning("Client creation aborted. Name already exists.");
            return ClientErrors.AlreadyExists;
        }

        var clientId = Guid.NewGuid();

        List<ContactClient> contacts = [];

        foreach (var contactCommand in command.Contacts ?? [])
        {
            var createContactResult = ContactClient.Create(
                Guid.NewGuid(),
                clientId,
                Normalize(contactCommand.Nom),
                Normalize(contactCommand.Tel),
                contactCommand.Role);

            if (createContactResult.IsError)
                return createContactResult.Errors;

            contacts.Add(createContactResult.Value);
        }

        var createClientResult = Client.Create(
            clientId,
            Normalize(command.Nom),
            command.Type,
            command.ICE,
            command.Adresse,
            command.Ville,
            Normalize(command.Tel),
            contacts);

        if (createClientResult.IsError)
            return createClientResult.Errors;

        var client = createClientResult.Value;

        _context.Clients.Add(client);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("clients", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Client created successfully. Id: {ClientId}", client.Id);

        return client.ToDto();
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
