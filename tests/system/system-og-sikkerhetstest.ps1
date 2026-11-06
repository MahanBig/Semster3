<#
  System- og sikkerhetstest for Heimevernet (krav 9).

  Tester hele løsningen slik en bruker møter den: nettleser -> webapp i Docker -> MariaDB.
  Skriptet later som det er flere nettlesere (anonym, admin, to vanlige brukere) og sjekker
  at sidene virker, at data blir lagret, og at sikkerhetstiltakene faktisk stopper det de skal.

  Slik kjører du (fra mappen Semster3, med appen startet):
      docker compose up --build -d
      pwsh tests/system/system-og-sikkerhetstest.ps1        (eller: powershell -File ...)

  Resultatet skrives til skjermen og til tests/system/resultat.md.
#>
param(
    [string]$BaseUrl = 'http://localhost:8080',
    [switch]$SkipRestart  # Hopp over testen som restarter web-containeren.
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http # Lastes ikke automatisk i Windows PowerShell.
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$results = [System.Collections.Generic.List[object]]::new()
$group = 'System'

function Check([string]$name, [bool]$ok, [string]$detail = '') {
    $results.Add([pscustomobject]@{ Type = $group; Ok = $(if ($ok) { 'PASS' } else { 'FAIL' }); Test = $name; Detail = $detail })
}
function New-Client {
    # HttpClient som husker cookies (som en nettleser), men ikke følger redirects, så vi kan sjekke dem.
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $handler.UseCookies = $true
    $handler.CookieContainer = [System.Net.CookieContainer]::new()
    [System.Net.Http.HttpClient]::new($handler)
}
function Req([string]$path, $client, [string]$method = 'Get', $body = $null) {
    if ($method -eq 'Post') {
        $form = [System.Collections.Generic.Dictionary[string, string]]::new()
        foreach ($key in $body.Keys) { $form[$key] = [string]$body[$key] }
        $resp = $client.PostAsync("$BaseUrl$path", [System.Net.Http.FormUrlEncodedContent]::new($form)).GetAwaiter().GetResult()
    } else {
        $resp = $client.GetAsync("$BaseUrl$path").GetAwaiter().GetResult()
    }
    ToResult $resp
}
# Skjema med fil (multipart/form-data), som når nettleseren sender et bilde.
function ReqFile([string]$path, $client, $fields, [byte[]]$file, [string]$fileName, [string]$contentType) {
    $content = [System.Net.Http.MultipartFormDataContent]::new()
    foreach ($key in $fields.Keys) { $content.Add([System.Net.Http.StringContent]::new([string]$fields[$key]), $key) }
    $part = [System.Net.Http.ByteArrayContent]::new($file)
    $part.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new($contentType)
    $content.Add($part, 'Image', $fileName)
    ToResult ($client.PostAsync("$BaseUrl$path", $content).GetAwaiter().GetResult())
}
function ToResult($resp) {
    $headers = @{}
    foreach ($h in $resp.Headers) { $headers[$h.Key] = ($h.Value -join ', ') }
    foreach ($h in $resp.Content.Headers) { $headers[$h.Key] = ($h.Value -join ', ') }
    [pscustomobject]@{
        StatusCode = [int]$resp.StatusCode
        Content    = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Location   = "$($resp.Headers.Location)"
        Headers    = $headers
    }
}
function Token($response) {
    if ($response.Content -match '__RequestVerificationToken"[^>]*value="([^"]+)"') { return $Matches[1] }
    throw "Fant ikke anti-forgery-token"
}
function Loc($r) { $r.Location }
function Register($client, [string]$email, [string]$password) {
    $r = Req '/Account/Register' $client
    Req '/Account/Register' $client 'Post' @{ Name = 'Systemtest'; Email = $email; Password = $password; ConfirmPassword = $password; Consent = 'true'; __RequestVerificationToken = (Token $r) }
}
function Login($client, [string]$email, [string]$password) {
    $r = Req '/Account/Login' $client
    Req '/Account/Login' $client 'Post' @{ Email = $email; Password = $password; RememberMe = 'false'; __RequestVerificationToken = (Token $r) }
}
function Denied($r) { $r.StatusCode -eq 302 -and (Loc $r) -match '/Account/AccessDenied' }
function NeedsLogin($r) { $r.StatusCode -eq 302 -and (Loc $r) -match '/Account/Login' }

$envFile = Join-Path $repoRoot '.env'
$adminEmail = 'admin@heimevernet.local'
$adminPassword = ((Get-Content $envFile | Where-Object { $_ -match '^ADMIN_PASSWORD=' }) -split '=', 2)[1]
$run = Get-Random -Maximum 99999
$password = 'Test1234!x'
$ola  = "systemtest-ola-$run@example.com"
$kari = "systemtest-kari-$run@example.com"
$anon = New-Client; $admin = New-Client; $olaC = New-Client; $kariC = New-Client

try {
    # ======================= SYSTEMTEST =======================
    $group = 'System'
    $r = Register $olaC $ola $password; Check 'Ny bruker kan registrere seg' ($r.StatusCode -eq 302) "$($r.StatusCode)"
    $r = Req '/' $olaC; Check 'Ny bruker er logget inn, og menyen viser navnet (ikke e-posten)' ($r.Content -match 'Logget inn som </span>Systemtest</strong>' -and $r.Content -match ('title="Innstillinger for ' + [regex]::Escape($ola) + '"')) ''
    $r = Register $kariC $kari $password; Check 'Bruker nr. 2 kan registrere seg' ($r.StatusCode -eq 302) "$($r.StatusCode)"
    $per = "systemtest-per-$run@example.com"; $perC = New-Client
    $r = Register $perC $per $password; Check 'Bruker nr. 3 kan registrere seg' ($r.StatusCode -eq 302) "$($r.StatusCode)"

    # Roller: alle nye brukere er Privat. Admin gjør Kari til Offentlig på admin-siden.
    $r = Login $admin $adminEmail $adminPassword; Check 'Admin logger inn' ($r.StatusCode -eq 302) "$($r.StatusCode)"
    $r = Req '/UserAdmin' $admin
    $kariRow = '(?:(?!</tr>)[\s\S])*?'  # Resten av raden til Kari, ikke lenger.
    $kariId = if ($r.Content -match ([regex]::Escape($kari) + $kariRow + 'name="userId" value="([^"]+)"')) { $Matches[1] } else { '' }
    Check 'Admin-siden viser nye brukere med rollen Privat' ($r.StatusCode -eq 200 -and $r.Content -match ([regex]::Escape($kari) + $kariRow + '<option value="Privat" selected="selected">')) ''
    $r2 = Req '/UserAdmin/SetRole' $admin 'Post' @{ userId = $kariId; role = 'Offentlig'; __RequestVerificationToken = (Token $r) }
    $r = Req '/UserAdmin' $admin
    Check 'Admin gir Kari rollen Offentlig' ($r2.StatusCode -eq 302 -and $r.Content -match ([regex]::Escape($kari) + $kariRow + '<option value="Offentlig" selected="selected">')) ''
    $kariC = New-Client; Login $kariC $kari $password | Out-Null  # Ny innlogging med den nye rollen.
    $r = Req '/' $olaC; Check 'Forsiden (kart) åpnes for innlogget bruker' ($r.StatusCode -eq 200 -and $r.Content -match 'id="map"') "$($r.StatusCode)"
    Check 'Forsiden har pluss-knappen for å sette en pinne og registrere en ressurs' ($r.Content -match 'id="add-pin"' -and $r.Content -match 'data-create-url="/ResourceHandler/Create"') ''
    $r = Req '/ResourceHandler' $kariC
    $ids = [regex]::Matches($r.Content, 'href="/Resource/Index/(\d+)"') | ForEach-Object { $_.Groups[1].Value }
    Check 'Offentlig ser alle ressursene fra databasen' ($r.StatusCode -eq 200 -and $ids.Count -ge 3 -and $r.Content -notmatch 'Mine ressurser') "$($ids.Count) ressurser"
    $r = Req "/Resource/Index/$($ids[0])" $kariC; Check 'Ressurssiden åpnes' ($r.StatusCode -eq 200) "$($r.StatusCode)"
    Check 'Ressurs uten eier: Offentlig ser ikke Endre/Slett' ($r.Content -notmatch '/ResourceHandler/Edit/') ''
    $r = Req '/ResourceHandler' $olaC
    Check 'Privat ser bare sine egne ressurser (ingen ennå)' ($r.Content -match 'Mine ressurser' -and ([regex]::Matches($r.Content, 'href="/Resource/Index/\d+"')).Count -eq 0) ''
    $r = Req '/' $olaC; Check 'Privat ser ingen andres ressurser på kartet' (([regex]::Matches($r.Content, 'data-filter-item')).Count -eq 0) ''
    $r = Req "/Resource/Index/$($ids[0])" $olaC; Check 'Privat får 404 på en ressurs hen ikke eier' ($r.StatusCode -eq 404) "$($r.StatusCode)"
    $r = Req '/Resource/Index/999999' $olaC; Check 'Ukjent ressurs gir 404' ($r.StatusCode -eq 404) "$($r.StatusCode)"


    # Krav 3: legge til, endre, slette
    $name = "Systemtest aggregat $run"
    $r = Req '/ResourceHandler' $olaC; Check 'Innlogget bruker ser "Ny ressurs"' ($r.Content -match 'href="/ResourceHandler/Create"') ''
    $r = Req '/ResourceHandler/Create' $olaC; Check 'Innlogget bruker kan åpne "Ny ressurs"' ($r.StatusCode -eq 200) "$($r.StatusCode)"
    $token = Token $r
    $r2 = Req '/ResourceHandler/Create' $olaC 'Post' @{ Name = ''; Description = 'x'; Category = 'Verktøy'; __RequestVerificationToken = $token }
    Check 'Ressurs uten navn blir avvist med feilmelding' ($r2.StatusCode -eq 200 -and $r2.Content -match 'field-validation-error') "$($r2.StatusCode)"
    $r2 = Req '/ResourceHandler/Create' $olaC 'Post' @{ Name = 'x'; Description = 'x'; Category = 'Verktøy'; ContactEmail = 'ikke-en-epost'; __RequestVerificationToken = $token }
    Check 'Ugyldig e-post i kontaktinfo blir avvist' ($r2.StatusCode -eq 200 -and $r2.Content -match 'Ugyldig e-postadresse') "$($r2.StatusCode)"
    $r2 = Req '/ResourceHandler/Create' $olaC 'Post' @{ Name = $name; Description = '5 kW, diesel'; Category = 'Strøm og lading'; Address = 'Storgata 1'; PostalCode = '4790'; PostalPlace = 'Lillesand'; Latitude = '58.249500'; Longitude = '8.377800'; ContactName = 'Vaktmester Per'; ContactPhone = '40000000'; ContactEmail = 'vakt@example.no'; __RequestVerificationToken = $token }
    Check 'Bruker legger til ressurs' ($r2.StatusCode -eq 302 -and (Loc $r2) -match '/Resource/Index/(\d+)') "$(Loc $r2)"
    $newId = if ((Loc $r2) -match '/Resource/Index/(\d+)') { $Matches[1] } else { 0 }
    $r = Req "/Resource/Index/$newId" $olaC
    Check 'Ressursen lagres med alle felt og eier' ($r.Content -match [regex]::Escape($name) -and $r.Content -match 'Storgata 1, 4790 Lillesand' -and $r.Content -match 'Lagt inn av Systemtest') ''
    Check 'Punktet fra kartet lagres og vises på ressurssiden' ($r.Content -match 'id="detail-map"' -and $r.Content -match 'data-lat="58.2495"' -and $r.Content -match 'mlat=58.2495&amp;mlon=8.3778') ''
    Check 'Kontaktinfoen på ressursen vises med ring- og e-postknapp' ($r.Content -match 'Vaktmester Per' -and $r.Content -match 'href="tel:40000000"' -and $r.Content -match 'href="mailto:vakt@example.no"') ''
    Check 'Eier ser Endre og Slett' ($r.Content -match "/ResourceHandler/Edit/$newId" -and $r.Content -match "/ResourceHandler/Delete/$newId") ''
    $r = Req '/ResourceHandler' $olaC
    Check 'Ressurslisten har punktet, så avstand og kartmarkør kan vises' ($r.Content -match ('data-lat="58.2495"[^>]*data-lng="8.3778"')) ''
    Check 'Ressurslisten har filtre for avstand, kategori, sted og sortering' ($r.Content -match 'data-filter-distance="5000"' -and $r.Content -match 'data-filter-chip' -and $r.Content -match '<option value="Lillesand">' -and $r.Content -match 'data-filter-sort') ''
    $r = Req '/ResourceHandler/Create?lat=58.1464&lng=7.9959' $olaC
    Check '"Legg til ressurs her" fra kartet fyller inn punktet i skjemaet' ($r.StatusCode -eq 200 -and $r.Content -match 'name="Latitude"[^>]*value="58.1464"' -and $r.Content -match 'name="Longitude"[^>]*value="7.9959"') ''

    # Bilde og kategorien Annet
    $png = [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==')
    $r = Req '/ResourceHandler/Create' $olaC
    $r2 = ReqFile '/ResourceHandler/Create' $olaC @{ Name = "Bildetest $run"; Description = 'Med bilde'; Category = 'Annet'; __RequestVerificationToken = (Token $r) } $png 'bilde.png' 'image/png'
    $imgId = if ((Loc $r2) -match '/Resource/Index/(\d+)') { $Matches[1] } else { 0 }
    $r = Req "/Resource/Index/$imgId" $olaC
    Check 'Ressurs med bilde og kategorien Annet lagres' ($r2.StatusCode -eq 302 -and $r.Content -match "/Resource/Image/$imgId" -and $r.Content -match 'Annet') "$(Loc $r2)"
    $r = Req "/Resource/Image/$imgId" $olaC; Check 'Eier får bildet (image/png)' ($r.StatusCode -eq 200 -and $r.Headers['Content-Type'] -eq 'image/png') "$($r.StatusCode) $($r.Headers['Content-Type'])"
    $r = Req "/Resource/Image/$imgId" $kariC; Check 'Offentlig får bildet' ($r.StatusCode -eq 200) "$($r.StatusCode)"
    $r = Req '/ResourceHandler' $olaC
    Check 'Privat ser sine egne ressurser i listen' ($r.Content -match [regex]::Escape($name) -and $r.Content -match "Bildetest $run") 

    $r = Req "/ResourceHandler/Edit/$newId" $olaC; Check 'Eier kan åpne Endre' ($r.StatusCode -eq 200 -and $r.Content -match [regex]::Escape($name)) "$($r.StatusCode)"
    $r2 = Req "/ResourceHandler/Edit/$newId" $olaC 'Post' @{ Name = "$name (10 kW)"; Description = '10 kW, diesel'; Category = 'Strøm og lading'; PostalPlace = 'Grimstad'; PostalCode = '4876'; __RequestVerificationToken = (Token $r) }
    $r = Req "/Resource/Index/$newId" $olaC
    Check 'Eier endrer ressursen, endringen lagres' ($r2.StatusCode -eq 302 -and $r.Content -match [regex]::Escape("$name (10 kW)") -and $r.Content -match 'Grimstad' -and $r.Content -match 'Endringene ble lagret') ''

    $r = Req "/Resource/Index/$newId" $kariC; Check 'Annen bruker ser ikke Endre/Slett' ($r.Content -notmatch "/ResourceHandler/Edit/$newId") ''

    $r = Req "/ResourceHandler/Edit/$newId" $admin; Check 'Admin kan åpne Endre på andres ressurs' ($r.StatusCode -eq 200) "$($r.StatusCode)"
    $r = Req '/UserAdmin' $admin; Check 'Admin-siden viser brukere, ressurser og behov' ($r.StatusCode -eq 200 -and $r.Content -match [regex]::Escape($ola) -and $r.Content -match 'id="ressurser"' -and $r.Content -match 'id="behov"' -and $r.Content -match [regex]::Escape($name)) "$($r.StatusCode)"

    # ======================= SIKKERHETSTEST =======================
    $group = 'Sikkerhet'
    $r = Req '/Account/Login' $anon
    Check 'Header X-Content-Type-Options: nosniff' ($r.Headers['X-Content-Type-Options'] -eq 'nosniff') "$($r.Headers['X-Content-Type-Options'])"
    Check 'Header X-Frame-Options: DENY (clickjacking)' ($r.Headers['X-Frame-Options'] -eq 'DENY') "$($r.Headers['X-Frame-Options'])"
    Check 'Header Referrer-Policy' ($r.Headers['Referrer-Policy'] -eq 'strict-origin-when-cross-origin') "$($r.Headers['Referrer-Policy'])"
    Check 'Header Permissions-Policy' ($r.Headers['Permissions-Policy'] -match 'camera=\(\)') "$($r.Headers['Permissions-Policy'])"

    # Tilgangskontroll
    $r = Req '/' $anon; Check 'Utlogget: forsiden sender til innlogging' (NeedsLogin $r) (Loc $r)
    $r = Req '/ResourceHandler' $anon; Check 'Utlogget: ressurslisten sender til innlogging' (NeedsLogin $r) (Loc $r)
    $r = Req "/Resource/Index/$newId" $anon; Check 'Utlogget: ressurssiden sender til innlogging' (NeedsLogin $r) (Loc $r)
    $r = Req '/Account/Login' $anon; Check 'Innloggingssiden er åpen og har ikke "Fortsett uten å logge inn"' ($r.StatusCode -eq 200 -and $r.Content -notmatch 'Fortsett uten') "$($r.StatusCode)"
    $r = Req '/Account/Register' $anon; Check 'Registreringssiden er åpen' ($r.StatusCode -eq 200) "$($r.StatusCode)"
    $r = Req '/img/kriseklar.svg' $anon; Check 'Logo og CSS lastes uten innlogging (innloggingssiden ser riktig ut)' ($r.StatusCode -eq 200 -and (Req '/css/site.css' $anon).StatusCode -eq 200) "$($r.StatusCode)"
    $r = Req '/ResourceHandler/Create' $anon; Check 'Utlogget: "Ny ressurs" krever innlogging' (NeedsLogin $r) (Loc $r)
    $r = Req "/ResourceHandler/Edit/$newId" $anon; Check 'Utlogget: Endre krever innlogging' (NeedsLogin $r) (Loc $r)
    $r = Req '/UserAdmin' $anon; Check 'Utlogget: brukeradministrasjon krever innlogging' (NeedsLogin $r) (Loc $r)
    $r = Req '/UserAdmin' $kariC; Check 'Vanlig bruker får "Ingen tilgang" på brukeradministrasjon' (Denied $r) (Loc $r)
    $r = Req "/ResourceHandler/Edit/$newId" $kariC; Check 'Annen bruker får "Ingen tilgang" på Endre (GET)' (Denied $r) (Loc $r)
    $r = Req "/ResourceHandler/Delete/$newId" $kariC; Check 'Annen bruker får "Ingen tilgang" på Slett (GET)' (Denied $r) (Loc $r)
    $kariToken = Token (Req '/ResourceHandler/Create' $kariC)
    $r = Req "/ResourceHandler/Edit/$newId" $kariC 'Post' @{ Name = 'Hacket'; Description = 'x'; Category = 'Verktøy'; __RequestVerificationToken = $kariToken }
    Check 'Annen bruker kan ikke endre ved å sende POST direkte' (Denied $r) (Loc $r)
    $r = Req "/ResourceHandler/Delete/$newId" $kariC 'Post' @{ __RequestVerificationToken = $kariToken }
    Check 'Annen bruker kan ikke slette ved å sende POST direkte' (Denied $r) (Loc $r)
    $r = Req "/Resource/Index/$newId" $kariC; Check 'Ressursen er uendret etter angrepene' ($r.StatusCode -eq 200 -and $r.Content -notmatch 'Hacket') ''

    # CSRF: POST uten anti-forgery-token skal stoppes
    $r = Req "/ResourceHandler/Delete/$newId" $olaC 'Post' @{}; Check 'CSRF: Slett uten token blir blokkert (400)' ($r.StatusCode -eq 400) "$($r.StatusCode)"
    $r = Req '/ResourceHandler/Create' $olaC 'Post' @{ Name = 'x'; Description = 'x'; Category = 'Verktøy' }; Check 'CSRF: Ny ressurs uten token blir blokkert (400)' ($r.StatusCode -eq 400) "$($r.StatusCode)"
    $r = Req "/Resource/Index/$newId" $perC; Check 'Privat får 404 på en annens ressurs (avslører ikke at den finnes)' ($r.StatusCode -eq 404) "$($r.StatusCode)"
    $r = Req "/Resource/Image/$imgId" $perC; Check 'Privat får ikke bildet til en annens ressurs' ($r.StatusCode -eq 404) "$($r.StatusCode)"
    $r = Req '/ResourceHandler/Create' $olaC
    $r2 = ReqFile '/ResourceHandler/Create' $olaC @{ Name = "Falskt bilde $run"; Description = 'x'; Category = 'Annet'; __RequestVerificationToken = (Token $r) } ([Text.Encoding]::UTF8.GetBytes('<script>alert(1)</script>')) 'bilde.png' 'image/png'
    Check 'Opplasting: en fil som later som den er PNG, blir avvist' ($r2.StatusCode -eq 200 -and $r2.Content -match 'JPEG, PNG eller WebP') "$($r2.StatusCode)"
    $r = Req '/UserAdmin/SetRole' $admin 'Post' @{ userId = $kariId; role = 'Admin' }; Check 'CSRF: Endre rolle uten token blir blokkert (400)' ($r.StatusCode -eq 400) "$($r.StatusCode)"
    $r = Req '/Settings' $perC
    $r = Req '/Settings/Profile' $perC 'Post' @{ 'Profile.Name' = 'x'; 'Profile.Email' = $per }; Check 'CSRF: Endre profil uten token blir blokkert (400)' ($r.StatusCode -eq 400) "$($r.StatusCode)"

    # XSS: HTML/script i input skal vises som tekst, ikke kjøres
    $xss = "<script>alert('xss$run')</script>"
    $r = Req '/ResourceHandler/Create' $olaC
    $r2 = Req '/ResourceHandler/Create' $olaC 'Post' @{ Name = "XSS-test $run"; Description = $xss; Category = 'Verktøy'; __RequestVerificationToken = (Token $r) }
    $xssId = if ((Loc $r2) -match '/Resource/Index/(\d+)') { $Matches[1] } else { 0 }
    $r = Req "/Resource/Index/$xssId" $kariC
    Check 'XSS: script i beskrivelse blir vist som tekst (HTML-kodet)' ($r.Content -notmatch [regex]::Escape($xss) -and $r.Content -match '&lt;script&gt;') ''

    # SQL-injeksjon: skal behandles som vanlig tekst
    $sqli = "x'); DROP TABLE Resources; --"
    $r = Req '/ResourceHandler/Create' $olaC
    $r2 = Req '/ResourceHandler/Create' $olaC 'Post' @{ Name = "SQLi-test $run"; Description = $sqli; Category = 'Verktøy'; __RequestVerificationToken = (Token $r) }
    $r = Req '/ResourceHandler' $kariC
    Check 'SQL-injeksjon: teksten lagres som tekst, tabellen finnes fortsatt' ($r2.StatusCode -eq 302 -and $r.StatusCode -eq 200 -and $r.Content -match "SQLi-test $run") ''

    # Åpen redirect: ReturnUrl til et annet nettsted skal ignoreres
    $evil = New-Client
    $r = Req '/Account/Login' $evil
    $r = Req '/Account/Login' $evil 'Post' @{ Email = $kari; Password = $password; RememberMe = 'false'; ReturnUrl = 'https://evil.example.com'; __RequestVerificationToken = (Token $r) }
    Check 'Åpen redirect: ReturnUrl til annet nettsted blir ignorert' ($r.StatusCode -eq 302 -and (Loc $r) -notmatch 'evil') (Loc $r)

    # Passord og innlogging
    $r = Register (New-Client) "svak-$run@example.com" 'weakpass'; Check 'Svakt passord blir avvist' ($r.StatusCode -eq 200 -and $r.Content -match 'text-danger') "$($r.StatusCode)"
    $r = Register (New-Client) $ola $password; Check 'Samme e-post kan ikke registreres to ganger' ($r.StatusCode -eq 200) "$($r.StatusCode)"
    $r = Login (New-Client) $kari 'FeilPassord1!'; Check 'Feil passord gir samme melding som ukjent e-post' ($r.Content -match 'Feil e-post eller passord') ''
    $r = Login (New-Client) "finnes-ikke-$run@example.com" 'FeilPassord1!'; Check 'Ukjent e-post gir samme melding (avslører ikke brukere)' ($r.Content -match 'Feil e-post eller passord') ''
    $lockUser = "systemtest-lock-$run@example.com"
    Register (New-Client) $lockUser $password | Out-Null
    for ($i = 0; $i -lt 5; $i++) { Login (New-Client) $lockUser 'FeilPassord1!' | Out-Null }
    $r = Login (New-Client) $lockUser $password
    Check 'Kontoen låses etter 5 feil passord (også riktig passord avvises)' ($r.StatusCode -eq 200 -and $r.Content -match 'Kontoen er midlertidig') "$($r.StatusCode)"
    $r = Login (New-Client) $kari $password
    $setCookie = "$($r.Headers['Set-Cookie'])"
    Check 'Innloggings-cookien er HttpOnly (JavaScript kan ikke lese den)' ($setCookie -match '\.AspNetCore\.Identity\.Application=[^,]*httponly') ''

    # Eier sletter til slutt (systemtest av slett)
    $group = 'System'
    $r = Req "/ResourceHandler/Delete/$newId" $olaC; Check 'Eier får bekreftelsesside før sletting' ($r.StatusCode -eq 200 -and $r.Content -match 'Slette ressursen') "$($r.StatusCode)"
    $r2 = Req "/ResourceHandler/Delete/$newId" $olaC 'Post' @{ __RequestVerificationToken = (Token $r) }
    $r = Req "/Resource/Index/$newId" $kariC
    Check 'Eier sletter ressursen, den er borte' ($r2.StatusCode -eq 302 -and $r.StatusCode -eq 404) "$($r2.StatusCode), deretter $($r.StatusCode)"
    foreach ($id in @($xssId)) {
        $r = Req "/ResourceHandler/Delete/$id" $admin
        Req "/ResourceHandler/Delete/$id" $admin 'Post' @{ __RequestVerificationToken = (Token $r) } | Out-Null
    }
    $r = Req "/Resource/Index/$xssId" $kariC; Check 'Admin kan slette andres ressurs' ($r.StatusCode -eq 404) "$($r.StatusCode)"

    # Innstillinger: endre navn og passord
    $r = Req '/Settings' $perC; Check 'Innstillinger viser info om brukeren' ($r.StatusCode -eq 200 -and $r.Content -match [regex]::Escape($per) -and $r.Content -match 'Privat') "$($r.StatusCode)"
    $r2 = Req '/Settings/Profile' $perC 'Post' @{ 'Profile.Name' = "Per Endret $run"; 'Profile.Email' = $per; 'Profile.PhoneNumber' = '98765432'; __RequestVerificationToken = (Token $r) }
    $r = Req '/Settings' $perC; Check 'Bruker endrer navn og telefon i innstillingene' ($r2.StatusCode -eq 302 -and $r.Content -match "Per Endret $run" -and $r.Content -match '98765432') ''
    Check 'Det nye navnet vises i menyen med en gang' ($r.Content -match "Logget inn som </span>Per Endret $run</strong>") ''
    $r2 = Req '/Settings/Password' $perC 'Post' @{ 'Password.CurrentPassword' = 'FeilPassord1!'; 'Password.NewPassword' = 'NyttPass1!x'; 'Password.ConfirmPassword' = 'NyttPass1!x'; __RequestVerificationToken = (Token $r) }
    Check 'Bytte passord med feil nåværende passord blir avvist' ($r2.StatusCode -eq 200 -and $r2.Content -match 'Feil passord') "$($r2.StatusCode)"
    $r = Req '/Settings' $perC
    $r2 = Req '/Settings/Password' $perC 'Post' @{ 'Password.CurrentPassword' = $password; 'Password.NewPassword' = 'NyttPass1!x'; 'Password.ConfirmPassword' = 'NyttPass1!x'; __RequestVerificationToken = (Token $r) }
    $rOld = Login (New-Client) $per $password; $rNew = Login (New-Client) $per 'NyttPass1!x'
    Check 'Bruker bytter passord: det nye virker, det gamle ikke' ($r2.StatusCode -eq 302 -and $rOld.StatusCode -eq 200 -and $rNew.StatusCode -eq 302) "$($r2.StatusCode) $($rOld.StatusCode) $($rNew.StatusCode)"
    $r = Req '/Settings/Download' $perC; Check 'Bruker kan laste ned sine data' ($r.StatusCode -eq 200 -and $r.Content -match [regex]::Escape($per)) "$($r.StatusCode)"

    # Admin sletter en bruker
    $r = Req '/UserAdmin' $admin
    $perId = if ($r.Content -match ([regex]::Escape($per) + '(?:(?!</tr>)[\s\S])*?name="userId" value="([^"]+)"')) { $Matches[1] } else { '' }
    $r2 = Req '/UserAdmin/DeleteUser' $admin 'Post' @{ userId = $perId; __RequestVerificationToken = (Token $r) }
    $r = Login (New-Client) $per 'NyttPass1!x'
    Check 'Admin sletter en bruker, og hen kan ikke logge inn lenger' ($r2.StatusCode -eq 302 -and $r.StatusCode -eq 200 -and $r.Content -match 'Feil e-post eller passord') "$($r2.StatusCode) $($r.StatusCode)"

    $r = Req '/' $olaC
    $r = Req '/Account/Logout' $olaC 'Post' @{ __RequestVerificationToken = (Token $r) }
    $r = Req '/ResourceHandler/Create' $olaC; Check 'Etter utlogging kreves innlogging igjen' (NeedsLogin $r) (Loc $r)

    if (-not $SkipRestart) {
        $countBefore = ([regex]::Matches((Req '/ResourceHandler' $kariC).Content, 'href="/Resource/Index/(\d+)"')).Count
        # Docker skriver fremdrift til stderr. Med 'Stop' ville Windows PowerShell tolket det som en feil.
        Push-Location $repoRoot; & { $ErrorActionPreference = 'Continue'; docker compose restart heimevernet-web *> $null }; Pop-Location
        $ready = $false
        for ($i = 0; $i -lt 30 -and -not $ready; $i++) { Start-Sleep 2; try { $ready = (Req '/' $kariC).StatusCode -eq 200 } catch { } }
        $countAfter = ([regex]::Matches((Req '/ResourceHandler' $kariC).Content, 'href="/Resource/Index/(\d+)"')).Count
        Check 'Etter omstart: data er beholdt og ikke duplisert' ($ready -and $countAfter -eq $countBefore) "$countBefore før, $countAfter etter"
        $r = Req '/UserAdmin' $admin; Check 'Etter omstart: admin er fortsatt logget inn' ($r.StatusCode -eq 200) "$($r.StatusCode)"
    }
}
catch {
    Check 'Skriptfeil' $false $_.Exception.Message
}

$results | Format-Table Type, Ok, Test, Detail -AutoSize -Wrap | Out-String -Width 220
$passed = ($results | Where-Object Ok -eq 'PASS').Count
"BESTÅTT: $passed / $($results.Count)"

# Markdown-rapport som kan limes inn i README.
$md = [System.Text.StringBuilder]::new()
[void]$md.AppendLine("# Resultat: system- og sikkerhetstest")
[void]$md.AppendLine("")
[void]$md.AppendLine("Kjørt $(Get-Date -Format 'yyyy-MM-dd HH:mm') mot $BaseUrl. **$passed av $($results.Count) bestått.**")
foreach ($g in 'System', 'Sikkerhet') {
    [void]$md.AppendLine(""); [void]$md.AppendLine("## $g"); [void]$md.AppendLine("")
    [void]$md.AppendLine("| Resultat | Test | Detalj |"); [void]$md.AppendLine("|---|---|---|")
    foreach ($x in $results | Where-Object Type -eq $g) {
        $icon = if ($x.Ok -eq 'PASS') { '✅' } else { '❌' }
        [void]$md.AppendLine("| $icon | $($x.Test) | $(($x.Detail -replace '\|', '/')) |")
    }
}
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'resultat.md'), $md.ToString(), [System.Text.UTF8Encoding]::new($false))
if ($passed -ne $results.Count) { exit 1 }
