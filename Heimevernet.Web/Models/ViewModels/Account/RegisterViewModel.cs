using System.ComponentModel.DataAnnotations;

namespace Heimevernet.Web.Models.ViewModels.Account;

// Data fra registreringsskjemaet. Valideringen virker på samme måte som i LoginViewModel.
public class RegisterViewModel
{
    [Required(ErrorMessage = "Skriv inn navnet ditt.")]
    [StringLength(100, ErrorMessage = "Navnet kan ha maks {1} tegn.")]
    [Display(Name = "Navn")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv inn e-post.")]
    [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
    [Display(Name = "E-post")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv inn passord.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Passordet må ha minst {2} tegn.")]
    [DataType(DataType.Password)]
    [Display(Name = "Passord")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    // [Compare] sjekker at feltet er likt Password.
    [Compare(nameof(Password), ErrorMessage = "Passordene er ikke like.")]
    [Display(Name = "Gjenta passord")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
    [StringLength(20, ErrorMessage = "Telefonnummeret kan ha maks {1} tegn.")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    // Sjekkes i AccountController.Register. [Required] godtar false.
    [Display(Name = "Jeg samtykker til at navn, e-post og telefon lagres og vises for andre brukere i appen.")]
    public bool Consent { get; set; }
}
