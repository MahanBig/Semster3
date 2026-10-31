namespace Heimevernet.Web.Models;

// Kategoriene fra designet (Beredskapskart): navn, farge, lys bakgrunnsfarge og ikon.
// Ressurser har bare en fritekst-"Type" i databasen, så FromType gjetter kategori ut fra ord i typen.
public sealed record ResourceCategory(string Key, string Label, string Color, string Tint, string IconPath, string[] Keywords)
{
    public static readonly ResourceCategory Food = new("mat", "Mat", "#2F7D4F", "#E3F1E8",
        "M7 3v8M5 3v5a2 2 0 0 0 4 0V3M7 11v10M17 3c-2 2-2 6 0 8v10",
        ["mat", "suppe", "matutdeling", "butikk"]);

    public static readonly ResourceCategory Water = new("vann", "Vann", "#0E7490", "#DDF1F5",
        "M12 3s6 6.5 6 11a6 6 0 0 1-12 0c0-4.5 6-11 6-11z",
        ["vann", "drikke"]);

    public static readonly ResourceCategory Shelter = new("ly", "Ly og varme", "#1D4ED8", "#E3EAFB",
        "M3 11l9-7 9 7M5 10v10h14V10M10 20v-5h4v5",
        ["ly", "varme", "overnatting", "husly", "bolig"]);

    public static readonly ResourceCategory Health = new("helse", "Helse", "#B91C1C", "#FBE4E4",
        "M9 3h6v6h6v6h-6v6H9v-6H3V9h6z",
        ["helse", "førstehjelp", "lege", "sanitet", "medisin"]);

    public static readonly ResourceCategory Tools = new("verktoy", "Verktøy", "#92400E", "#F6EADF",
        "M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.8-3.8a6 6 0 0 1-7.9 7.9l-6.9 6.9a2.1 2.1 0 0 1-3-3l6.9-6.9a6 6 0 0 1 7.9-7.9z",
        ["verktøy", "utstyr", "redskap", "kjøretøy", "pumpe"]);

    public static readonly ResourceCategory Power = new("strom", "Strøm og lading", "#6D28D9", "#EEE7FB",
        "M13 2L4 14h7l-1 8 9-12h-7z",
        ["strøm", "lading", "aggregat", "nødstrøm"]);

    public static readonly ResourceCategory Info = new("info", "Informasjon", "#334155", "#E6EAEF",
        "M12 3a9 9 0 1 0 0 18a9 9 0 1 0 0-18M12 11v5M12 8h.01",
        ["info", "informasjon"]);

    public static readonly IReadOnlyList<ResourceCategory> All = [Food, Water, Shelter, Health, Tools, Power, Info];

    // Finner kategorien som passer en ressurstype, f.eks. "Utstyr" -> Verktøy. Ukjent type gir Informasjon.
    public static ResourceCategory FromType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Info;
        }

        var text = type.Trim().ToLowerInvariant();
        foreach (var category in All)
        {
            if (string.Equals(category.Label, type.Trim(), StringComparison.OrdinalIgnoreCase)
                || category.Keywords.Any(keyword => text.Contains(keyword)))
            {
                return category;
            }
        }
        return Info;
    }

    // SVG-ikonet som HTML. Inneholder bare faste verdier fra denne klassen, aldri brukerdata.
    public string IconSvg(int size = 20, double strokeWidth = 2.2) =>
        $"""<svg width="{size}" height="{size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="{strokeWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)}" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="{IconPath}"></path></svg>""";
}
