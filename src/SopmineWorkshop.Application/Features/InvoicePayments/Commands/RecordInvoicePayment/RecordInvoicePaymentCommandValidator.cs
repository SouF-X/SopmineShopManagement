using FluentValidation;
using SopmineWorkshop.Domain.Enums;
namespace SopmineWorkshop.Application.Features.InvoicePayments.Commands.RecordInvoicePayment;
public sealed class RecordInvoicePaymentCommandValidator : AbstractValidator<RecordInvoicePaymentCommand>
{
    public RecordInvoicePaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty(); RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentDate).NotEqual(default(DateTime)); RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Reference).MaximumLength(100); RuleFor(x => x.Note).MaximumLength(500);
    }
}
