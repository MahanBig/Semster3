# Resultat: system- og sikkerhetstest

Kjørt 2026-11-04 23:36 mot http://localhost:8081. **56 av 56 bestått.**

## System

| Resultat | Test | Detalj |
|---|---|---|
| ✅ | Ny bruker kan registrere seg | 302 |
| ✅ | Ny bruker er logget inn etter registrering |  |
| ✅ | Bruker nr. 2 kan registrere seg | 302 |
| ✅ | Forsiden (kart) åpnes for innlogget bruker | 200 |
| ✅ | Ressurslisten viser ressurser fra databasen | 3 ressurser |
| ✅ | Ressurssiden åpnes | 200 |
| ✅ | Ressurs uten eier: vanlig bruker ser ikke Endre/Slett |  |
| ✅ | Ukjent ressurs gir 404 | 404 |
| ✅ | Innlogget bruker ser "Ny ressurs" |  |
| ✅ | Innlogget bruker kan åpne "Ny ressurs" | 200 |
| ✅ | Ressurs uten navn blir avvist med feilmelding | 200 |
| ✅ | Bruker legger til ressurs | /Resource/Index/4 |
| ✅ | Ressursen lagres med alle felt og eier |  |
| ✅ | Eier ser Endre og Slett |  |
| ✅ | Eier kan åpne Endre | 200 |
| ✅ | Eier endrer ressursen, endringen lagres |  |
| ✅ | Annen bruker ser ikke Endre/Slett |  |
| ✅ | Admin logger inn | 302 |
| ✅ | Admin kan åpne Endre på andres ressurs | 200 |
| ✅ | Admin kan åpne brukeradministrasjon | 200 |
| ✅ | Eier får bekreftelsesside før sletting | 200 |
| ✅ | Eier sletter ressursen, den er borte | 302, deretter 404 |
| ✅ | Admin kan slette andres ressurs | 404 |
| ✅ | Etter utlogging kreves innlogging igjen | http://localhost:8081/Account/Login?ReturnUrl=/ResourceHandler/Create |
| ✅ | Etter omstart: data er beholdt og ikke duplisert | 4 før, 4 etter |
| ✅ | Etter omstart: admin er fortsatt logget inn | 200 |

## Sikkerhet

| Resultat | Test | Detalj |
|---|---|---|
| ✅ | Header X-Content-Type-Options: nosniff | nosniff |
| ✅ | Header X-Frame-Options: DENY (clickjacking) | DENY |
| ✅ | Header Referrer-Policy | strict-origin-when-cross-origin |
| ✅ | Header Permissions-Policy | geolocation=(), camera=(), microphone=() |
| ✅ | Utlogget: forsiden sender til innlogging | http://localhost:8081/Account/Login?ReturnUrl=/ |
| ✅ | Utlogget: ressurslisten sender til innlogging | http://localhost:8081/Account/Login?ReturnUrl=/ResourceHandler |
| ✅ | Utlogget: ressurssiden sender til innlogging | http://localhost:8081/Account/Login?ReturnUrl=/Resource/Index/4 |
| ✅ | Innloggingssiden er åpen og har ikke "Fortsett uten å logge inn" | 200 |
| ✅ | Registreringssiden er åpen | 200 |
| ✅ | Logo og CSS lastes uten innlogging (innloggingssiden ser riktig ut) | 200 |
| ✅ | Utlogget: "Ny ressurs" krever innlogging | http://localhost:8081/Account/Login?ReturnUrl=/ResourceHandler/Create |
| ✅ | Utlogget: Endre krever innlogging | http://localhost:8081/Account/Login?ReturnUrl=/ResourceHandler/Edit/4 |
| ✅ | Utlogget: brukeradministrasjon krever innlogging | http://localhost:8081/Account/Login?ReturnUrl=/UserAdmin |
| ✅ | Vanlig bruker får "Ingen tilgang" på brukeradministrasjon | http://localhost:8081/Account/AccessDenied?ReturnUrl=/UserAdmin |
| ✅ | Annen bruker får "Ingen tilgang" på Endre (GET) | http://localhost:8081/Account/AccessDenied?ReturnUrl=/ResourceHandler/Edit/4 |
| ✅ | Annen bruker får "Ingen tilgang" på Slett (GET) | http://localhost:8081/Account/AccessDenied?ReturnUrl=/ResourceHandler/Delete/4 |
| ✅ | Annen bruker kan ikke endre ved å sende POST direkte | http://localhost:8081/Account/AccessDenied?ReturnUrl=/ResourceHandler/Edit/4 |
| ✅ | Annen bruker kan ikke slette ved å sende POST direkte | http://localhost:8081/Account/AccessDenied?ReturnUrl=/ResourceHandler/Delete/4 |
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
