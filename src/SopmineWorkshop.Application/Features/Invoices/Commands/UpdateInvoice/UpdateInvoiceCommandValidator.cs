using FluentValidation;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.UpdateInvoice;

public sealed class UpdateInvoiceCommandValidator : AbstractValidator<UpdateInvoiceCommand>
{
    public UpdateInvoiceCommandValidator()
    {
    }
}
