using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.UnitesMesure.Commands.DeleteUniteMesure;

public sealed class DeleteUniteMesureCommandHandler(
    ILogger<DeleteUniteMesureCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<DeleteUniteMesureCommand, Result<Deleted>>
{
    private readonly ILogger<DeleteUniteMesureCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Deleted>> Handle(DeleteUniteMesureCommand command, CancellationToken ct)
    {
        var unite = await _context.UnitesMesure
            .FirstOrDefaultAsync(u => u.Id == command.UniteMesureId, ct);

        if (unite is null)
        {
            _logger.LogWarning("Unite de mesure {UniteMesureId} not found for deletion.", command.UniteMesureId);
            return UniteMesureErrors.NotFound;
        }

        var normalizedLibelle = unite.Libelle.Trim().ToLower();
        var isUsedByProducts = await _context.Produits
            .AnyAsync(p => p.Unite != null && p.Unite.ToLower() == normalizedLibelle, ct);

        if (isUsedByProducts)
        {
            _logger.LogWarning("Unite de mesure {UniteMesureId} cannot be deleted because it is used by products.", command.UniteMesureId);
            return UniteMesureErrors.InUseByProducts;
        }

        _context.UnitesMesure.Remove(unite);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("unites-mesure", ct);
        await _cache.RemoveByTagAsync("produits", ct);

        _logger.LogInformation("Unite de mesure {UniteMesureId} deleted successfully.", command.UniteMesureId);

        return Result.Deleted;
    }
}
