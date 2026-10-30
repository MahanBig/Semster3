using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
        using var connection = OpenConnection();
        using var services = CreateServices(connection);
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
        using var connection = OpenConnection();
        using var services = CreateServices(connection);
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await IdentitySeeder.SeedAsync(roleManager, userManager, AdminEmail, AdminPassword, NullLogger.Instance);
        await IdentitySeeder.SeedAsync(roleManager, userManager, AdminEmail, AdminPassword, NullLogger.Instance);

        Assert.Equal(AppRoles.All.Count, roleManager.Roles.Count());
        Assert.Single(userManager.Users);
    }

    [Fact]
    public async Task SeedAsync_WithoutAdminSettings_CreatesOnlyRoles()
    {
        using var connection = OpenConnection();
        using var services = CreateServices(connection);
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await IdentitySeeder.SeedAsync(roleManager, userManager, null, null, NullLogger.Instance);

        Assert.Equal(AppRoles.All.Count, roleManager.Roles.Count());
        Assert.Empty(userManager.Users);
    }

    private static SqliteConnection OpenConnection()
    {
        // Keep SQLite open so the in-memory database lives for the whole test.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    // Builds the same Identity services as Program.cs, but against SQLite instead of MariaDB.
    private static ServiceProvider CreateServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<HeimevernetDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<HeimevernetDbContext>();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<HeimevernetDbContext>().Database.EnsureCreated();
        return provider;
    }
}
