using MediatR;

using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Application.Features.Invoices.Mappers;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Invoices.Queries.GetInvoices;

public sealed class GetInvoicesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetInvoicesQuery, Result<List<InvoiceDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<InvoiceDto>>> Handle(GetInvoicesQuery query, CancellationToken ct)
    {
        var invoices = await _context.Invoices
            .Include(document => document.Lines)
            .Include(document => document.Payments)
            .AsNoTracking()
            .OrderByDescending(document => document.CreatedAtUtc)
            .ThenByDescending(document => document.Date)
            .ThenByDescending(document => document.Id)
            .ToListAsync(ct);

        return invoices.ToDtos(DateTime.UtcNow);
    }
}
