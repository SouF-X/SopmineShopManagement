using MediatR;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetUsersQuery, Result<List<UserAccountDto>>>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<Result<List<UserAccountDto>>> Handle(GetUsersQuery query, CancellationToken ct)
        => _identityService.GetUsersAsync();
}
