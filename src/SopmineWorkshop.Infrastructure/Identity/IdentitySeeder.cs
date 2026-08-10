using System;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Domain.Identity;

namespace SopmineWorkshop.Infrastructure.Identity;

public sealed class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
    IConfiguration configuration,
    ILogger<IdentitySeeder> logger)
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<IdentitySeeder> _logger = logger;

    public async Task SeedAsync()
    {
        await EnsureRolesAsync();
        await EnsureDefaultAdminAsync();
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var role in Enum.GetNames<Role>())
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private async Task EnsureDefaultAdminAsync()
    {
        var section = _configuration.GetSection("DefaultAdmin");
        var email = section["Email"];
        var password = section["Password"];
        var role = section["Role"] ?? nameof(Role.Admin);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                _logger.LogWarning(
                    "Default admin creation failed for {Email}: {Errors}",
                    email,
                    FormatErrors(createResult));

                return;
            }
        }
        else
        {
            var requiresUpdate = false;

            if (!string.Equals(user.UserName, email, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = email;
                requiresUpdate = true;
            }

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                requiresUpdate = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                requiresUpdate = true;
            }

            if (requiresUpdate)
            {
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning(
                        "Default admin update failed for {Email}: {Errors}",
                        email,
                        FormatErrors(updateResult));

                    return;
                }
            }

        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, role);

            if (!addRoleResult.Succeeded)
            {
                _logger.LogWarning(
                    "Default admin role assignment failed for {Email}: {Errors}",
                    email,
                    FormatErrors(addRoleResult));
            }
        }
    }

    private static string FormatErrors(IdentityResult result)
        => string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}
