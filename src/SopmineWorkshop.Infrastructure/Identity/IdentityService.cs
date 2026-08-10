using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Identity.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Identity;

namespace SopmineWorkshop.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
    IAuthorizationService authorizationService) : IIdentityService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    public async Task<bool> AuthorizeAsync(string userId, string? policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);
        var result = await _authorizationService.AuthorizeAsync(principal, policyName!);

        return result.Succeeded;
    }

    public async Task<Result<AppUserDto>> AuthenticateAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        return new AppUserDto(
            user.Id,
            user.Email!,
            roles,
            claims);
    }

    public async Task<Result<UserAccountDto>> CreateUserAsync(string email, string password, string role)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedRole = NormalizeRole(role);

        if (normalizedRole is null)
        {
            return Error.Validation("Users.InvalidRole", "Role must be Admin or Employee.");
        }

        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return Error.Conflict("Users.EmailAlreadyExists", "A user with this email already exists.");
        }

        var user = new AppUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            return ToErrors(createResult);
        }

        var roleResult = await AddToRoleAsync(user, normalizedRole);

        if (roleResult.IsError)
        {
            await _userManager.DeleteAsync(user);
            return roleResult.Errors;
        }

        return await ToUserAccountDtoAsync(user);
    }

    public async Task<Result<Deleted>> DeleteUserAsync(string userId, string currentUserId)
    {
        if (string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Conflict("Users.DeleteSelf", "You cannot delete your own account.");
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Error.NotFound("Users.NotFound", "User not found.");
        }

        if (await IsLastAdminAsync(user))
        {
            return Error.Conflict("Users.LastAdmin", "At least one admin account must remain.");
        }

        var deleteResult = await _userManager.DeleteAsync(user);

        if (!deleteResult.Succeeded)
        {
            return ToErrors(deleteResult);
        }

        return Result.Deleted;
    }

    public async Task<Result<AppUserDto>> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Error.NotFound("Auth.UserNotFound", "User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        return new AppUserDto(
            user.Id,
            user.Email!,
            roles,
            claims);
    }

    public async Task<Result<UserAccountDto>> GetUserAccountByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Error.NotFound("Auth.UserNotFound", "User not found.");
        }

        return await ToUserAccountDtoAsync(user);
    }

    public async Task<Result<List<UserAccountDto>>> GetUsersAsync()
    {
        var users = await _userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync();

        var response = new List<UserAccountDto>(users.Count);

        foreach (var user in users)
        {
            response.Add(await ToUserAccountDtoAsync(user));
        }

        return response;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user is not null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<Result<Updated>> ResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Error.NotFound("Users.NotFound", "User not found.");
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

        if (!result.Succeeded)
        {
            return ToErrors(result);
        }

        return Result.Updated;
    }

    public async Task<Result<Updated>> UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Error.NotFound("Users.NotFound", "User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            return ToErrors(result);
        }

        return Result.Updated;
    }

    public async Task<Result<UserAccountDto>> UpdateUserAsync(string userId, string email, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Error.NotFound("Users.NotFound", "User not found.");
        }

        var normalizedEmail = NormalizeEmail(email);
        var normalizedRole = NormalizeRole(role);

        if (normalizedRole is null)
        {
            return Error.Validation("Users.InvalidRole", "Role must be Admin or Employee.");
        }

        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

        if (existingUser is not null &&
            !string.Equals(existingUser.Id, user.Id, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Conflict("Users.EmailAlreadyExists", "A user with this email already exists.");
        }

        if (normalizedRole != nameof(Role.Admin) && await IsLastAdminAsync(user))
        {
            return Error.Conflict("Users.LastAdmin", "At least one admin account must remain.");
        }

        user.UserName = normalizedEmail;
        user.Email = normalizedEmail;
        user.EmailConfirmed = true;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return ToErrors(updateResult);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, roles);

        if (!removeResult.Succeeded)
        {
            return ToErrors(removeResult);
        }

        var roleResult = await AddToRoleAsync(user, normalizedRole);

        if (roleResult.IsError)
        {
            return roleResult.Errors;
        }

        return await ToUserAccountDtoAsync(user);
    }

    private async Task<Result<Updated>> AddToRoleAsync(AppUser user, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(role));

            if (!createRoleResult.Succeeded)
            {
                return ToErrors(createRoleResult);
            }
        }

        var result = await _userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            return ToErrors(result);
        }

        return Result.Updated;
    }

    private async Task<bool> IsLastAdminAsync(AppUser user)
    {
        if (!await _userManager.IsInRoleAsync(user, nameof(Role.Admin)))
        {
            return false;
        }

        var admins = await _userManager.GetUsersInRoleAsync(nameof(Role.Admin));

        return admins.Count <= 1;
    }

    private async Task<UserAccountDto> ToUserAccountDtoAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? nameof(Role.Employee);

        return new UserAccountDto(user.Id, user.Email ?? user.UserName ?? string.Empty, role);
    }

    private static string NormalizeEmail(string email)
        => email.Trim();

    private static string? NormalizeRole(string role)
        => Enum.TryParse<Role>(role.Trim(), true, out var parsedRole)
            ? parsedRole.ToString()
            : null;

    private static List<Error> ToErrors(IdentityResult result)
        => result.Errors
            .Select(error => Error.Validation(error.Code, error.Description))
            .ToList();
}
