# Resultat: system- og sikkerhetstest

Kjørt 2026-11-03 17:49 mot http://localhost:8080. **55 av 55 bestått.**

## System

| Resultat | Test | Detalj |
|---|---|---|
| ✅ | Forsiden (kart) åpnes | 200 |
| ✅ | Ressurslisten viser ressurser fra databasen | 9 ressurser |
| ✅ | Utlogget ser "Logg inn for å legge til" i stedet for "Ny ressurs" |  |
| ✅ | Ressurssiden åpnes | 200 |
| ✅ | Utlogget ser ikke Endre/Slett |  |
| ✅ | Ukjent ressurs gir 404 | 404 |
| ✅ | Ny bruker kan registrere seg | 302 |
| ✅ | Ny bruker er logget inn etter registrering |  |
| ✅ | Bruker nr. 2 kan registrere seg | 302 |
| ✅ | Innlogget bruker ser "Ny ressurs" |  |
| ✅ | Innlogget bruker kan åpne "Ny ressurs" | 200 |
| ✅ | Ressurs uten navn blir avvist med feilmelding | 200 |
| ✅ | Bruker legger til ressurs | /Resource/Index/12 |
| ✅ | Ressursen lagres med alle felt og eier |  |
| ✅ | Eier ser Endre og Slett |  |
| ✅ | Eier kan åpne Endre | 200 |
| ✅ | Eier endrer ressursen, endringen lagres |  |
| ✅ | Annen bruker ser ikke Endre/Slett |  |
| ✅ | Admin logger inn | 302 |
| ✅ | Admin kan åpne Endre på andres ressurs | 200 |
| ✅ | Admin kan åpne brukeradministrasjon | 200 |
| ✅ | Innlogget bruker kan åpne "Meld inn posisjon" | 200 |
| ✅ | Innmeldt posisjon lagres og vises i oversikten |  |
| ✅ | Eier får bekreftelsesside før sletting | 200 |
| ✅ | Eier sletter ressursen, den er borte | 302, deretter 404 |
| ✅ | Admin kan slette andres ressurs | 404 |
| ✅ | Etter utlogging kreves innlogging igjen | http://localhost:8080/Account/Login?ReturnUrl=%2FResourceHandler%2FCreate |
| ✅ | Etter omstart: data er beholdt og ikke duplisert | 10 før, 10 etter |
| ✅ | Etter omstart: admin er fortsatt logget inn | 200 |

## Sikkerhet

| Resultat | Test | Detalj |
|---|---|---|
| ✅ | Header X-Content-Type-Options: nosniff | nosniff |
| ✅ | Header X-Frame-Options: DENY (clickjacking) | DENY |
| ✅ | Header Referrer-Policy | strict-origin-when-cross-origin |
| ✅ | Header Permissions-Policy | geolocation=(self), camera=(), microphone=() |
| ✅ | Utlogget: "Ny ressurs" krever innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2FResourceHandler%2FCreate |
| ✅ | Utlogget: Endre krever innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2FResourceHandler%2FEdit%2F12 |
| ✅ | Utlogget: "Meld inn posisjon" krever innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2FHome%2FCorrectMap |
| ✅ | Utlogget: brukeradministrasjon krever innlogging | http://localhost:8080/Account/Login?ReturnUrl=%2FUserAdmin |
| ✅ | Vanlig bruker får "Ingen tilgang" på brukeradministrasjon | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FUserAdmin |
| ✅ | Annen bruker får "Ingen tilgang" på Endre (GET) | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FResourceHandler%2FEdit%2F12 |
| ✅ | Annen bruker får "Ingen tilgang" på Slett (GET) | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FResourceHandler%2FDelete%2F12 |
| ✅ | Annen bruker kan ikke endre ved å sende POST direkte | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FResourceHandler%2FEdit%2F12 |
| ✅ | Annen bruker kan ikke slette ved å sende POST direkte | http://localhost:8080/Account/AccessDenied?ReturnUrl=%2FResourceHandler%2FDelete%2F12 |
| ✅ | Ressursen er uendret etter angrepene |  |
| ✅ | CSRF: Slett uten token blir blokkert (400) | 400 |
| ✅ | CSRF: Ny ressurs uten token blir blokkert (400) | 400 |
| ✅ | CSRF: Posisjon uten token blir blokkert (400) | 400 |
| ✅ | XSS: script i beskrivelse blir vist som tekst (HTML-kodet) |  |
| ✅ | SQL-injeksjon: teksten lagres som tekst, tabellen finnes fortsatt |  |
| ✅ | Åpen redirect: ReturnUrl til annet nettsted blir ignorert | / |
| ✅ | Svakt passord blir avvist | 200 |
| ✅ | Samme e-post kan ikke registreres to ganger | 200 |
| ✅ | Feil passord gir samme melding som ukjent e-post |  |
| ✅ | Ukjent e-post gir samme melding (avslører ikke brukere) |  |
| ✅ | Kontoen låses etter 5 feil passord (også riktig passord avvises) | 200 |
| ✅ | Innloggings-cookien er HttpOnly (JavaScript kan ikke lese den) |  |
