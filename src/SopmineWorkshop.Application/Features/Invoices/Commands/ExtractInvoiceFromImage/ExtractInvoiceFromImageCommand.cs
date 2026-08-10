using MediatR;

using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.ExtractInvoiceFromImage;

public sealed record ExtractInvoiceFromImageCommand(
    byte[] ImageBytes,
    string ContentType,
    string? FileName,
    InvoiceType Type = InvoiceType.Facture
) : IRequest<Result<InvoiceExtractionDto>>;
