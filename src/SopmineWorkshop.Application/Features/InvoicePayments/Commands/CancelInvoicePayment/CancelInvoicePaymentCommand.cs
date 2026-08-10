using MediatR;
using SopmineWorkshop.Application.Features.InvoicePayments.Dtos;
using SopmineWorkshop.Domain.Common.Results;
namespace SopmineWorkshop.Application.Features.InvoicePayments.Commands.CancelInvoicePayment;
public sealed record CancelInvoicePaymentCommand(Guid InvoiceId, Guid PaymentId, string? Reason) : IRequest<Result<InvoicePaymentMutationDto>>;
