using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.DeleteInvoice;

public sealed record DeleteInvoiceCommand(Guid InvoiceId) : IRequest<Result<Deleted>>;
