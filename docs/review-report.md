# Quick review: things that may need fixing

Written on 2026-11-14 during the ".NET version + map scaling" change (branch `mahan/dotnet-og-skalering`).
**None of these have been fixed.** They're listed most important first so you can decide what to do about each one.

Overall the codebase is in good shape: every POST action has antiforgery protection, all SQL goes through Dapper
parameters (no SQL injection), image uploads are checked by their real file bytes, and the whole app requires
login by default. The items below are the gaps I found.

---

## 1. ✅ FIXED: "Bruk min posisjon" can never work (bug, high)

> Fixed: `Permissions-Policy` is now `geolocation=(self)`.

**Where:** `Heimevernet.Web/Program.cs`, the security-headers middleware.

The app sends `Permissions-Policy: geolocation=(), camera=(), microphone=()`. The empty `()` means
"no page is allowed to use this feature", *including this app itself*. But the map and the resource list call
`navigator.geolocation.getCurrentPosition(...)` (`wwwroot/js/site.js`, around line 55).

I checked in the browser: `document.featurePolicy.allowsFeature('geolocation')` returns `false`.
So users always get the "you have not given access to your position" message, even if they want to share it.
Distances to resources are never shown.

**Possible fix:** change it to `geolocation=(self)`. That allows only your own site, and still blocks
any embedded third-party content.

## 2. ✅ FIXED: No Content-Security-Policy header (security, medium)

> Fixed: `Heimevernet.Web/Security/ContentSecurityPolicy.cs` builds the header with a new nonce per request,
> and all 7 inline `<script>` blocks carry `nonce="..."`. Remaining weakness: `style-src` still allows
> `'unsafe-inline'` because many views use `style="..."` (category colours). Moving those into CSS classes
> would let you remove it.

**Where:** `Program.cs`, same middleware.

A Content-Security-Policy (CSP) tells the browser which places scripts, styles and images may be loaded from.
If an attacker ever manages to inject HTML (XSS, cross-site scripting), a CSP stops their script from running.
It's the most important security header that's missing.

Things to know before adding one:
- `Views/Home/Index.cshtml` and several other views have inline `<script>` blocks. A strict CSP blocks those,
  so you'd either move them into `.js` files or use a "nonce" (a random value per request).
- You'd need to allow the OpenStreetMap tile server for images and Google Fonts (see item 4).

## 3. Docker always runs in "Development" mode (security, medium)

**Where:** `docker-compose.yml` sets `ASPNETCORE_ENVIRONMENT: Development`.

In `Program.cs`, the error page (`UseExceptionHandler`), HTTPS redirection and HSTS are only turned on when the
environment is *not* Development. So in Docker, a crash shows the detailed developer error page with stack traces
and code paths. That's fine on your own machine, but if this compose file is ever used to host the app for real,
it leaks internal details.

**Possible fix:** a separate `docker-compose.override.yml` for development, or default to `Production` and only
switch to Development locally.

## 4. Google Fonts is loaded from Google's servers (privacy, low–medium)

**Where:** `Views/Shared/_Layout.cshtml` (and 3 other views) link to `fonts.googleapis.com`.

Every page view sends the visitor's IP address to Google. Under GDPR (personvernforordningen) this has been ruled
a problem in the EU when done without consent. It also means the font is missing if Google can't be reached,
which matters for a crisis app.

**Possible fix:** download Atkinson Hyperlegible (it's free, OFL licence) into `wwwroot/fonts/` and use `@font-face`.

## 5. OpenStreetMap tiles straight from openstreetmap.org (policy, low)

The map loads tiles directly from the OpenStreetMap servers. That's fine for a school project, but their tile
usage policy says heavy or production use must use another provider or your own tile server. Worth knowing if the
app is ever used for real.

## 6. CI (GitHub Actions) and the new `global.json` (build, low)

**Where:** `.github/workflows/build-test.yml` uses `actions/setup-dotnet@v4` with `global-json-file`.

`global.json` now says `rollForward: latestMajor`. setup-dotnet doesn't fully understand that setting and will
most likely install exactly SDK `10.0.100` (the very first .NET 10 SDK, which has known security fixes missing).
It still builds, so nothing breaks, but you could add `dotnet-version: 10.0.x` to the workflow to always get the
newest .NET 10 patch. Check the first CI run on this branch to see which SDK it picked.

## 7. README pins very exact tool versions (docs, low)

`README.md` says "Docker 29.4.1". Any recent Docker works, and an exact version makes readers think they must
install that specific one. Something like "Docker Desktop (or Docker Engine) with Docker Compose" is enough.

## 8. Small accessibility notes (low)

- The map container has `role="application"`. That tells screen readers to hand all keys to the page, which can
  make it hard for screen-reader users to leave the map. Leaflet already adds sensible roles itself; consider
  `role="region"` instead.
- On phones the "Nærmest deg" list is hidden completely (`.map-list { display: none; }`). That's a deliberate
  design choice in the CSS comment, but it means phone users can only find resources through the map or the
  Ressurser page. Worth confirming that's what you want.
