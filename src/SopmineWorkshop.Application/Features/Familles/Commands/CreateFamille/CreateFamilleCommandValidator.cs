using FluentValidation;

namespace SopmineWorkshop.Application.Features.Familles.Commands.CreateFamille;

public sealed class CreateFamilleCommandValidator : AbstractValidator<CreateFamilleCommand>
{
    public CreateFamilleCommandValidator()
    {
    }
}
