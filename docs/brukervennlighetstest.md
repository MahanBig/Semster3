# Brukervennlighetstest – Heimevernet beredskapskart

Mal for brukervennlighetstesten (krav 9). Fyll inn én kolonne per testperson.
Målet er å finne ut om folk uten opplæring klarer de viktigste oppgavene, og hva de synes er vanskelig.

## Slik gjennomfører dere testen

1. **Testpersoner:** 3–5 personer som ikke har vært med på å lage løsningen. Prøv å få med noen som ikke er vant med data, og noen som bruker mobil.
2. **Utstyr:** Appen kjører (`docker compose up -d`) på http://localhost:8080. Test både på PC og mobil hvis dere kan.
3. **Roller:** Én leder testen og leser opp oppgavene, én skriver notater. Ikke hjelp testpersonen – be dem tenke høyt ("si hva du ser etter og hva du tror skjer").
4. **Tid:** Ca. 15 minutter per person. Ta tiden på hver oppgave.
5. **Etterpå:** Testpersonen fyller ut spørreskjemaet (SUS) under. Spør til slutt: "Hva var vanskeligst?" og "Hva ville du forandret?"

Si dette før dere starter: *"Det er løsningen vi tester, ikke deg. Du kan ikke gjøre noe feil."*

## Oppgaver

| # | Oppgave (les opp for testpersonen) | Lykkes hvis … |
|---|---|---|
| 1 | Finn ut hvor nærmeste vannpost er. | Finner en ressurs av typen vann i listen. |
| 2 | Finn telefonnummeret til den som eier et aggregat. | Åpner ressurssiden og leser nummeret. |
| 3 | Lag en ny bruker. | Er logget inn etter registrering. |
| 4 | Legg inn en ny ressurs: "Vedovn til utlån" i ditt postnummer. | Ressursen vises i listen. |
| 5 | Du oppdaget en skrivefeil – endre beskrivelsen på ressursen du la inn. | Endringen er lagret. |
| 6 | Ressursen er ikke lenger tilgjengelig – fjern den. | Ressursen er borte fra listen. |
| 7 | Logg ut. | Er logget ut. |

## Resultater per oppgave

Skriv ✅ (klarte det uten hjelp), ⚠️ (klarte det med nøling/omveier) eller ❌ (ga opp / trengte hjelp), og tiden i sekunder.

| Oppgave | Person 1 | Person 2 | Person 3 | Person 4 | Person 5 |
|---|---|---|---|---|---|
| Bakgrunn (alder, PC/mobil, datavant?) | | | | | |
| 1 Finn vannpost | | | | | |
| 2 Finn telefonnummer | | | | | |
| 3 Lag bruker | | | | | |
| 4 Legg inn ressurs | | | | | |
| 5 Endre ressurs | | | | | |
| 6 Slett ressurs | | | | | |
| 7 Logg ut | | | | | |

## Spørreskjema (System Usability Scale, SUS)

Testpersonen krysser av 1 (helt uenig) til 5 (helt enig).

| # | Påstand | P1 | P2 | P3 | P4 | P5 |
|---|---|---|---|---|---|---|
| 1 | Jeg tror jeg vil bruke denne løsningen ofte. | | | | | |
| 2 | Løsningen var unødvendig komplisert. | | | | | |
| 3 | Løsningen var lett å bruke. | | | | | |
| 4 | Jeg tror jeg ville trengt hjelp fra en tekniker for å bruke løsningen. | | | | | |
| 5 | De ulike funksjonene hang godt sammen. | | | | | |
| 6 | Det var for mye som ikke stemte overens i løsningen. | | | | | |
| 7 | Jeg tror de fleste vil lære seg løsningen veldig raskt. | | | | | |
| 8 | Løsningen var tungvint å bruke. | | | | | |
| 9 | Jeg følte meg trygg da jeg brukte løsningen. | | | | | |
| 10 | Jeg måtte lære mye før jeg kom i gang. | | | | | |
| | **SUS-poeng (0–100)** | | | | | |

**Slik regner dere SUS-poeng:** For oddetallspåstandene (1, 3, 5, 7, 9): svar − 1. For partallspåstandene (2, 4, 6, 8, 10): 5 − svar. Legg sammen de ti tallene og gang med 2,5. Over 68 regnes som over gjennomsnittet.

## Funn og tiltak

| Funn (hva skjedde?) | Hvor mange fikk problemet? | Alvorlighet (lav/middels/høy) | Tiltak vi gjorde |
|---|---|---|---|
| | | | |
| | | | |

## Oppsummering

*Skriv 3–5 setninger: Gjennomsnittlig SUS-poeng, hvilke oppgaver gikk greit, hva var vanskelig, og hva dere endret etter testen.*
