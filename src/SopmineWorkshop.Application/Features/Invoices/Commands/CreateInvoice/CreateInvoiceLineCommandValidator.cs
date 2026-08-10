using FluentValidation;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.CreateInvoice;

public sealed class CreateInvoiceLineCommandValidator : AbstractValidator<CreateInvoiceLineCommand>
{
    public CreateInvoiceLineCommandValidator()
    {
    }
}
