using Heimevernet.Web.DataAccess.Identity;
using Heimevernet.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.Web.UnitTests;

// Tester SQL-en i DapperUserStore gjennom UserManager, slik appen bruker den.
public class DapperUserStoreTests
{
    private const string Password = "Test1234!";

    [Fact]
    public async Task CreateAndUpdate_PersistChanges_AndEmailLookupIgnoresCase()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var created = await userManager.CreateAsync(
            new ApplicationUser { UserName = "ola@test.no", Email = "ola@test.no", Name = "Ola Nordmann", Consent = true }, Password);
        Assert.True(created.Succeeded);

        var user = await userManager.FindByEmailAsync("OLA@Test.no");
        Assert.NotNull(user);
        user.PhoneNumber = "12345678";
        Assert.True((await userManager.UpdateAsync(user)).Succeeded);

        var reloaded = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("12345678", reloaded.PhoneNumber);
        Assert.Equal("Ola Nordmann", reloaded.Name);
        Assert.True(reloaded.Consent);
        Assert.True(await userManager.CheckPasswordAsync(reloaded, Password));
    }

    [Fact]
    public async Task UpdateAsync_WithStaleCopy_ReturnsConcurrencyFailure()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "kari@test.no", Email = "kari@test.no" };
        await userManager.CreateAsync(user, Password);

        // To separate kopier av samme rad, som to nettleserfaner som endrer den samme brukeren.
        var firstCopy = await userManager.FindByIdAsync(user.Id);
        var secondCopy = await userManager.FindByIdAsync(user.Id);
        firstCopy!.PhoneNumber = "11111111";
        secondCopy!.PhoneNumber = "22222222";

        var firstResult = await userManager.UpdateAsync(firstCopy);
        var secondResult = await userManager.UpdateAsync(secondCopy);

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Contains(secondResult.Errors, error => error.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure));
    }

    [Fact]
    public async Task RoleMethods_AddCheckListAndRemoveRoles()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        await roleManager.CreateAsync(new ApplicationRole("Admin"));
        await roleManager.CreateAsync(new ApplicationRole("Bruker"));
        var user = new ApplicationUser { UserName = "per@test.no", Email = "per@test.no" };
        await userManager.CreateAsync(user, Password);

        await userManager.AddToRoleAsync(user, "Admin");
        await userManager.AddToRoleAsync(user, "Bruker");

        Assert.True(await userManager.IsInRoleAsync(user, "admin")); // Rollenavn skiller ikke mellom store og små bokstaver.
        Assert.Equal("Admin, Bruker", string.Join(", ", await userManager.GetRolesAsync(user)));
        Assert.Single(await userManager.GetUsersInRoleAsync("Admin"));

        await userManager.RemoveFromRoleAsync(user, "Admin");

        Assert.False(await userManager.IsInRoleAsync(user, "Admin"));
        Assert.Equal("Bruker", Assert.Single(await userManager.GetRolesAsync(user)));
    }

    [Fact]
    public async Task AccessFailedAsync_FiveFailedAttempts_LocksOutUser()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "lise@test.no", Email = "lise@test.no" };
        await userManager.CreateAsync(user, Password);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await userManager.AccessFailedAsync(user);
        }

        // Leser brukeren på nytt, så låsetiden må ha blitt lagret i og lest fra databasen.
        var reloaded = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(reloaded);
        Assert.True(await userManager.IsLockedOutAsync(reloaded));
    }

    [Fact]
    public async Task RoleThatRequiresVerification_DoesNotCountUntilVerified()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var roleVerification = new DapperRoleVerificationRepository(database);
        await roleManager.CreateAsync(new ApplicationRole("Admin") { RequiresVerification = true, AccessLevel = 100 });
        var user = new ApplicationUser { UserName = "per@test.no", Email = "per@test.no" };
        await userManager.CreateAsync(user, Password);

        await userManager.AddToRoleAsync(user, "Admin");

        // Raden finnes, men rollen gir ingen tilgang ennå.
        Assert.False(await userManager.IsInRoleAsync(user, "Admin"));
        Assert.Empty(await userManager.GetRolesAsync(user));
        var pending = Assert.Single(await roleVerification.GetRolesAsync(user.Id));
        Assert.False(pending.IsActive);

        Assert.True(await roleVerification.VerifyAsync(user.Id, "Admin", verifiedBy: null));

        Assert.True(await userManager.IsInRoleAsync(user, "Admin"));
        Assert.True(Assert.Single(await roleVerification.GetRolesAsync(user.Id)).IsActive);
    }
}
