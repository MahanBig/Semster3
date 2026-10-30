using Heimevernet.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Web.DataAccess;

// Sørger for at rollene finnes, og lager en første admin-bruker.
// Uten en admin ville ingen kunne åpne brukeradministrasjonen og gi andre roller.
public static class IdentitySeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        string? adminEmail,
        string? adminPassword,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(roleManager);
        ArgumentNullException.ThrowIfNull(userManager);

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(roleName)), $"create role '{roleName}'");
            }
        }

        // Passordet skal aldri ligge i koden eller i Git. Det kommer fra konfigurasjon
        // (Aspire-parameter eller user secrets). Mangler det, hopper vi bare over admin-brukeren.
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("AdminUser:Email or AdminUser:Password is not configured, so no admin user was created.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            EnsureSucceeded(await userManager.CreateAsync(admin, adminPassword), "create the admin user");
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, AppRoles.Admin), "give the admin user the Admin role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not {action}: {errors}");
        }
    }
}
