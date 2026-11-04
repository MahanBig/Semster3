using Heimevernet.Web.DataAccess.Identity;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Web.DataAccess;

// Sørger for at rollene finnes, og lager en første admin-bruker.
// Uten en admin ville ingen kunne åpne brukeradministrasjonen og gi andre roller.
public static class IdentitySeeder
{
    public static async Task SeedAsync(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IRoleVerificationRepository roleVerification,
        string? adminEmail,
        string? adminPassword,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(roleManager);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(roleVerification);

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                EnsureSucceeded(await roleManager.CreateAsync(AppRoles.Create(roleName)), $"create role '{roleName}'");
            }
        }

        // Passordet skal aldri ligge i koden eller i Git. Det kommer fra konfigurasjon
        // (ADMIN_PASSWORD i .env-filen som docker-compose.yml leser). Mangler det, hopper vi bare over admin-brukeren.
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("AdminUser:Email or AdminUser:Password is not configured, so no admin user was created.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Name = "Administrator",
                Consent = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                // Et for svakt passord skal ikke stoppe hele appen. Vi skriver en tydelig feil i loggen i stedet,
                // og appen starter uten admin-bruker til passordet er rettet.
                logger.LogError(
                    "Could not create the admin user {Email}: {Errors} Fix ADMIN_PASSWORD in .env and restart.",
                    adminEmail,
                    string.Join(" ", result.Errors.Select(error => error.Description)));
                return;
            }
        }

        // Admin krever godkjenning, så oppstarten godkjenner den første admin-brukeren.
        // IsInRoleAsync kan ikke brukes til sjekken, fordi den overser roller som ikke er godkjent.
        var adminRole = (await roleVerification.GetRolesAsync(admin.Id)).FirstOrDefault(role => role.RoleName == AppRoles.Admin);
        if (adminRole is null)
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, AppRoles.Admin), "give the admin user the Admin role");
        }
        if (adminRole is null || !adminRole.IsActive)
        {
            await roleVerification.VerifyAsync(admin.Id, AppRoles.Admin, verifiedBy: null);
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
