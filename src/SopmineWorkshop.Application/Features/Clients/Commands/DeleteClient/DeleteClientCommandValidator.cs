using FluentValidation;

namespace SopmineWorkshop.Application.Features.Clients.Commands.DeleteClient;

public sealed class DeleteClientCommandValidator : AbstractValidator<DeleteClientCommand>
{
    public DeleteClientCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("L'identifiant du client est obligatoire.");
    }
}