using FluentValidation;

namespace SopmineWorkshop.Application.Features.Produits.Commands.DeleteProduit;

public sealed class DeleteProduitCommandValidator : AbstractValidator<DeleteProduitCommand>
{
    public DeleteProduitCommandValidator()
    {
        RuleFor(x => x.ProduitId)
            .NotEmpty().WithMessage("L'identifiant du produit est obligatoire.");
    }
}