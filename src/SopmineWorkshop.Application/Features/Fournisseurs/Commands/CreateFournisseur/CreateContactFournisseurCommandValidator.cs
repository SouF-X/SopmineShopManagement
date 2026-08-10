using FluentValidation;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.CreateFournisseur;

public sealed class CreateContactFournisseurCommandValidator : AbstractValidator<CreateContactFournisseurCommand>
{
    public CreateContactFournisseurCommandValidator()
    {
    }
}
