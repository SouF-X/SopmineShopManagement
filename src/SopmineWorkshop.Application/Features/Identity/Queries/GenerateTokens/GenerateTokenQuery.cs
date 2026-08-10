using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Identity.Queries.GenerateTokens;

public sealed record GenerateTokenQuery(
    string Email,
    string Password) : IRequest<Result<TokenResponse>>;
