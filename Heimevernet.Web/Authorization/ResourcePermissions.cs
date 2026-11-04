using System.Security.Claims;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.Authorization;

// Regelen for hvem som kan endre og slette en ressurs, samlet ett sted:
//   - Admin kan endre og slette alle ressurser.
//   - En vanlig bruker kan endre og slette ressurser hen selv har lagt inn (CreatedBy).
//   - Ikke innloggede kan ingenting.
// Brukes både av controlleren (som stopper forespørselen) og av viewet (som skjuler knappene).
public static class ResourcePermissions
{
    public static bool CanModify(ClaimsPrincipal? user, Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (user.IsInRole(AppRoles.Admin))
        {
            return true;
        }

        return resource.CreatedBy is not null
            && string.Equals(resource.CreatedBy, user.Identity.Name, StringComparison.OrdinalIgnoreCase);
    }
}
