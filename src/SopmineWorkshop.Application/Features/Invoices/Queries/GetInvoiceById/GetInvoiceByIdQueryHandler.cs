using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Application.Features.Invoices.Mappers;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Application.Features.Invoices.Queries.GetInvoiceById;

public sealed class GetInvoiceByIdQueryHandler(
    ILogger<GetInvoiceByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    private readonly ILogger<GetInvoiceByIdQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<InvoiceDto>> Handle(GetInvoiceByIdQuery query, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(document => document.Lines)
            .Include(document => document.Payments)
            .FirstOrDefaultAsync(document => document.Id == query.InvoiceId, ct);

        if (invoice is null)
        {
            _logger.LogWarning("Invoice with id {InvoiceId} was not found", query.InvoiceId);
            return InvoiceErrors.NotFound;
        }

        return invoice.ToDto(DateTime.UtcNow);
    }
}
