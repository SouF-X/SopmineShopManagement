using MediatR;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetCurrentUserQuery, Result<UserAccountDto>>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<Result<UserAccountDto>> Handle(GetCurrentUserQuery query, CancellationToken ct)
        => _identityService.GetUserAccountByIdAsync(query.UserId);
}
