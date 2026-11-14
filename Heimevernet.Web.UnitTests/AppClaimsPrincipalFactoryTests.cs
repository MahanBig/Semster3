using System.Security.Claims;
using Heimevernet.Web.Authorization;
using Heimevernet.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heimevernet.Web.UnitTests;

// Tester at navnet kommer med i innloggingen, så menyen kan vise navnet i stedet for e-posten.
public class AppClaimsPrincipalFactoryTests
{
    [Fact]
    public async Task Login_ContainsTheNameOfTheUser()
    {
        using var database = new SqliteTestDatabase();
        using var services = IdentityTestServices.Create(database);
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "ola@test.no", Email = "ola@test.no", Name = "Ola Nordmann" };
        await userManager.CreateAsync(user, "Test1234!x");
        var factory = new AppClaimsPrincipalFactory(
            userManager, services.GetRequiredService<RoleManager<ApplicationRole>>(), services.GetRequiredService<IOptions<IdentityOptions>>());

        var principal = await factory.CreateAsync(user);

        Assert.Equal("Ola Nordmann", AppClaimsPrincipalFactory.DisplayName(principal));
        Assert.Equal("ola@test.no", principal.Identity?.Name); // Brukernavnet er fortsatt e-posten.
    }

    [Fact]
    public void DisplayName_WithoutName_FallsBackToEmail()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "kari@test.no")], "Test"));

        Assert.Equal("kari@test.no", AppClaimsPrincipalFactory.DisplayName(principal));
    }
}
