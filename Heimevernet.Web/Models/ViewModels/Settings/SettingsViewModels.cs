using System.ComponentModel.DataAnnotations;

namespace Heimevernet.Web.Models.ViewModels.Settings;

// Innstillingssiden: info om brukeren og tre skjemaer (profil, passord, slette konto).
// Hvert skjema har sin egen view model, så en feil i ett skjema ikke påvirker de andre.
public class SettingsViewModel
{
    public ProfileViewModel Profile { get; set; } = new();
    public ChangePasswordViewModel Password { get; set; } = new();

    // Bare til visning
    public string? Role { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ResourceCount { get; init; }
    public int NeedCount { get; init; }
    public bool IsAdmin => Role == AppRoles.Admin;
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Skriv inn navnet ditt.")]
    [StringLength(100, ErrorMessage = "Navnet kan ha maks {1} tegn.")]
    [Display(Name = "Navn")]
    public string Name { get; set; } = string.Empty;

    // E-posten er også brukernavnet når man logger inn.
    [Required(ErrorMessage = "Skriv inn e-post.")]
    [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
    [StringLength(256)]
    [Display(Name = "E-post")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
    [StringLength(20, ErrorMessage = "Telefonnummeret kan ha maks {1} tegn.")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Skriv inn passordet du har nå.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nåværende passord")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv inn et nytt passord.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Passordet må ha minst {2} tegn.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nytt passord")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passordene er ikke like.")]
    [Display(Name = "Gjenta nytt passord")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
