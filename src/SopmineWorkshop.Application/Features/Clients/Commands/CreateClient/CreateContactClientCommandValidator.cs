using FluentValidation;

namespace SopmineWorkshop.Application.Features.Clients.Commands.CreateClient;

public sealed class CreateContactClientCommandValidator : AbstractValidator<CreateContactClientCommand>
{
    public CreateContactClientCommandValidator()
    {
    }
}