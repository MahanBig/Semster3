using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Web.Models.Entities;

// IdentityRole + feltene fra Rolle i ER-diagrammet.
public class ApplicationRole : IdentityRole
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    public string? Description { get; set; }
    public int AccessLevel { get; set; }
    public bool RequiresVerification { get; set; }
}
