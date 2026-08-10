using MediatR;

using SopmineWorkshop.Application.Features.DocumentNominations.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.DocumentNominations.Queries.GetDocumentNominations;

public sealed record GetDocumentNominationsQuery(bool CanAccessPurchases)
    : IRequest<Result<List<DocumentNominationDto>>>;
