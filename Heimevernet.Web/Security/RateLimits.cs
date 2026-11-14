namespace Heimevernet.Web.Security;

// Rate limiting: hvor mange forsøk én IP-adresse får gjøre på kort tid.
// Uten dette kan et script prøve tusenvis av passord eller lage tusenvis av brukere i minuttet.
// (Låsingen etter 5 feil passord i Program.cs beskytter én konto. Rate limiting beskytter hele appen.)
//
// Brukes slik:
//   - Program.cs registrerer policyen (AddRateLimiter) og slår den på (UseRateLimiter).
//   - AccountController setter [EnableRateLimiting(RateLimits.Auth)] på innlogging og registrering.
public static class RateLimits
{
    // Navnet på policyen. Står som konstant så controller og Program.cs alltid bruker det samme navnet.
    public const string Auth = "auth";

    // Maks antall innloggings- og registreringsforsøk per IP-adresse per minutt.
    // 30 er langt mer enn et menneske trenger, men stopper automatiske angrep (et script får 30, ikke tusenvis).
    // Systemtesten gjør 21 slike forsøk på under ett minutt, så grensen må ligge godt over det.
    public const int AuthPermitLimit = 30;
    public static readonly TimeSpan AuthWindow = TimeSpan.FromMinutes(1);
}
