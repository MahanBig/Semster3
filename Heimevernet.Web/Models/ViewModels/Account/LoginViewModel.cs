using System.ComponentModel.DataAnnotations;

namespace Heimevernet.Web.Models.ViewModels.Account;

// Data fra innloggingsskjemaet. Attributtene ([Required] osv.) brukes til validering
// både i nettleseren (jQuery Validation) og på serveren (ModelState.IsValid).
public class LoginViewModel
{
    [Required(ErrorMessage = "Skriv inn e-post.")]
    [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
    [Display(Name = "E-post")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv inn passord.")]
    [DataType(DataType.Password)]
    [Display(Name = "Passord")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Husk meg")]
    public bool RememberMe { get; set; }

    // Siden brukeren prøvde å åpne før hen ble sendt til innlogging.
    public string? ReturnUrl { get; set; }
}
