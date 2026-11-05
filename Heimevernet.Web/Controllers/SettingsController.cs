using System.Text.Json;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.DataAccess.Identity;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.Controllers;

// Innstillinger for den innloggede brukeren: se info om seg selv, endre navn, e-post og telefon,
// bytte passord, laste ned dataene sine og slette kontoen.
// Alt gjelder bare brukeren selv: Id-en hentes alltid fra innloggingen, aldri fra skjemaet.
[Authorize]
public class SettingsController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IRoleVerificationRepository roleVerification,
    IResourceRepository resourceRepository,
    INeedRepository needRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }
        return View(await BuildModel(user));
    }

    // POST /Settings/Profile: navn, e-post og telefon.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel profile)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var email = profile.Email.Trim();
        if (ModelState.IsValid && !string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var taken = await userManager.FindByEmailAsync(email);
            if (taken is not null && taken.Id != user.Id)
            {
                ModelState.AddModelError("Profile.Email", "Det finnes allerede en bruker med denne e-posten.");
            }
        }
        if (!ModelState.IsValid)
        {
            return await ShowWithErrors(user, profile: profile);
        }

        user.Name = profile.Name.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(profile.PhoneNumber) ? null : profile.PhoneNumber.Trim();
        if (!string.Equals(email, user.Email, StringComparison.Ordinal))
        {
            // E-posten er også brukernavnet, så begge endres. Identity lager de normaliserte (STORE BOKSTAVER) selv.
            user.Email = email;
            user.UserName = email;
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("Profile.Email", error.Description);
            }
            return await ShowWithErrors(user, profile: profile);
        }

        // Innloggings-cookien har navnet i seg. RefreshSignIn lager den på nytt, så menyen viser det nye navnet med en gang.
        await signInManager.RefreshSignInAsync(user);
        TempData["Message"] = "Profilen ble lagret.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Settings/Password
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Password(ChangePasswordViewModel password)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }
        if (!ModelState.IsValid)
        {
            return await ShowWithErrors(user, password: password);
        }

        // Sjekker det gamle passordet og passordreglene (Program.cs) før det nye lagres som hash.
        var result = await userManager.ChangePasswordAsync(user, password.CurrentPassword, password.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var key = error.Code == nameof(IdentityErrorDescriber.PasswordMismatch) ? "Password.CurrentPassword" : "Password.NewPassword";
                ModelState.AddModelError(key, error.Code == nameof(IdentityErrorDescriber.PasswordMismatch) ? "Feil passord." : error.Description);
            }
            return await ShowWithErrors(user, password: password);
        }

        // Passordbytte gir nytt sikkerhetsstempel, så andre innlogginger (f.eks. på en annen PC) blir logget ut.
        // Denne innloggingen fornyes, så brukeren slipper å logge inn på nytt her.
        await signInManager.RefreshSignInAsync(user);
        TempData["Message"] = "Passordet ble byttet.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Settings/Download: alt vi lagrer om brukeren, som en JSON-fil (retten til innsyn i personopplysningsloven).
    [HttpGet]
    public async Task<IActionResult> Download()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var data = new
        {
            user.Name,
            user.Email,
            user.PhoneNumber,
            user.CreatedAt,
            Roles = (await roleVerification.GetRolesAsync(user.Id)).Select(role => role.RoleName),
            Resources = resourceRepository.GetAll().Where(resource => resource.UserId == user.Id)
                .Select(resource => new { resource.Name, resource.Description, resource.CategoryName, resource.Quantity, resource.Status, resource.Address, resource.PostalCode, resource.PostalPlace, resource.Latitude, resource.Longitude, resource.RegisteredAt }),
            Needs = needRepository.GetAll().Where(need => need.UserId == user.Id)
                .Select(need => new { need.Title, need.Description, need.Quantity, need.Priority, need.Status, need.RegisteredAt })
        };
        var json = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions { WriteIndented = true });
        return File(json, "application/json", "mine-data-kriseklar.json");
    }

    // POST /Settings/Delete: sletter kontoen etter at passordet er bekreftet.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string? confirmPassword)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        // En admin kan ikke slette seg selv her, så appen aldri står uten admin ved et uhell.
        if (await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            TempData["Error"] = "En admin kan ikke slette sin egen konto. Be en annen admin gjøre det.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrEmpty(confirmPassword) || !await userManager.CheckPasswordAsync(user, confirmPassword))
        {
            TempData["Error"] = "Feil passord. Kontoen ble ikke slettet.";
            return RedirectToAction(nameof(Index));
        }

        // Ressursene og behovene blir stående uten eier (ON DELETE SET NULL), så de forsvinner ikke fra kartet.
        await userManager.DeleteAsync(user);
        await signInManager.SignOutAsync();
        TempData["Message"] = "Kontoen din er slettet.";
        return RedirectToAction("Login", "Account");
    }

    // ---------- Hjelpemetoder ----------

    private async Task<SettingsViewModel> BuildModel(ApplicationUser user) => new()
    {
        Profile = new ProfileViewModel { Name = user.Name ?? string.Empty, Email = user.Email ?? string.Empty, PhoneNumber = user.PhoneNumber },
        Role = UserAdminController.ActiveRole(await roleVerification.GetRolesAsync(user.Id)),
        CreatedAt = user.CreatedAt,
        ResourceCount = resourceRepository.GetAll().Count(resource => resource.UserId == user.Id),
        NeedCount = needRepository.GetAll().Count(need => need.UserId == user.Id)
    };

    // Viser siden igjen med feilmeldingene. Skjemaet med feil beholder det brukeren skrev, de andre fylles fra databasen.
    private async Task<IActionResult> ShowWithErrors(ApplicationUser user, ProfileViewModel? profile = null, ChangePasswordViewModel? password = null)
    {
        var model = await BuildModel(user);
        if (profile is not null)
        {
            model.Profile = profile;
        }
        if (password is not null)
        {
            model.Password = new ChangePasswordViewModel(); // Passord sendes aldri tilbake til nettleseren.
        }
        return View(nameof(Index), model);
    }
}
