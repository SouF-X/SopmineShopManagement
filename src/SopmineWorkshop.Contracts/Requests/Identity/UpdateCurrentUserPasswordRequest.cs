namespace SopmineWorkshop.Contracts.Requests.Identity;

public sealed record UpdateCurrentUserPasswordRequest(
    string CurrentPassword,
    string NewPassword);
