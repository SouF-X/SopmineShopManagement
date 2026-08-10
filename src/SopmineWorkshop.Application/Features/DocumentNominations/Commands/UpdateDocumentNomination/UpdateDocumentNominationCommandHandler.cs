using MediatR;

using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.DocumentNominations.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Settings;

namespace SopmineWorkshop.Application.Features.DocumentNominations.Commands.UpdateDocumentNomination;

public sealed class UpdateDocumentNominationCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateDocumentNominationCommand, Result<DocumentNominationDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<DocumentNominationDto>> Handle(
        UpdateDocumentNominationCommand command,
        CancellationToken ct)
    {
        var definition = DocumentNominationCatalog.Find(command.Key);
        if (definition is null)
        {
            return DocumentNominationErrors.NotFound;
        }

        if (definition.Nature == InvoiceNature.Achat && !command.CanAccessPurchases)
        {
            return DocumentNominationErrors.Forbidden;
        }

        var root = DocumentNominationRules.NormalizeRoot(command.Root);
        var dateFormat = DocumentNominationRules.NormalizeDateFormat(command.DateFormat);

        if (root.Length > 30)
        {
            return DocumentNominationErrors.RootTooLong;
        }

        if (!DocumentNominationRules.IsSupportedDateFormat(dateFormat))
        {
            return DocumentNominationErrors.DateFormatInvalid;
        }

        var setting = await _context.DocumentNominations
            .FirstOrDefaultAsync(item =>
                item.Nature == definition.Nature &&
                item.Type == definition.Type,
                ct);

        if (setting is null)
        {
            setting = new DocumentNomination(
                Guid.NewGuid(),
                definition.Nature,
                definition.Type,
                root,
                dateFormat,
                Math.Clamp(command.IncrementSize, 1, 8));

            _context.DocumentNominations.Add(setting);
        }
        else
        {
            setting.Update(
                root,
                dateFormat,
                Math.Clamp(command.IncrementSize, 1, 8));
        }

        await _context.SaveChangesAsync(ct);

        return DocumentNominationMapper.ToDto(definition, setting);
    }
}
