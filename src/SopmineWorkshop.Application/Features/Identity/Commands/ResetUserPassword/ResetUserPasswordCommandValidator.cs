using FluentValidation;

namespace SopmineWorkshop.Application.Features.Identity.Commands.ResetUserPassword;

public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.NewPassword)
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
    }
}
