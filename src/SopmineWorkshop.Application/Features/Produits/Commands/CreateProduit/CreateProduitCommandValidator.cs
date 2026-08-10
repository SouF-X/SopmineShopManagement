using FluentValidation;

namespace SopmineWorkshop.Application.Features.Produits.Commands.CreateProduit;

public sealed class CreateProduitCommandValidator : AbstractValidator<CreateProduitCommand>
{
    public CreateProduitCommandValidator()
    {
    }
}
