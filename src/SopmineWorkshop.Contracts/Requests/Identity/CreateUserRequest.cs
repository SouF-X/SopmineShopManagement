namespace SopmineWorkshop.Contracts.Requests.Identity;

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string Role);
