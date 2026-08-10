using FluentValidation;

using SopmineWorkshop.Domain.Identity;

namespace SopmineWorkshop.Application.Features.Identity.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Role)
            .NotEmpty()
            .Must(role => Enum.TryParse<Role>(role, true, out _))
            .WithMessage("Role must be Admin or Employee.");
    }
}
