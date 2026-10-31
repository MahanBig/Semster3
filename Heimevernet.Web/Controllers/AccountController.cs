using Heimevernet.Web.Models;
using Heimevernet.Web.Models.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.Controllers;

// Innlogging, registrering og utlogging.
// SignInManager håndterer cookien (logget inn eller ikke). UserManager håndterer brukerne i databasen.
public class AccountController(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // Stopper at en annen nettside sender inn skjemaet på vegne av brukeren (CSRF).
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Brukernavnet er e-posten (se Register). lockoutOnFailure teller feil passord.
        var result = await signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        // Samme melding for feil e-post og feil passord, så ingen kan finne ut hvilke e-poster som finnes.
        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Kontoen er midlertidig låst etter for mange forsøk. Prøv igjen senere."
            : "Feil e-post eller passord.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new IdentityUser { UserName = model.Email, Email = model.Email };

        // CreateAsync hasher passordet. Selve passordet lagres aldri i databasen.
        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Alle nye brukere får den vanlige rollen. Admin må gis av en annen admin.
            await userManager.AddToRoleAsync(user, AppRoles.User);
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        // F.eks. "e-posten er allerede i bruk" eller "passordet må ha et tall".
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpPost] // POST og ikke GET, så en lenke eller et bilde på en annen side ikke kan logge deg ut.
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // Sender bare videre til adresser på vår egen side. Ellers kan noen lage en innloggingslenke
    // som sender brukeren til en falsk side etterpå (open redirect).
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}
