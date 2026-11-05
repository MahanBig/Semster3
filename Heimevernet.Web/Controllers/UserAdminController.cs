using Heimevernet.Web.DataAccess.Identity;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.UserAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.Controllers;

// Hele controlleren krever at brukeren er logget inn OG har rollen Admin.
// Er man ikke logget inn, sendes man til /Account/Login. Mangler man rollen, til /Account/AccessDenied.
[Authorize(Roles = AppRoles.Admin)]
public class UserAdminController(
    UserManager<ApplicationUser> userManager,
    IRoleVerificationRepository roleVerification) : Controller
{
    // GET /UserAdmin: alle brukerne med rollene sine.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Users kommer fra DapperUserStore.Users (henter alle brukerne fra AspNetUsers).
        var users = userManager.Users.OrderBy(user => user.Email).ToList();

        var model = new List<UserWithRolesViewModel>();
        // Én spørring per bruker for å hente rollene. Greit med få brukere, men med mange brukere
        // burde vi hentet alle rollene i én spørring.
        foreach (var user in users)
        {
            model.Add(new UserWithRolesViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                Roles = await roleVerification.GetRolesAsync(user.Id)
            });
        }

        return View(model);
    }

    // POST: gir en bruker en rolle. En rolle som admin gir, er godkjent med en gang,
    // og VerifiedBy blir admin-en som ga den.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRole(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !AppRoles.All.Contains(role))
        {
            return NotFound();
        }

        // Finnes koblingen allerede (venter på godkjenning), legges den ikke inn på nytt.
        var existing = (await roleVerification.GetRolesAsync(user.Id)).FirstOrDefault(status => status.RoleName == role);
        if (existing is null)
        {
            await userManager.AddToRoleAsync(user, role);
        }

        await roleVerification.VerifyAsync(user.Id, role, verifiedBy: userManager.GetUserId(User));
        return RedirectToAction(nameof(Index));
    }

    // POST: fjerner en rolle fra en bruker.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !AppRoles.All.Contains(role))
        {
            return NotFound();
        }

        // Hindrer at en admin fjerner sin egen admin-rolle og låser seg selv ute.
        if (role == AppRoles.Admin && user.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "Du kan ikke fjerne din egen admin-rolle.";
            return RedirectToAction(nameof(Index));
        }

        await userManager.RemoveFromRoleAsync(user, role);
        return RedirectToAction(nameof(Index));
    }
}
