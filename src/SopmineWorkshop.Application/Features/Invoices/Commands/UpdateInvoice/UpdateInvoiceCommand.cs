using MediatR;

using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.UpdateInvoice;

public sealed record UpdateInvoiceCommand(
    Guid InvoiceId,
    string Reference,
    InvoiceType Type,
    InvoiceNature Nature,
    DateTime Date,
    DateTime? DueDate,
    Guid? FournisseurId,
    Guid? ClientId,
    InvoiceStatus? Status,
    InvoicePaymentStatus? PaymentStatus,
    InvoicePaymentMethod? PaymentMethod,
    string? Notes,
    decimal Total,
    List<UpdateInvoiceLineCommand> Lines
) : IRequest<Result<InvoiceDto>>;
