using System.Security.Cryptography;

namespace Heimevernet.Web.Security;

// Content-Security-Policy (CSP): en liste nettleseren sjekker før den laster noe.
// Klarer en angriper å få inn et <script> på en side (XSS), nekter nettleseren å kjøre det,
// fordi det verken kommer fra vår egen server eller har riktig nonce.
//
// Nonce ("number used once"): en tilfeldig verdi som lages på nytt for HVER forespørsel.
// Headeren sier "kjør bare inline-scripts med denne verdien", og våre egne <script nonce="..."> har den.
// En angriper kan ikke gjette den, fordi den er ny hver gang siden lastes.
public static class ContentSecurityPolicy
{
    // Nøkkelen nonce-en lagres under i HttpContext.Items, så viewene kan hente den: Context.Items[NonceKey].
    public const string NonceKey = "CspNonce";

    // 16 tilfeldige byte fra en kryptografisk sikker generator (ikke Random, som kan forutsies).
    public static string CreateNonce() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    public static string Build(string nonce, bool isDevelopment)
    {
        // Under utvikling kobler Visual Studio (Browser Link / Hot Reload) seg til appen via localhost.
        // Uten dette ville CSP-en blokkert dem. I produksjon er de ikke med.
        var devConnect = isDevelopment ? " ws://localhost:* wss://localhost:* http://localhost:*" : "";

        string[] directives =
        [
            // Utgangspunkt: alt som ikke står under, får bare komme fra vår egen server.
            "default-src 'self'",
            // Scripts: filene våre i wwwroot + inline-scripts som har dagens nonce.
            $"script-src 'self' 'nonce-{nonce}'",
            // Stiler: egne filer + Google Fonts. 'unsafe-inline' trengs for style="..." i viewene
            // (f.eks. kategorifargene). Inline-stiler er mye mindre farlige enn inline-scripts, de kan ikke kjøre kode.
            "style-src 'self' https://fonts.googleapis.com 'unsafe-inline'",
            "font-src 'self' https://fonts.gstatic.com",
            // Bilder: egne + kartflisene fra OpenStreetMap. data: brukes av Bootstrap (pil i nedtrekkslister,
            // hake i avkrysningsbokser). blob: brukes av forhåndsvisningen av bildet du laster opp.
            "img-src 'self' data: blob: https://*.tile.openstreetmap.org",
            // fetch(): adresseoppslaget når man klikker i kartet (Nominatim).
            "connect-src 'self' https://nominatim.openstreetmap.org" + devConnect,
            // Ingen <object>/<embed> (gamle plugins som Flash). Brukes ikke, og er et kjent angrepspunkt.
            "object-src 'none'",
            // Hindrer at en injisert <base href="..."> får alle relative lenker til å peke til en annen side.
            "base-uri 'self'",
            // Skjemaer kan bare sendes til vår egen server.
            "form-action 'self'",
            // Siden kan ikke vises i en <iframe> på andre sider (samme som X-Frame-Options: DENY, men nyere).
            "frame-ancestors 'none'"
        ];
        return string.Join("; ", directives);
    }
}
