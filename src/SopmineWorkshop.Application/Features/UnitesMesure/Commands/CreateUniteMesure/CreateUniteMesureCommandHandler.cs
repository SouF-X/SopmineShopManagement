using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.UnitesMesure.Dtos;
using SopmineWorkshop.Application.Features.UnitesMesure.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.UnitesMesure.Commands.CreateUniteMesure;

public sealed class CreateUniteMesureCommandHandler(
    ILogger<CreateUniteMesureCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<CreateUniteMesureCommand, Result<UniteMesureDto>>
{
    private readonly ILogger<CreateUniteMesureCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<UniteMesureDto>> Handle(CreateUniteMesureCommand command, CancellationToken ct)
    {
        var normalizedLibelle = Normalize(command.Libelle).ToLower();

        var exists = !string.IsNullOrWhiteSpace(normalizedLibelle) &&
            await _context.UnitesMesure.AnyAsync(
                u => u.Libelle != null && u.Libelle.ToLower() == normalizedLibelle,
                ct);

        if (exists)
        {
            _logger.LogWarning("Unite de mesure creation aborted. Libelle already exists.");
            return UniteMesureErrors.AlreadyExists;
        }

        var createResult = UniteMesure.Create(Guid.NewGuid(), Normalize(command.Libelle));

        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var unite = createResult.Value;

        _context.UnitesMesure.Add(unite);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("unites-mesure", ct);
        await _cache.RemoveByTagAsync("produits", ct);

        _logger.LogInformation("Unite de mesure created successfully. Id: {UniteMesureId}", unite.Id);

        return unite.ToDto();
    }

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;
}
