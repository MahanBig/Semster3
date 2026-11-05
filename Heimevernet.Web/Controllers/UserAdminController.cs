using Heimevernet.Web.DataAccess;
using Heimevernet.Web.DataAccess.Identity;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Need;
using Heimevernet.Web.Models.ViewModels.Resource;
using Heimevernet.Web.Models.ViewModels.UserAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.Controllers;

// Admin-siden: roller, brukere, ressurser og behov på ett sted.
// Hele controlleren krever at brukeren er logget inn OG har rollen Admin.
// Er man ikke logget inn, sendes man til /Account/Login. Mangler man rollen, til /Account/AccessDenied.
// Endre og slette ressurser og behov gjøres med de vanlige sidene (ResourceHandler og Need), som slipper admin inn.
[Authorize(Roles = AppRoles.Admin)]
public class UserAdminController(
    UserManager<ApplicationUser> userManager,
    IRoleVerificationRepository roleVerification,
    IResourceRepository resourceRepository,
    INeedRepository needRepository) : Controller
{
    // GET /UserAdmin
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var resources = resourceRepository.GetAll();
        var resourceCounts = resources
            .Where(resource => resource.UserId is not null)
            .GroupBy(resource => resource.UserId!)
            .ToDictionary(group => group.Key, group => group.Count());

        // Users kommer fra DapperUserStore.Users (henter alle brukerne fra AspNetUsers).
        var users = new List<AdminUserRow>();
        // Én spørring per bruker for å hente rollene. Greit med få brukere, men med mange brukere
        // burde vi hentet alle rollene i én spørring.
        foreach (var user in userManager.Users.OrderBy(user => user.Email).ToList())
        {
            users.Add(new AdminUserRow
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                Role = ActiveRole(await roleVerification.GetRolesAsync(user.Id)),
                ResourceCount = resourceCounts.GetValueOrDefault(user.Id)
            });
        }

        return View(new AdminPageViewModel
        {
            Users = users,
            Resources = resources.Select(ResourceViewModel.FromEntity).OrderBy(resource => resource.Name).ToList(),
            Needs = needRepository.GetAll().Select(NeedViewModel.FromEntity).ToList(),
            CurrentUserId = userManager.GetUserId(User)
        });
    }

    // POST /UserAdmin/SetRole: gir brukeren én rolle og fjerner de andre.
    // En rolle som admin gir, er godkjent med en gang, og VerifiedBy blir admin-en som ga den.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !AppRoles.All.Contains(role))
        {
            return NotFound();
        }

        // Hindrer at en admin fjerner sin egen admin-rolle og låser seg selv ute.
        if (user.Id == userManager.GetUserId(User) && role != AppRoles.Admin)
        {
            TempData["Error"] = "Du kan ikke fjerne din egen admin-rolle. Be en annen admin gjøre det.";
            return RedirectToAction(nameof(Index));
        }

        var current = await roleVerification.GetRolesAsync(user.Id);
        foreach (var other in current.Where(status => status.RoleName != role && AppRoles.All.Contains(status.RoleName)))
        {
            await userManager.RemoveFromRoleAsync(user, other.RoleName);
        }
        if (current.All(status => status.RoleName != role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
        await roleVerification.VerifyAsync(user.Id, role, verifiedBy: userManager.GetUserId(User));

        // Ny sikkerhetsstempel: brukerens innlogging leses på nytt, så den nye rollen gjelder uten å vente på utlogging.
        await userManager.UpdateSecurityStampAsync(user);

        TempData["Message"] = $"{user.Email} har nå rollen {role}.";
        return RedirectToAction(nameof(Index));
    }

    // POST /UserAdmin/DeleteUser: sletter en bruker. Ressursene og behovene hen la inn blir stående uten eier
    // (ON DELETE SET NULL i Schema.sql), så bare admin kan endre dem etterpå.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }
        if (user.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "Du kan ikke slette deg selv her.";
            return RedirectToAction(nameof(Index));
        }

        var result = await userManager.DeleteAsync(user);
        TempData[result.Succeeded ? "Message" : "Error"] = result.Succeeded
            ? $"Brukeren {user.Email} ble slettet."
            : "Brukeren kunne ikke slettes: " + string.Join(" ", result.Errors.Select(error => error.Description));
        return RedirectToAction(nameof(Index));
    }

    // Den rollen som gir mest tilgang av de aktive (godkjente) rollene.
    internal static string? ActiveRole(IEnumerable<UserRoleStatus> roles)
    {
        var active = roles.Where(status => status.IsActive).Select(status => status.RoleName).ToHashSet();
        return AppRoles.All.FirstOrDefault(active.Contains);
    }
}
