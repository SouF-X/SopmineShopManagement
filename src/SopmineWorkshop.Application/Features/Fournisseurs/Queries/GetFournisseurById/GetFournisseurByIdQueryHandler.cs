using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Application.Features.Fournisseurs.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Fournisseurs;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Queries.GetFournisseurById;

public sealed class GetFournisseurByIdQueryHandler(
    ILogger<GetFournisseurByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetFournisseurByIdQuery, Result<FournisseurDto>>
{
    private readonly ILogger<GetFournisseurByIdQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<FournisseurDto>> Handle(GetFournisseurByIdQuery query, CancellationToken ct)
    {
        var fournisseur = await _context.Fournisseurs
            .AsNoTracking()
            .Include(f => f.Contacts)
            .FirstOrDefaultAsync(f => f.Id == query.FournisseurId, ct);

        if (fournisseur is null)
        {
            _logger.LogWarning("Fournisseur with id {FournisseurId} was not found", query.FournisseurId);
            return FournisseurErrors.NotFound;
        }

        return fournisseur.ToDto();
    }
}
