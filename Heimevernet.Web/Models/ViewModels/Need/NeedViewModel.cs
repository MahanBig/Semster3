using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Heimevernet.Web.Models.ViewModels.Need;

// Brukes både til å vise behov og i skjemaet for nytt/endret behov.
// Lengdene matcher kolonnene i Needs, Locations og PostalCodes (Schema.sql).
public class NeedViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Skriv en tittel.")]
    [StringLength(100, ErrorMessage = "Tittelen kan ha maks {1} tegn.")]
    [Display(Name = "Tittel")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Beskriv hva som trengs.")]
    [StringLength(2000, ErrorMessage = "Beskrivelsen kan ha maks {1} tegn.")]
    [Display(Name = "Beskrivelse")]
    public string Description { get; set; } = string.Empty;

    // Kategorinavnet. Controlleren slår opp Id-en (NeedController.FindCategory).
    [Required(ErrorMessage = "Velg en kategori.")]
    [Display(Name = "Kategori")]
    public string Category { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Antall må være mellom {1} og {2}.")]
    [Display(Name = "Antall som trengs")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Velg en prioritet.")]
    [Display(Name = "Prioritet")]
    public string Priority { get; set; } = NeedPriority.Medium;

    [DataType(DataType.Date)]
    [Display(Name = "Frist")]
    public DateTime? Deadline { get; set; }

    [Display(Name = "Organisasjon")]
    public int? OrganizationId { get; set; }

    [StringLength(150, ErrorMessage = "Adressen kan ha maks {1} tegn.")]
    [Display(Name = "Adresse")]
    public string? Address { get; set; }

    [RegularExpression(@"^\d{4}$", ErrorMessage = "Postnummer må være 4 siffer.")]
    [Display(Name = "Postnummer")]
    public string? PostalCode { get; set; }

    [StringLength(50, ErrorMessage = "Poststedet kan ha maks {1} tegn.")]
    [Display(Name = "Poststed")]
    public string? PostalPlace { get; set; }

    // Bare for visning. [BindNever] hindrer at de sendes inn fra skjemaet.

    [BindNever]
    public string Status { get; set; } = NeedStatus.Open;

    [BindNever]
    public int AllocatedQuantity { get; set; }

    [BindNever]
    public string? OrganizationName { get; set; }

    [BindNever]
    public string? OwnerName { get; set; }

    [BindNever]
    public string? OwnerPhone { get; set; }

    [BindNever]
    public DateTime? RegisteredAt { get; set; }

    [BindNever]
    public bool CanModify { get; set; }

    // Regler attributtene ikke kan uttrykke, f.eks. regler som gjelder to felt sammen.
    // IValidatableObject gjør at ASP.NET kjører denne på serveren etter at attributtene er sjekket.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!NeedPriority.All.Contains(Priority))
        {
            yield return new ValidationResult("Ugyldig prioritet.", [nameof(Priority)]);
        }

        if (string.IsNullOrWhiteSpace(PostalCode) != string.IsNullOrWhiteSpace(PostalPlace))
        {
            yield return new ValidationResult(
                "Fyll inn både postnummer og poststed, eller ingen av dem.", [nameof(PostalCode), nameof(PostalPlace)]);
        }
    }

    // Fra entitet (databasen) til view model. Har eieren ikke lagt inn navn, vises e-posten.
    public static NeedViewModel FromEntity(Entities.Need need) => new()
    {
        Id = need.Id,
        Title = need.Title,
        Description = need.Description,
        Category = need.CategoryName,
        Quantity = need.Quantity,
        Priority = need.Priority,
        Deadline = need.Deadline,
        OrganizationId = need.OrganizationId,
        Address = need.Address,
        PostalCode = need.PostalCode,
        PostalPlace = need.PostalPlace,
        Status = need.Status,
        AllocatedQuantity = need.AllocatedQuantity,
        OrganizationName = need.OrganizationName,
        OwnerName = need.OwnerName ?? need.OwnerEmail,
        OwnerPhone = need.OwnerPhone,
        RegisteredAt = need.RegisteredAt
    };
}
