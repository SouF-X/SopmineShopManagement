using MediatR;

using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.ConvertBonLivraisons;

public sealed record ConvertBonLivraisonsCommand(
    List<Guid> InvoiceIds
) : IRequest<Result<InvoiceDto>>;
