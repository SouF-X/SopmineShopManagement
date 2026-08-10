using MediatR;
using SopmineWorkshop.Application.Features.InvoicePayments.Dtos;
using SopmineWorkshop.Domain.Common.Results;
namespace SopmineWorkshop.Application.Features.InvoicePayments.Queries.GetInvoicePayments;
public sealed record GetInvoicePaymentsQuery(Guid InvoiceId) : IRequest<Result<List<InvoicePaymentDto>>>;
