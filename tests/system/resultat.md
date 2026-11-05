# Resultat: system- og sikkerhetstest

Kjørt 2026-11-05 21:53 mot http://localhost:8080. **61 av 61 bestått.**

## System

| Resultat | Test | Detalj |
|---|---|---|
| ✅ | Ny bruker kan registrere seg | 302 |
| ✅ | Ny bruker er logget inn etter registrering |  |
| ✅ | Bruker nr. 2 kan registrere seg | 302 |
| ✅ | Forsiden (kart) åpnes for innlogget bruker | 200 |
| ✅ | Forsiden har pluss-knappen for å sette en pinne og registrere en ressurs |  |
| ✅ | Ressurslisten viser ressurser fra databasen | 7 ressurser |
| ✅ | Ressurssiden åpnes | 200 |
| ✅ | Ressurs uten eier: vanlig bruker ser ikke Endre/Slett |  |
| ✅ | Ukjent ressurs gir 404 | 404 |
| ✅ | Innlogget bruker ser "Ny ressurs" |  |
| ✅ | Innlogget bruker kan åpne "Ny ressurs" | 200 |
| ✅ | Ressurs uten navn blir avvist med feilmelding | 200 |
| ✅ | Bruker legger til ressurs | /Resource/Index/16 |
| ✅ | Ressursen lagres med alle felt og eier |  |
| ✅ | Punktet fra kartet lagres og vises på ressurssiden |  |
| ✅ | Eier ser Endre og Slett |  |
| ✅ | Ressurslisten har punktet, så avstand og kartmarkør kan vises |  |
| ✅ | Ressurslisten har filtre for avstand, kategori, sted og sortering |  |
| ✅ | "Legg til ressurs her" fra kartet fyller inn punktet i skjemaet |  |
| ✅ | Eier kan åpne Endre | 200 |
| ✅ | Eier endrer ressursen, endringen lagres |  |
| ✅ | Annen bruker ser ikke Endre/Slett |  |
| ✅ | Admin logger inn | 302 |
| ✅ | Admin kan åpne Endre på andres ressurs | 200 |
| ✅ | Admin kan åpne brukeradministrasjon | 200 |
| ✅ | Eier får bekreftelsesside før sletting | 200 |
| ✅ | Eier sletter ressursen, den er borte | 302, deretter 404 |
| ✅ | Admin kan slette andres ressurs | 404 |
| ✅ | Etter utlogging kreves innlogging igjen | http://localhost:8080/Account/Login?ReturnUrl=%2FResourceHandler%2FCreate |
| ✅ | Etter omstart: data er beholdt og ikke duplisert | 8 før, 8 etter |
| ✅ | Etter omstart: admin er fortsatt logget inn | 200 |

## Sikkerhet

| Resultat | Test | Detalj |
|---|---|---|
| ✅ | Header X-Content-Type-Options: nosniff | nosniff |
| ✅ | Header X-Frame-Options: DENY (clickjacking) | DENY |
| ✅ | Header Referrer-Policy | strict-origin-when-cross-origin |
| ✅ | Header Permissions-Policy | geolocation=(), camera=(), microphone=() |
| ✅ | Utlogget: forsiden sender til innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2F |
| ✅ | Utlogget: ressurslisten sender til innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2FResourceHandler |
| ✅ | Utlogget: ressurssiden sender til innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2FResource%2FIndex%2F16 |
| ✅ | Innloggingssiden er åpen og har ikke "Fortsett uten å logge inn" | 200 |
| ✅ | Registreringssiden er åpen | 200 |
| ✅ | Logo og CSS lastes uten innlogging (innloggingssiden ser riktig ut) | 200 |
| ✅ | Utlogget: "Ny ressurs" krever innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2FResourceHandler%2FCreate |
| ✅ | Utlogget: Endre krever innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2FResourceHandler%2FEdit%2F16 |
| ✅ | Utlogget: brukeradministrasjon krever innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2FUserAdmin |
| ✅ | Vanlig bruker får "Ingen tilgang" på brukeradministrasjon | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FUserAdmin |
| ✅ | Annen bruker får "Ingen tilgang" på Endre (GET) | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FResourceHandler%2FEdit%2F16 |
| ✅ | Annen bruker får "Ingen tilgang" på Slett (GET) | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FResourceHandler%2FDelete%2F16 |
| ✅ | Annen bruker kan ikke endre ved å sende POST direkte | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FResourceHandler%2FEdit%2F16 |
| ✅ | Annen bruker kan ikke slette ved å sende POST direkte | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FResourceHandler%2FDelete%2F16 |
| ✅ | Ressursen er uendret etter angrepene |  |
| ✅ | CSRF: Slett uten token blir blokkert (400) | 400 |
| ✅ | CSRF: Ny ressurs uten token blir blokkert (400) | 400 |
| ✅ | XSS: script i beskrivelse blir vist som tekst (HTML-kodet) |  |
| ✅ | SQL-injeksjon: teksten lagres som tekst, tabellen finnes fortsatt |  |
| ✅ | Åpen redirect: ReturnUrl til annet nettsted blir ignorert | / |
| ✅ | Svakt passord blir avvist | 200 |
| ✅ | Samme e-post kan ikke registreres to ganger | 200 |
| ✅ | Feil passord gir samme melding som ukjent e-post |  |
| ✅ | Ukjent e-post gir samme melding (avslører ikke brukere) |  |
| ✅ | Kontoen låses etter 5 feil passord (også riktig passord avvises) | 200 |
| ✅ | Innloggings-cookien er HttpOnly (JavaScript kan ikke lese den) |  |
