using FluentValidation;
namespace SopmineWorkshop.Application.Features.Statements.Queries.GetPartyStatement;
public sealed class GetPartyStatementQueryValidator : AbstractValidator<GetPartyStatementQuery>
{
 public GetPartyStatementQueryValidator() { RuleFor(x => x.PartyKind).IsInEnum(); RuleFor(x => x.PartyId).NotEmpty(); RuleFor(x => x).Must(x => !x.From.HasValue || !x.To.HasValue || x.From.Value.Date <= x.To.Value.Date).WithMessage("From must not be after To."); RuleFor(x => x.PaymentProgress).IsInEnum().When(x => x.PaymentProgress.HasValue); }
}
