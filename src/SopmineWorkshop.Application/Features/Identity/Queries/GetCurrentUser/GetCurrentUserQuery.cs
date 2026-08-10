using MediatR;

using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(string UserId) : IRequest<Result<UserAccountDto>>;
