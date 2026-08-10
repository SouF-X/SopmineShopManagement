using FluentValidation;

namespace SopmineWorkshop.Application.Features.Identity.Commands.DeleteUser;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.CurrentUserId).NotEmpty();
    }
}
