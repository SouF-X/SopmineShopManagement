using SopmineWorkshop.Application.Features.Identity;
using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Common.Interfaces;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default);
}
