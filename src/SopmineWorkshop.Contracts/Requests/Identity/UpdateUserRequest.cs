namespace SopmineWorkshop.Contracts.Requests.Identity;

public sealed record UpdateUserRequest(
    string Email,
    string Role);
