using System.ComponentModel.DataAnnotations;

namespace Heimevernet.Web.Models.ViewModels.Account;

public class RegisterViewModel
{
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
    [Compare(nameof(Password), ErrorMessage = "Passordene er ikke like.")]
    [Display(Name = "Gjenta passord")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
