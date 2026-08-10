using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Familles.Commands.DeleteFamille;

public sealed class DeleteFamilleCommandHandler(
    ILogger<DeleteFamilleCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<DeleteFamilleCommand, Result<Deleted>>
{
    private readonly ILogger<DeleteFamilleCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Deleted>> Handle(DeleteFamilleCommand command, CancellationToken ct)
    {
        var famille = await _context.FamillesProduit
            .FirstOrDefaultAsync(f => f.Id == command.FamilleId, ct);

        if (famille is null)
        {
            _logger.LogWarning("Famille {FamilleId} not found for deletion.", command.FamilleId);
            return FamilleProduitErrors.NotFound;
        }

        var normalizedLibelle = famille.Libelle.Trim().ToLower();
        var isUsedByProducts = await _context.Produits
            .AnyAsync(p => p.Famille != null && p.Famille.ToLower() == normalizedLibelle, ct);

        if (isUsedByProducts)
        {
            _logger.LogWarning("Famille {FamilleId} cannot be deleted because it is used by products.", command.FamilleId);
            return FamilleProduitErrors.InUseByProducts;
        }

        _context.FamillesProduit.Remove(famille);

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("familles", ct);
        await _cache.RemoveByTagAsync("produits", ct);

        _logger.LogInformation("Famille {FamilleId} deleted successfully.", command.FamilleId);

        return Result.Deleted;
    }
}
