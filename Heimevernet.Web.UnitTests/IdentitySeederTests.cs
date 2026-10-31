using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Heimevernet.Web.UnitTests;

public class IdentitySeederTests
{
    private const string AdminEmail = "admin@test.local";
    private const string AdminPassword = "Test1234!";

    [Fact]
    public async Task SeedAsync_WithAdminSettings_CreatesRolesAndAdminUser()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await IdentitySeeder.SeedAsync(roleManager, userManager, AdminEmail, AdminPassword, NullLogger.Instance);

        Assert.True(await roleManager.RoleExistsAsync(AppRoles.Admin));
        Assert.True(await roleManager.RoleExistsAsync(AppRoles.User));
        var admin = await userManager.FindByEmailAsync(AdminEmail);
        Assert.NotNull(admin);
        Assert.True(await userManager.IsInRoleAsync(admin, AppRoles.Admin));
        Assert.True(await userManager.CheckPasswordAsync(admin, AdminPassword));
    }

    [Fact]
    public async Task SeedAsync_RepeatedRuns_DoNotCreateDuplicates()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await IdentitySeeder.SeedAsync(roleManager, userManager, AdminEmail, AdminPassword, NullLogger.Instance);
        await IdentitySeeder.SeedAsync(roleManager, userManager, AdminEmail, AdminPassword, NullLogger.Instance);

        Assert.Equal(AppRoles.All.Count, roleManager.Roles.Count());
        Assert.Single(userManager.Users);
    }

    [Fact]
    public async Task SeedAsync_WeakAdminPassword_LogsErrorButDoesNotThrow()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // "weakpass" has no capital letter, digit or symbol, so Identity refuses it.
        await IdentitySeeder.SeedAsync(roleManager, userManager, AdminEmail, "weakpass", NullLogger.Instance);

        Assert.Equal(AppRoles.All.Count, roleManager.Roles.Count());
        Assert.Null(await userManager.FindByEmailAsync(AdminEmail));
    }

    [Fact]
    public async Task SeedAsync_WithoutAdminSettings_CreatesOnlyRoles()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await IdentitySeeder.SeedAsync(roleManager, userManager, null, null, NullLogger.Instance);

        Assert.Equal(AppRoles.All.Count, roleManager.Roles.Count());
        Assert.Empty(userManager.Users);
    }
}
