using MediatR;

using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.DocumentNominations.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Settings;

namespace SopmineWorkshop.Application.Features.DocumentNominations.Queries.GetDocumentNominations;

public sealed class GetDocumentNominationsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetDocumentNominationsQuery, Result<List<DocumentNominationDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<DocumentNominationDto>>> Handle(
        GetDocumentNominationsQuery query,
        CancellationToken ct)
    {
        var savedSettings = await _context.DocumentNominations
            .AsNoTracking()
            .ToListAsync(ct);

        var definitions = query.CanAccessPurchases
            ? DocumentNominationCatalog.Definitions
            : DocumentNominationCatalog.Definitions
                .Where(definition => definition.Nature != InvoiceNature.Achat);

        return definitions
            .Select(definition => DocumentNominationMapper.ToDto(
                definition,
                savedSettings.FirstOrDefault(setting =>
                    setting.Nature == definition.Nature &&
                    setting.Type == definition.Type)))
            .ToList();
    }
}
