using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> AuthorizeAsync(string userId, string? policyName);
    Task<Result<AppUserDto>> AuthenticateAsync(string email, string password);
    Task<Result<UserAccountDto>> CreateUserAsync(string email, string password, string role);
    Task<Result<Deleted>> DeleteUserAsync(string userId, string currentUserId);
    Task<Result<AppUserDto>> GetUserByIdAsync(string userId);
    Task<Result<UserAccountDto>> GetUserAccountByIdAsync(string userId);
    Task<Result<List<UserAccountDto>>> GetUsersAsync();
    Task<string?> GetUserNameAsync(string userId);
    Task<bool> IsInRoleAsync(string userId, string role);
    Task<Result<Updated>> ResetPasswordAsync(string userId, string newPassword);
    Task<Result<Updated>> UpdatePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<Result<UserAccountDto>> UpdateUserAsync(string userId, string email, string role);
}
