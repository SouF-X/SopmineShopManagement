using FluentValidation;

namespace SopmineWorkshop.Application.Features.Produits.Commands.UpdateProduit;

public sealed class UpdateProduitCommandValidator : AbstractValidator<UpdateProduitCommand>
{
    public UpdateProduitCommandValidator()
    {
    }
}
