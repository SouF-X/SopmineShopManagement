using MediatR;
using Microsoft.EntityFrameworkCore;
using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.InvoicePayments.Dtos;
using SopmineWorkshop.Application.Features.InvoicePayments.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Application.Features.InvoicePayments.Queries.GetInvoicePayments;
public sealed class GetInvoicePaymentsQueryHandler(IAppDbContext context) : IRequestHandler<GetInvoicePaymentsQuery, Result<List<InvoicePaymentDto>>>
{
    public async Task<Result<List<InvoicePaymentDto>>> Handle(GetInvoicePaymentsQuery query, CancellationToken ct)
    {
        if (!await context.Invoices.AnyAsync(x => x.Id == query.InvoiceId, ct)) return InvoiceErrors.NotFound;
        var payments = await context.InvoicePayments.AsNoTracking().Where(x => x.InvoiceId == query.InvoiceId)
            .OrderBy(x => x.PaymentDate).ThenBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
        return payments.Select(x => x.ToDto()).ToList();
    }
}
