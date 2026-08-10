using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Identity;
using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Infrastructure.Identity;

public sealed class TokenProvider(IConfiguration configuration) : ITokenProvider
{
    private readonly IConfiguration _configuration = configuration;

    public Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default)
        => CreateAsync(user);

    private Task<Result<TokenResponse>> CreateAsync(AppUserDto user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var secret = jwtSettings["Secret"];
        var expirationValue = jwtSettings["TokenExpirationInMinutes"];

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(secret) || !int.TryParse(expirationValue, out var expirationInMinutes))
        {
            return Task.FromResult<Result<TokenResponse>>(
                Error.Unexpected("JwtSettings.Invalid", "JWT settings are missing or invalid."));
        }

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.UserId),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        claims.AddRange(user.Claims);

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiresOnUtc = DateTime.UtcNow.AddMinutes(expirationInMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresOnUtc,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(descriptor);

        TokenResponse response = new()
        {
            AccessToken = tokenHandler.WriteToken(securityToken),
            ExpiresOnUtc = expiresOnUtc
        };

        return Task.FromResult<Result<TokenResponse>>(response);
    }
}
