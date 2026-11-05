using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Web.Models.Entities;

// IdentityUser + feltene fra Bruker i ER-diagrammet som Identity ikke har.
public class ApplicationUser : IdentityUser
{
    public string? Name { get; set; }
    public bool Consent { get; set; } // Samtykke til at navn, e-post og telefon vises for andre brukere.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // UTC
}
