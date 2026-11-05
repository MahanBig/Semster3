using System.Security.Claims;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.Authorization;

// Reglene for hvem som kan se, endre og slette ressurser og behov, samlet ett sted:
//   - Se ressurser: Admin og Offentlig ser alle. Privat ser bare sine egne.
//   - Endre og slette: Admin kan alt. Ellers bare den som la inn ressursen eller behovet (UserId).
//   - Ikke innloggede kan ingenting.
// Brukes både av controllerne (som stopper forespørselen) og av viewene (som skjuler knappene).
public static class ResourcePermissions
{
    // Admin og Offentlig ser alle ressursene. Privat ser bare sine egne.
    public static bool CanSeeAll(ClaimsPrincipal? user)
    {
        return user?.Identity?.IsAuthenticated == true
            && (user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Public));
    }

    public static bool CanView(ClaimsPrincipal? user, Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return CanSeeAll(user) || IsOwnerOrAdmin(user, resource.UserId);
    }

    // Ressursene brukeren har lov til å se. Brukes av kartet, listen og detaljsiden.
    // Filtreres i C# fordi datamengden er liten. Med mange ressurser burde det vært en WHERE i SQL-en.
    public static IEnumerable<Resource> Visible(ClaimsPrincipal? user, IEnumerable<Resource> resources)
    {
        return CanSeeAll(user) ? resources : resources.Where(resource => CanView(user, resource));
    }

    public static bool CanModify(ClaimsPrincipal? user, Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return IsOwnerOrAdmin(user, resource.UserId);
    }

    public static bool CanModify(ClaimsPrincipal? user, Need need)
    {
        ArgumentNullException.ThrowIfNull(need);
        return IsOwnerOrAdmin(user, need.UserId);
    }

    // Sammenligner bruker-Id (claimet NameIdentifier), ikke e-post, som kan endres.
    public static bool IsOwnerOrAdmin(ClaimsPrincipal? user, string? ownerUserId)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (user.IsInRole(AppRoles.Admin))
        {
            return true;
        }

        return ownerUserId is not null
            && ownerUserId == user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
