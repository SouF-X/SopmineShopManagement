using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Familles.Dtos;
using SopmineWorkshop.Application.Features.Familles.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Familles.Commands.CreateFamille;

public sealed class CreateFamilleCommandHandler(
    ILogger<CreateFamilleCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<CreateFamilleCommand, Result<FamilleProduitDto>>
{
    private readonly ILogger<CreateFamilleCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<FamilleProduitDto>> Handle(CreateFamilleCommand command, CancellationToken ct)
    {
        var normalizedLibelle = Normalize(command.Libelle).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedLibelle) &&
            await _context.FamillesProduit.AnyAsync(
                f => f.Libelle != null && f.Libelle.ToLower() == normalizedLibelle,
                ct);

        if (exists)
        {
            _logger.LogWarning("Famille creation aborted. Libelle already exists.");
            return FamilleProduitErrors.AlreadyExists;
        }

        var createResult = FamilleProduit.Create(Guid.NewGuid(), Normalize(command.Libelle));

        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var famille = createResult.Value;

        _context.FamillesProduit.Add(famille);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("familles", ct);
        await _cache.RemoveByTagAsync("produits", ct);

        _logger.LogInformation("Famille created successfully. Id: {FamilleId}", famille.Id);

        return famille.ToDto();
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
