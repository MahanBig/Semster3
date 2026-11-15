using System.Reflection;
using Heimevernet.Web.Controllers;
using Heimevernet.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Heimevernet.Web.UnitTests;

// Sjekker at innlogging og registrering faktisk har rate limiting, så det ikke forsvinner ved et uhell.
// (Selve tellingen gjøres av ASP.NET og testes av systemtesten mot Docker.)
public class RateLimitingTests
{
    [Theory]
    [InlineData(nameof(AccountController.Login))]
    [InlineData(nameof(AccountController.Register))]
    public void LoginAndRegisterPost_AreRateLimited(string actionName)
    {
        var post = typeof(AccountController).GetMethods()
            .Single(method => method.Name == actionName && method.GetCustomAttribute<HttpPostAttribute>() is not null);

        var attribute = post.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(RateLimits.Auth, attribute.PolicyName);
    }

    [Fact]
    public void ShowingTheLoginPage_IsNotRateLimited()
    {
        // Bare innsending (POST) teller. Å åpne siden (GET) skal aldri gi «for mange forsøk».
        var get = typeof(AccountController).GetMethods()
            .Single(method => method.Name == nameof(AccountController.Login) && method.GetCustomAttribute<HttpGetAttribute>() is not null);

        Assert.Null(get.GetCustomAttribute<EnableRateLimitingAttribute>());
    }
}
