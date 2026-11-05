namespace Heimevernet.Web.Models;

// Verdiene i ENUM-kolonnene i Schema.sql. Må være helt like verdiene i databasen.
// Med konstanter gir en skrivefeil kompileringsfeil i stedet for en feil i databasen.
// Label gir teksten som vises til brukeren. En ukjent verdi vises som den er.

public static class ResourceStatus
{
    public const string Available = "tilgjengelig";
    public const string InUse = "i_bruk";
    public const string Unavailable = "utilgjengelig";

    public static readonly IReadOnlyList<string> All = [Available, InUse, Unavailable];

    public static string Label(string? status) => status switch
    {
        Available => "Tilgjengelig",
        InUse => "I bruk",
        Unavailable => "Utilgjengelig",
        _ => status ?? string.Empty
    };
}

public static class NeedStatus
{
    public const string Open = "åpen";
    public const string InProgress = "under_behandling";
    public const string Covered = "dekket";

    public static readonly IReadOnlyList<string> All = [Open, InProgress, Covered];

    public static string Label(string? status) => status switch
    {
        Open => "Åpen",
        InProgress => "Under behandling",
        Covered => "Dekket",
        _ => status ?? string.Empty
    };
}

public static class NeedPriority
{
    public const string Low = "lav";
    public const string Medium = "middels";
    public const string High = "høy";
    public const string Critical = "kritisk";

    public static readonly IReadOnlyList<string> All = [Low, Medium, High, Critical];

    public static string Label(string? priority) => priority switch
    {
        Low => "Lav",
        Medium => "Middels",
        High => "Høy",
        Critical => "Kritisk",
        _ => priority ?? string.Empty
    };

    // Tekstfarge og lys bakgrunnsfarge til prioritetsmerket. Kritisk er rød, så det er det som synes best.
    public static (string Color, string Tint) Colors(string? priority) => priority switch
    {
        Critical => ("#B91C1C", "#FBE4E4"),
        High => ("#92400E", "#F6EADF"),
        Medium => ("#1D4ED8", "#E3EAFB"),
        _ => ("#334155", "#E6EAEF")
    };
}
