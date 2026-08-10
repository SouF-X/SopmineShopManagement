using FluentValidation;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.ConvertBonLivraisons;

public sealed class ConvertBonLivraisonsCommandValidator : AbstractValidator<ConvertBonLivraisonsCommand>
{
    public ConvertBonLivraisonsCommandValidator()
    {
        RuleFor(x => x.InvoiceIds)
            .NotNull().WithMessage("Selectionne au moins un bon de livraison.")
            .NotEmpty().WithMessage("Selectionne au moins un bon de livraison.");

        RuleForEach(x => x.InvoiceIds)
            .NotEmpty().WithMessage("L'identifiant du bon de livraison est invalide.");
    }
}
