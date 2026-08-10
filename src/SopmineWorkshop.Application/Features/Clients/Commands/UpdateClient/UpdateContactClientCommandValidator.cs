using FluentValidation;

namespace SopmineWorkshop.Application.Features.Clients.Commands.UpdateClient;

public sealed class UpdateContactClientCommandValidator : AbstractValidator<UpdateContactClientCommand>
{
    public UpdateContactClientCommandValidator()
    {
    }
}