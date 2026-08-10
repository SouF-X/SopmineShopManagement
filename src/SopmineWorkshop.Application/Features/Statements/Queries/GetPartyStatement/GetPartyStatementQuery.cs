using MediatR;
using SopmineWorkshop.Application.Features.Statements.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
namespace SopmineWorkshop.Application.Features.Statements.Queries.GetPartyStatement;
public sealed record GetPartyStatementQuery(StatementPartyKind PartyKind, Guid PartyId, DateTime? From, DateTime? To, InvoicePaymentProgress? PaymentProgress) : IRequest<Result<PartyStatementDto>>;
