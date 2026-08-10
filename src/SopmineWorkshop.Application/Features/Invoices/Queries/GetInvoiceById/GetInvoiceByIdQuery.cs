using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Invoices.Queries.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(Guid InvoiceId) : ICachedQuery<Result<InvoiceDto>>
{
    public string CacheKey => $"invoices:{InvoiceId}";

    public string[] Tags => ["invoices"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
