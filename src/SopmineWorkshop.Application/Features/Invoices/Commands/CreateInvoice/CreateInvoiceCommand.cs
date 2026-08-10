using MediatR;

using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.CreateInvoice;

public sealed record CreateInvoiceCommand(
    string? Reference,
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
    CreateInvoiceSupplierCommand? NewSupplier,
    List<CreateInvoiceLineCommand> Lines,
    bool CatalogueMode = true
) : IRequest<Result<InvoiceDto>>;
