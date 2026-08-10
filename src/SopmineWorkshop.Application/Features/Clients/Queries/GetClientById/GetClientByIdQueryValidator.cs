using FluentValidation;

namespace SopmineWorkshop.Application.Features.Clients.Queries.GetClientById;

public sealed class GetClientByIdQueryValidator : AbstractValidator<GetClientByIdQuery>
{
    public GetClientByIdQueryValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("L'identifiant du client est obligatoire.");
    }
}