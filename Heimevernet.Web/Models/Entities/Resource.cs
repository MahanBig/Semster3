namespace Heimevernet.Web.Models.Entities;

// Én ressurs som noen kan låne ut eller gi bort, f.eks. et aggregat. Tilsvarer én rad i Resources.
// Egenskapsnavnene er like kolonnenavnene, så Dapper kan fylle objektet automatisk.
public class Resource
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Status { get; set; } = ResourceStatus.Available;
    public DateTime RegisteredAt { get; set; } // UTC
    public string? UserId { get; set; } // Eieren. Null for eksempeldata og hvis brukeren er slettet.
    public int CategoryId { get; set; }
    public int? LocationId { get; set; }

    // Fra andre tabeller (JOIN)
    public string CategoryName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? PostalPlace { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public string? OwnerPhone { get; set; }
}
