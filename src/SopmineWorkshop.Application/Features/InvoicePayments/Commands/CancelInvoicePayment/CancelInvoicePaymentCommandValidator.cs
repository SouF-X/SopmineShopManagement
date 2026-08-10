using FluentValidation;
namespace SopmineWorkshop.Application.Features.InvoicePayments.Commands.CancelInvoicePayment;
public sealed class CancelInvoicePaymentCommandValidator : AbstractValidator<CancelInvoicePaymentCommand>
{
    public CancelInvoicePaymentCommandValidator() { RuleFor(x => x.InvoiceId).NotEmpty(); RuleFor(x => x.PaymentId).NotEmpty(); RuleFor(x => x.Reason).MaximumLength(500); }
}
