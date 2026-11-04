using System.Reflection;
using Heimevernet.Web.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Heimevernet.Web.UnitTests;

// Hele appen krever innlogging (FallbackPolicy i Program.cs). Bare disse sidene skal være åpne,
// ellers kan ingen logge inn eller registrere seg. Testen sier fra hvis noen fjerner [AllowAnonymous] ved en feil,
// eller legger det på en side som skal være beskyttet.
public class AllowAnonymousTests
{
    private static readonly string[] OpenActions =
    [
        "AccountController.Login",
        "AccountController.Register",
        "AccountController.AccessDenied",
        "HomeController.Error",
    ];

    [Fact]
    public void OnlyLoginRegisterAccessDeniedAndErrorAreOpenWithoutLogin()
    {
        var open = typeof(HomeController).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Microsoft.AspNetCore.Mvc.Controller)))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null
                    || type.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
                .Select(method => $"{type.Name}.{method.Name}"))
            .Distinct()
            .Order()
            .ToArray();

        Assert.Equal(OpenActions.Order().ToArray(), open);
    }
}
