namespace Heimevernet.Web.Models;

// Reglene for hva skjemafeltene godtar, samlet ett sted, så "gyldig telefonnummer" betyr det samme overalt.
// De brukes i [RegularExpression(...)] i view modellene. Samme regex sjekkes to steder:
//   1. I nettleseren (jQuery Validation leser data-val-regex-attributtet som asp-for lager), for rask tilbakemelding.
//   2. På serveren (ModelState.IsValid). Det er DENNE som faktisk beskytter appen, fordi nettleseren kan omgås.
// NB: Regexene må virke både i .NET og i JavaScript. Derfor brukes ikke \p{L} (krever u-flagget i JavaScript),
// men bokstavområder: A-Z, a-z og À-ÖØ-öø-ÿ (som dekker æ, ø, å, é, ü osv.).
public static class InputRules
{
    // Telefon: valgfri + først, deretter 8 til 15 siffer. Ett mellomrom mellom sifrene er lov ("+47 912 34 567").
    // 8 siffer er et norsk nummer. 15 er det lengste et internasjonalt nummer kan være (standarden E.164).
    public const string Phone = @"^\+?(\d ?){7,14}\d$";
    public const string PhoneMessage = "Skriv et gyldig telefonnummer, f.eks. 912 34 567 eller +47 912 34 567.";

    // E-post: noe@noe.noe, uten mellomrom. [EmailAddress] alene sjekker bare at det finnes én @,
    // så "a@b" ville blitt godtatt. Dette er ikke en perfekt e-postsjekk (det finnes ingen enkel en),
    // men den stopper de vanligste skrivefeilene. Den eneste sikre sjekken er å sende en e-post.
    public const string Email = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string EmailMessage = "Skriv en gyldig e-postadresse, f.eks. ola@eksempel.no.";

    // Navn på personer: bokstaver, mellomrom, bindestrek, apostrof og punktum ("Anne-Lise O'Hara Jr.").
    // Ingen tall eller tegn som < > @. Må starte med en bokstav.
    public const string PersonName = @"^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ .'-]*$";
    public const string PersonNameMessage = "Navnet kan bare ha bokstaver, mellomrom, bindestrek og apostrof.";

    // Poststed: bokstaver, mellomrom og bindestrek ("Kristiansand S", "Mo i Rana", "Bø i Telemark").
    public const string PostalPlace = @"^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ .'-]*$";
    public const string PostalPlaceMessage = "Poststedet kan bare ha bokstaver, mellomrom og bindestrek.";

    // Postnummer: nøyaktig 4 siffer (norske postnumre).
    public const string PostalCode = @"^\d{4}$";
    public const string PostalCodeMessage = "Postnummer må være 4 siffer.";

    // Antall (ressurser og behov). Brukes både i [Range] og i HTML-attributtene min/max.
    public const int MinQuantity = 1;
    public const int MaxQuantity = 100000;
}
