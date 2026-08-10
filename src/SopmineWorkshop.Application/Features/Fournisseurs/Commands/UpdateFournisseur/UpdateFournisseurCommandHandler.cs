using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.UpdateFournisseur;

public sealed class UpdateFournisseurCommandHandler(
    ILogger<UpdateFournisseurCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<UpdateFournisseurCommand, Result<Updated>>
{
    private readonly ILogger<UpdateFournisseurCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Updated>> Handle(UpdateFournisseurCommand command, CancellationToken ct)
    {
        var fournisseur = await _context.Fournisseurs
            .Include(f => f.Contacts)
            .FirstOrDefaultAsync(f => f.Id == command.FournisseurId, ct);

        if (fournisseur is null)
        {
            _logger.LogWarning("Fournisseur {FournisseurId} not found for update", command.FournisseurId);
            return FournisseurErrors.NotFound;
        }

        var normalizedName = Normalize(command.Nom).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedName) &&
            await _context.Fournisseurs.AnyAsync(
                f => f.Id != command.FournisseurId &&
                     f.Nom != null &&
                     f.Nom.ToLower() == normalizedName,
                ct);

        if (exists)
        {
            _logger.LogWarning("Fournisseur update aborted. Name already exists.");
            return FournisseurErrors.AlreadyExists;
        }

        List<ContactFournisseur> validatedContacts = [];

        foreach (var contactCommand in command.Contacts ?? [])
        {
            var contactId = contactCommand.ContactFournisseurId ?? Guid.NewGuid();

            var contactResult = ContactFournisseur.Create(
                contactId,
                fournisseur.Id,
                Normalize(contactCommand.Nom),
                Normalize(contactCommand.Tel),
                contactCommand.Role);

            if (contactResult.IsError)
            {
                return contactResult.Errors;
            }

            validatedContacts.Add(contactResult.Value);
        }

        var updateFournisseurResult = fournisseur.Update(
            Normalize(command.Nom),
            Normalize(command.ICE),
            Normalize(command.Adresse),
            Normalize(command.Ville),
            Normalize(command.TelFix),
            NormalizeOptional(command.SiteWeb),
            NormalizeOptional(command.Email));

        if (updateFournisseurResult.IsError)
        {
            return updateFournisseurResult.Errors;
        }

        var upsertContactsResult = fournisseur.UpsertContacts(validatedContacts);

        if (upsertContactsResult.IsError)
        {
            return upsertContactsResult.Errors;
        }

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("fournisseurs", ct);
        await _cache.RemoveByTagAsync("produits", ct);
        await _cache.RemoveByTagAsync("invoices", ct);

        _logger.LogInformation("Fournisseur {FournisseurId} updated successfully", fournisseur.Id);

        return Result.Updated;
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
    {
        var normalized = Normalize(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
