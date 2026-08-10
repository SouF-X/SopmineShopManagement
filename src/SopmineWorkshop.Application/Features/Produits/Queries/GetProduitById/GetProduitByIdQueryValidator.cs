using FluentValidation;

namespace SopmineWorkshop.Application.Features.Produits.Queries.GetProduitById;

public sealed class GetProduitByIdQueryValidator : AbstractValidator<GetProduitByIdQuery>
{
    public GetProduitByIdQueryValidator()
    {
        RuleFor(x => x.ProduitId)
            .NotEmpty().WithMessage("L'identifiant du produit est obligatoire.");
    }
}