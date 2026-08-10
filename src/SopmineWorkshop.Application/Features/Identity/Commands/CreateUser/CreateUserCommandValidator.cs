using FluentValidation;

using SopmineWorkshop.Domain.Identity;

namespace SopmineWorkshop.Application.Features.Identity.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(100)
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Password must contain at least one symbol.");

        RuleFor(command => command.Role)
            .NotEmpty()
            .Must(role => Enum.TryParse<Role>(role, true, out _))
            .WithMessage("Role must be Admin or Employee.");
    }
}
