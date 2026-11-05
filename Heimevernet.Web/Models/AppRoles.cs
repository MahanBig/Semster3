using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.Models;

// Navnene på rollene samlet ett sted, så vi slipper skrivefeil som "admin" vs "Admin".
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "Bruker";

    public static readonly IReadOnlyList<string> All = [Admin, User];

    // Lager rolleobjektet med beskrivelse og tilgangsnivå. Brukes av IdentitySeeder ved oppstart.
    public static ApplicationRole Create(string roleName) => roleName switch
    {
        Admin => new ApplicationRole(Admin)
        {
            Description = "Kan administrere brukere og endre alle ressurser og behov.",
            AccessLevel = 100,
            RequiresVerification = true
        },
        User => new ApplicationRole(User)
        {
            Description = "Vanlig bruker. Kan melde inn ressurser og behov.",
            AccessLevel = 10,
            RequiresVerification = false
        },
        _ => throw new ArgumentOutOfRangeException(nameof(roleName), roleName, "Unknown role.")
    };
}
