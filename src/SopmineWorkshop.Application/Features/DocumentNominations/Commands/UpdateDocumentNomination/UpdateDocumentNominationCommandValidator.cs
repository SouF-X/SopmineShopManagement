using FluentValidation;

using SopmineWorkshop.Application.Features.DocumentNominations;
using SopmineWorkshop.Domain.Settings;

namespace SopmineWorkshop.Application.Features.DocumentNominations.Commands.UpdateDocumentNomination;

public sealed class UpdateDocumentNominationCommandValidator
    : AbstractValidator<UpdateDocumentNominationCommand>
{
    public UpdateDocumentNominationCommandValidator()
    {
        RuleFor(command => command.Root)
            .Must(root => DocumentNominationRules.NormalizeRoot(root).Length <= 30)
            .WithMessage(DocumentNominationErrors.RootTooLong.Description);

        RuleFor(command => command.DateFormat)
            .Must(dateFormat => DocumentNominationRules.IsSupportedDateFormat(
                DocumentNominationRules.NormalizeDateFormat(dateFormat)))
            .WithMessage(DocumentNominationErrors.DateFormatInvalid.Description);
    }
}
