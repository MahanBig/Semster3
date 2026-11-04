using System.Security.Claims;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.Authorization;

// Regelen for hvem som kan endre og slette en ressurs eller et behov, samlet ett sted:
//   - Admin kan endre og slette alt.
//   - En vanlig bruker kan endre og slette det hen selv har lagt inn (UserId).
//   - Ikke innloggede kan ingenting.
// Brukes både av controllerne (som stopper forespørselen) og av viewene (som skjuler knappene).
public static class ResourcePermissions
{
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
