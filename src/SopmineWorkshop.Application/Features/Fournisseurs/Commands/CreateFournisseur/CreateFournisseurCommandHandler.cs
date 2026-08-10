using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Application.Features.Fournisseurs.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.CreateFournisseur;

public sealed class CreateFournisseurCommandHandler(
    ILogger<CreateFournisseurCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<CreateFournisseurCommand, Result<FournisseurDto>>
{
    private readonly ILogger<CreateFournisseurCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<FournisseurDto>> Handle(CreateFournisseurCommand command, CancellationToken ct)
    {
        var normalizedName = Normalize(command.Nom).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedName) &&
            await _context.Fournisseurs.AnyAsync(
                f => f.Nom != null && f.Nom.ToLower() == normalizedName,
                ct);

        if (exists)
        {
            _logger.LogWarning("Fournisseur creation aborted. Name already exists.");

            return FournisseurErrors.AlreadyExists;
        }

        var fournisseurId = Guid.NewGuid();

        List<ContactFournisseur> contacts = [];

        foreach (var contactCommand in command.Contacts ?? [])
        {
            var createContactResult = ContactFournisseur.Create(
                Guid.NewGuid(),
                fournisseurId,
                Normalize(contactCommand.Nom),
                Normalize(contactCommand.Tel),
                contactCommand.Role);

            if (createContactResult.IsError)
            {
                return createContactResult.Errors;
            }

            contacts.Add(createContactResult.Value);
        }

        var createFournisseurResult = Fournisseur.Create(
            fournisseurId,
            Normalize(command.Nom),
            Normalize(command.ICE),
            Normalize(command.Adresse),
            Normalize(command.Ville),
            Normalize(command.TelFix),
            NormalizeOptional(command.SiteWeb),
            NormalizeOptional(command.Email),
            contacts);

        if (createFournisseurResult.IsError)
        {
            return createFournisseurResult.Errors;
        }

        var fournisseur = createFournisseurResult.Value;

        _context.Fournisseurs.Add(fournisseur);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("fournisseurs", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Fournisseur created successfully. Id: {FournisseurId}", fournisseur.Id);

        return fournisseur.ToDto();
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
    {
        var normalized = Normalize(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
