using FluentValidation;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.CreateInvoice;

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
    }
}
