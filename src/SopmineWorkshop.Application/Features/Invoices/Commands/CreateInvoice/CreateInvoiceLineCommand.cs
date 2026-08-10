using MediatR;

using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.CreateInvoice;

public sealed record CreateInvoiceLineCommand(
    Guid? ProduitId,
    string? ProductReference,
    string? ProductName,
    string? ProductFamily,
    string? ProductUnit,
    decimal Quantity,
    decimal Price,
    decimal? PriceTTC,
    decimal TVA
) : IRequest<Result<InvoiceLineDto>>;
