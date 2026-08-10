using MediatR;

using SopmineWorkshop.Application.Features.DocumentNominations.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.DocumentNominations.Commands.UpdateDocumentNomination;

public sealed record UpdateDocumentNominationCommand(
    string Key,
    string? Root,
    string? DateFormat,
    int IncrementSize,
    bool CanAccessPurchases) : IRequest<Result<DocumentNominationDto>>;
