using FluentValidation;

namespace SopmineWorkshop.Application.Features.Identity.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryValidator : AbstractValidator<GetCurrentUserQuery>
{
    public GetCurrentUserQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
}
