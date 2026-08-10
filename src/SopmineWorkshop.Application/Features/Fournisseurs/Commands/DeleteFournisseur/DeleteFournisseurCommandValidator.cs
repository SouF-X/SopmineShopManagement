using FluentValidation;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.DeleteFournisseur;

public class RemoveFournisseurCommandValidator : AbstractValidator<DeleteFournisseurCommand>
{
    public RemoveFournisseurCommandValidator()
    {
        RuleFor(x => x.FournisseurId)
            .NotEmpty().WithMessage("Fournisseur Id is required.");
    }
}
