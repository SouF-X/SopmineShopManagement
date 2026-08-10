using MediatR;
using SopmineWorkshop.Application.Features.InvoicePayments.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.InvoicePayments.Commands.RecordInvoicePayment;
public sealed record RecordInvoicePaymentCommand(Guid InvoiceId, decimal Amount, DateTime PaymentDate, InvoicePaymentMethod Method, string? Reference, string? Note) : IRequest<Result<InvoicePaymentMutationDto>>;
