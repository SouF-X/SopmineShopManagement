using FluentValidation;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.DeleteInvoice;

public sealed class DeleteInvoiceCommandValidator : AbstractValidator<DeleteInvoiceCommand>
{
    public DeleteInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("L'identifiant du document est obligatoire.");
    }
}
