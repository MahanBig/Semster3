using System.ComponentModel.DataAnnotations;

namespace Heimevernet.Web.Models.ViewModels
{
    // Data fra skjemaet på /Home/CorrectMap.
    // Koordinatene er tekst fordi JavaScript skriver dem med punktum (58.12). En norsk Windows ville ellers
    // forventet komma. Controlleren gjør dem om til tall med InvariantCulture.
    public class PositionModel
    {
        private const string NumberPattern = @"^-?\d{1,3}(\.\d+)?$";

        [Required(ErrorMessage = "Klikk i kartet for å velge en posisjon.")]
        [RegularExpression(NumberPattern, ErrorMessage = "Ugyldig breddegrad.")]
        public string Latitude { get; set; } = string.Empty;

        [Required(ErrorMessage = "Klikk i kartet for å velge en posisjon.")]
        [RegularExpression(NumberPattern, ErrorMessage = "Ugyldig lengdegrad.")]
        public string Longitude { get; set; } = string.Empty;

        [Required(ErrorMessage = "Skriv en beskrivelse.")]
        [StringLength(1000, ErrorMessage = "Beskrivelsen kan ha maks {1} tegn.")]
        [Display(Name = "Beskrivelse")]
        public string Description { get; set; } = string.Empty;
    }
}
