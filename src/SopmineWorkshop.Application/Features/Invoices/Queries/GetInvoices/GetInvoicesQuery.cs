using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Invoices.Queries.GetInvoices;

public sealed record GetInvoicesQuery : ICachedQuery<Result<List<InvoiceDto>>>
{
    public string CacheKey => "invoices";

    public string[] Tags => ["invoices"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
