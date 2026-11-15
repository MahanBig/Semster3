using System.Security.Claims;
using Heimevernet.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Heimevernet.Web.Authorization;

// Bestemmer hva som lagres i innloggings-cookien (claims) når noen logger inn.
// Identity tar bare med Id, brukernavn (e-posten) og roller. Vi legger til navnet, så menyen kan vise
// "Ola Nordmann" i stedet for e-posten, uten å spørre databasen på hver side.
// Når navnet endres i innstillingene, lager RefreshSignInAsync cookien på nytt, og det nye navnet kommer med.
public class AppClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, options)
{
    public const string DisplayNameClaim = "display_name";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (!string.IsNullOrWhiteSpace(user.Name))
        {
            identity.AddClaim(new Claim(DisplayNameClaim, user.Name));
        }
        return identity;
    }

    // Navnet til den innloggede brukeren. Har brukeren ikke navn, brukes e-posten.
    public static string DisplayName(ClaimsPrincipal user) =>
        user.FindFirst(DisplayNameClaim)?.Value ?? user.Identity?.Name ?? string.Empty;
}
