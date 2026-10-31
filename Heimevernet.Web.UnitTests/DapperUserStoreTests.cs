using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.Web.UnitTests;

// Tests the SQL in DapperUserStore through UserManager, the same way the app uses it.
public class DapperUserStoreTests
{
    private const string Password = "Test1234!";

    [Fact]
    public async Task CreateAndUpdate_PersistChanges_AndEmailLookupIgnoresCase()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var created = await userManager.CreateAsync(new IdentityUser { UserName = "ola@test.no", Email = "ola@test.no" }, Password);
        Assert.True(created.Succeeded);

        var user = await userManager.FindByEmailAsync("OLA@Test.no");
        Assert.NotNull(user);
        user.PhoneNumber = "12345678";
        Assert.True((await userManager.UpdateAsync(user)).Succeeded);

        var reloaded = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("12345678", reloaded.PhoneNumber);
        Assert.True(await userManager.CheckPasswordAsync(reloaded, Password));
    }

    [Fact]
    public async Task UpdateAsync_WithStaleCopy_ReturnsConcurrencyFailure()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = "kari@test.no", Email = "kari@test.no" };
        await userManager.CreateAsync(user, Password);

        // Two separate copies of the same row, like two browser tabs editing the same user.
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole("Admin"));
        await roleManager.CreateAsync(new IdentityRole("Bruker"));
        var user = new IdentityUser { UserName = "per@test.no", Email = "per@test.no" };
        await userManager.CreateAsync(user, Password);

        await userManager.AddToRoleAsync(user, "Admin");
        await userManager.AddToRoleAsync(user, "Bruker");

        Assert.True(await userManager.IsInRoleAsync(user, "admin")); // Role names ignore case.
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = "lise@test.no", Email = "lise@test.no" };
        await userManager.CreateAsync(user, Password);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await userManager.AccessFailedAsync(user);
        }

        // Read the user again, so the lockout time must have been saved to and read from the database.
        var reloaded = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(reloaded);
        Assert.True(await userManager.IsLockedOutAsync(reloaded));
    }
}
