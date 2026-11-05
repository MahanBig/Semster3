using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Web.Models.Entities;

// IdentityRole + feltene fra Rolle i ER-diagrammet.
public class ApplicationRole : IdentityRole
{
    // Dapper og Identity trenger en tom konstruktør for å kunne lage objektet.
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    public string? Description { get; set; }
    public int AccessLevel { get; set; } // Høyere tall gir mer tilgang (se AppRoles).
    public bool RequiresVerification { get; set; } // true: rollen gir ikke tilgang før den er godkjent.
}
