using FluentValidation;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.UpdateInvoice;

public sealed class UpdateInvoiceLineCommandValidator : AbstractValidator<UpdateInvoiceLineCommand>
{
    public UpdateInvoiceLineCommandValidator()
    {
    }
}
