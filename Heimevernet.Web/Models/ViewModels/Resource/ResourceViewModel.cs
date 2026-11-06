using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Heimevernet.Web.Models.ViewModels.Resource
{
    // Brukes både til å vise ressurser og i skjemaet for ny/endret ressurs.
    // Lengdene matcher kolonnene i Resources, Locations og PostalCodes (Schema.sql).
    public class ResourceViewModel : IValidatableObject
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Skriv et navn.")]
        [StringLength(100, ErrorMessage = "Navnet kan ha maks {1} tegn.")]
        [Display(Name = "Navn")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Skriv en beskrivelse.")]
        [StringLength(2000, ErrorMessage = "Beskrivelsen kan ha maks {1} tegn.")]
        [Display(Name = "Beskrivelse")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Velg en kategori.")]
        [Display(Name = "Kategori")]
        public string Category { get; set; } = string.Empty;

        [Range(1, 100000, ErrorMessage = "Antall må være mellom {1} og {2}.")]
        [Display(Name = "Antall")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Velg en status.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = ResourceStatus.Available;

        [StringLength(150, ErrorMessage = "Adressen kan ha maks {1} tegn.")]
        [Display(Name = "Adresse")]
        public string? Address { get; set; }

        [RegularExpression(@"^\d{4}$", ErrorMessage = "Postnummer må være 4 siffer.")]
        [Display(Name = "Postnummer")]
        public string? PostalCode { get; set; }

        [StringLength(50, ErrorMessage = "Poststedet kan ha maks {1} tegn.")]
        [Display(Name = "Poststed")]
        public string? PostalPlace { get; set; }

        // Punktet brukeren valgte på kartet. Fylles inn av JavaScript i skjemaet (minikartet).
        [Range(-90, 90, ErrorMessage = "Ugyldig breddegrad.")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Ugyldig lengdegrad.")]
        public double? Longitude { get; set; }

        // Kontaktinfo for ressursen (valgfritt). Tomt = eierens telefon fra profilen vises.
        [StringLength(100, ErrorMessage = "Kontaktnavnet kan ha maks {1} tegn.")]
        [Display(Name = "Kontaktperson")]
        public string? ContactName { get; set; }

        [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
        [StringLength(20, ErrorMessage = "Telefonnummeret kan ha maks {1} tegn.")]
        [Display(Name = "Telefon")]
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
        [StringLength(255, ErrorMessage = "E-posten kan ha maks {1} tegn.")]
        [Display(Name = "E-post")]
        public string? ContactEmail { get; set; }

        public bool HasPosition => Latitude is not null && Longitude is not null;

        // Vises på siden, men tas aldri fra skjemaet ([BindNever]): controlleren setter dem selv.
        // Ellers kunne noen sende inn en annen eier og "eie" ressursen (overposting).
        [BindNever]
        public string? OwnerName { get; set; }

        [BindNever]
        public string? OwnerPhone { get; set; }

        [BindNever]
        public DateTime? RegisteredAt { get; set; }

        [BindNever]
        public bool CanModify { get; set; }

        [BindNever]
        public bool HasImage { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ResourceStatus.All.Contains(Status))
            {
                yield return new ValidationResult("Ugyldig status.", [nameof(Status)]);
            }

            if (string.IsNullOrWhiteSpace(PostalCode) != string.IsNullOrWhiteSpace(PostalPlace))
            {
                yield return new ValidationResult(
                    "Fyll inn både postnummer og poststed, eller ingen av dem.", [nameof(PostalCode), nameof(PostalPlace)]);
            }

            if ((Latitude is null) != (Longitude is null))
            {
                yield return new ValidationResult("Velg et punkt på kartet, eller fjern det.", [nameof(Latitude)]);
            }
        }

        public static ResourceViewModel FromEntity(Entities.Resource resource) => new()
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            Category = resource.CategoryName,
            Quantity = resource.Quantity,
            Status = resource.Status,
            Address = resource.Address,
            PostalCode = resource.PostalCode,
            PostalPlace = resource.PostalPlace,
            Latitude = resource.Latitude,
            Longitude = resource.Longitude,
            HasImage = resource.HasImage,
            ContactName = resource.ContactName,
            ContactPhone = resource.ContactPhone,
            ContactEmail = resource.ContactEmail,
            OwnerName = resource.OwnerName ?? resource.OwnerEmail,
            OwnerPhone = resource.OwnerPhone,
            RegisteredAt = resource.RegisteredAt
        };
    }
}
