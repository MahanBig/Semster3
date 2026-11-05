using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.Models;

// Navnene på rollene samlet ett sted, så vi slipper skrivefeil som "admin" vs "Admin".
// Hver bruker har én av disse rollene:
//   Privat:    privatperson. Ser og melder inn bare sine egne ressurser. Alle nye brukere starter her.
//   Offentlig: offentlig aktør (kommune, etat, beredskap). Ser alle ressursene og kan melde inn nye.
//   Admin:     kan alt over, og har admin-siden (roller, brukere, ressurser og behov).
// Endre og slette en ressurs kan bare eieren og admin (se Authorization/ResourcePermissions.cs).
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Public = "Offentlig";
    public const string Private = "Privat";

    // Rekkefølgen er fra mest til minst tilgang. Brukes i nedtrekkslisten på admin-siden.
    public static readonly IReadOnlyList<string> All = [Admin, Public, Private];

    // Kort forklaring som vises på admin-siden og i innstillingene.
    public static string Describe(string? role) => role switch
    {
        Admin => "Kan se, endre og slette alt, og styre brukere og roller.",
        Public => "Ser alle ressursene og kan melde inn nye.",
        Private => "Ser og melder inn bare sine egne ressurser.",
        _ => string.Empty
    };

    // Lager rolleobjektet med beskrivelse og tilgangsnivå. Brukes av IdentitySeeder ved oppstart.
    // Admin og Offentlig krever godkjenning: de teller ikke før en admin har gitt dem (VerifiedAt i AspNetUserRoles).
    public static ApplicationRole Create(string roleName) => roleName switch
    {
        Admin => new ApplicationRole(Admin) { Description = Describe(Admin), AccessLevel = 100, RequiresVerification = true },
        Public => new ApplicationRole(Public) { Description = Describe(Public), AccessLevel = 50, RequiresVerification = true },
        Private => new ApplicationRole(Private) { Description = Describe(Private), AccessLevel = 10, RequiresVerification = false },
        _ => throw new ArgumentOutOfRangeException(nameof(roleName), roleName, "Unknown role.")
    };
}
