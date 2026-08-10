namespace SopmineWorkshop.Application.Features.Identity.Dtos;

public sealed record UserAccountDto(
    string UserId,
    string Email,
    string Role);
