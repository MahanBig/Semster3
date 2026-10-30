using System.ComponentModel.DataAnnotations;

namespace Heimevernet.Web.Models.ViewModels.Resource
{
    // Brukes både til å vise ressurser og i skjemaet for ny ressurs.
    // Lengdene matcher kolonnene i tabellen Resources (Schema.sql).
    public class ResourceViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Skriv et navn.")]
        [StringLength(200, ErrorMessage = "Navnet kan ha maks {1} tegn.")]
        [Display(Name = "Navn")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Skriv en beskrivelse.")]
        [StringLength(2000, ErrorMessage = "Beskrivelsen kan ha maks {1} tegn.")]
        [Display(Name = "Beskrivelse")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Skriv en type, f.eks. Kjøretøy.")]
        [StringLength(100, ErrorMessage = "Typen kan ha maks {1} tegn.")]
        [Display(Name = "Type")]
        public string Type { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Adressen kan ha maks {1} tegn.")]
        [Display(Name = "Adresse")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "Poststedet kan ha maks {1} tegn.")]
        [Display(Name = "Poststed")]
        public string? City { get; set; }

        [RegularExpression(@"^\d{4}$", ErrorMessage = "Postnummer må være 4 siffer.")]
        [Display(Name = "Postnummer")]
        public string? ZipCode { get; set; }

        [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
        [StringLength(20, ErrorMessage = "Telefonnummeret kan ha maks {1} tegn.")]
        [Display(Name = "Eiers telefonnummer")]
        public string? OwnerTelephoneNumber { get; set; }
    }
}
