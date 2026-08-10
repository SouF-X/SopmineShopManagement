using FluentValidation;

namespace SopmineWorkshop.Application.Features.Invoices.Queries.GetInvoiceById;

public sealed class GetInvoiceByIdQueryValidator : AbstractValidator<GetInvoiceByIdQuery>
{
    public GetInvoiceByIdQueryValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("L'identifiant du document est obligatoire.");
    }
}
