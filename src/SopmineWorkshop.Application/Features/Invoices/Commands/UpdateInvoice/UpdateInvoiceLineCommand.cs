using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.UpdateInvoice;

public sealed record UpdateInvoiceLineCommand(
    Guid? InvoiceLineId,
    Guid? ProduitId,
    string? ProductReference,
    string? ProductName,
    string? ProductFamily,
    string? ProductUnit,
    decimal Quantity,
    decimal Price,
    decimal? PriceTTC,
    decimal TVA
) : IRequest<Result<Updated>>;
