using FluentValidation;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Queries.GetFournisseurById;

public sealed class GetFournisseurByIdQueryValidator : AbstractValidator<GetFournisseurByIdQuery>
{
    public GetFournisseurByIdQueryValidator()
    {
        RuleFor(request => request.FournisseurId)
            .NotEmpty()
            .WithErrorCode("FournisseurId_Is_Required")
            .WithMessage("FournisseurId is required.");
    }
}
