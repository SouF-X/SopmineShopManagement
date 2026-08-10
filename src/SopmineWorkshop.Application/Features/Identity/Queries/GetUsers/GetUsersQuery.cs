using MediatR;

using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Queries.GetUsers;

public sealed record GetUsersQuery : IRequest<Result<List<UserAccountDto>>>;
