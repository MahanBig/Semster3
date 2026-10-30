using Heimevernet.Web.Models;
using Heimevernet.Web.Models.ViewModels.UserAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Web.Controllers;

// Hele controlleren krever at brukeren er logget inn OG har rollen Admin.
// Er man ikke logget inn, sendes man til /Account/Login. Mangler man rollen, til /Account/AccessDenied.
[Authorize(Roles = AppRoles.Admin)]
public class UserAdminController(UserManager<IdentityUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await userManager.Users.OrderBy(user => user.Email).ToListAsync();

        var model = new List<UserWithRolesViewModel>();
        foreach (var user in users)
        {
            model.Add(new UserWithRolesViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                Roles = await userManager.GetRolesAsync(user)
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRole(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !AppRoles.All.Contains(role))
        {
            return NotFound();
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
        return RedirectToAction(nameof(Index));
    }

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
