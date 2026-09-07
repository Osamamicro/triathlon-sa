# Visual/functional QA — Week 1 public shell port (Task 1.4)

**Date:** 2026-09-07
**Scope:** ported ASP.NET Core Razor Pages public shell, published snapshot of commit d71215e
("Public layout ported from prototype" + fix round). Home page (`/en`, `/ar`) is the only
built route; full matrix en/ar × dark/light × 390×844/1024×768/1440×900 = 12 screenshots, all
read, plus targeted a11y-tree, network, console, tab-order and side-by-side prototype checks.
**Server:** snapshot directory
`<scratchpad>/qa-snapshot`, started with
`DOTNET_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 dotnet .\Triathlon.Web.dll`
(Postgres container `stf-pg` already running on 5432). Stopped at end of run.
**Driver:** `playwright-cli -s=qa` (and `-s=proto` for the prototype side-by-side), session cwd
`<scratchpad>/qa`, screenshots under `<scratchpad>/qa/week1/en/`, `week1/ar/`, crops/extra
evidence under `week1/evidence/`.

## Findings

| ID | Sev | Page | Locale/Theme/VP | What | Evidence | Suggested fix |
|---|---|---|---|---|---|---|
| N1 | P2 | `/en`, `/ar` (all pages sharing the mobile nav) | en+ar / dark+light / 390×844 | Opening the mobile burger menu shows the hero heading/kicker text bleeding through behind the nav panel items even though `.main-nav.open` computes `background-color: rgba(255,255,255,0.97)` (should be near-opaque). Confirmed this is a **carry-over from the static prototype** — side-by-side with `index.html` at 390×844 (`playwright-cli -s=proto`) shows the identical bleed-through pattern, so this is not a Week 1 regression, but it is visible in the ported app too and worth fixing once (e.g. a real backdrop or solid computed background, or a `backdrop-filter`/stacking-context bug prevents the declared color from fully painting). | `week1/evidence/mobile-menu-en-open.png`, cropped `week1/evidence/mobile-menu-en-open-crop.png`; prototype comparison screenshot `.playwright-cli/page-2026-09-06T23-52-05-141Z.png` (session `proto`) shows the same artifact on `index.html`. | Investigate why a declared 97%-opaque white background still lets underlying text show through this strongly (check stacking context / any blend-mode / whether the panel's actual painted rect is smaller than its hit area) and raise opacity to fully solid, or add a scrim behind `.main-nav.open`. Track as a design-system fix, not a Week-1-port defect. |
| N2 | P2 | `/en`, `/ar` (footer) | en+ar / all themes / all VP | Footer heading level skips from `h2` (page sections) straight to `h4` for the three footer columns ("Compete"/"Get involved"/"Contact" and Arabic equivalents) — no `h3` in between, so the heading outline is non-sequential. | `eval` dump of all headings on `/en`: sequence ends `H2: Ready for your first start line? … H4: Compete … H4: Get involved … H4: Contact` (no intervening H3). Same structure confirmed on `/ar` via a11y snapshot. | Change footer column headings from `h4` to `h3` (or add a wrapping `h3`/`section` before them) to keep the heading hierarchy sequential for screen-reader users. |
| N3 | P2 | all pages | any | `GET /favicon.ico` → **404**. No favicon request appears in the Playwright network capture (browser didn't request it during automated navigation), confirmed directly with `curl -s -o /dev/null -w "%{http_code}" http://localhost:5080/favicon.ico` → `404`. | Direct curl output: `404`. | Add a `favicon.ico` (or `<link rel="icon">` pointing at an existing asset) to `wwwroot`. |
| N4 | Info (not scored) | `/en/events`, `/en/join`, `/en/training`, `/en/rules`, `/en/governance`, `/en/statistics`, `/ar/events`, `/en/events/riyadh-sprint-2026`, `/en/register` | — | All not-yet-built routes return `404` as expected for Week 1 (confirmed once via `curl`, not filed per-link). The 404 page itself is the **generic ASP.NET/MudBlazor scaffold page** ("Not found" / "There is nothing at this address." / "Back to the dashboard") with no site header, footer, or branding — it doesn't match the public site design at all and the "Back to the dashboard" link is a dashboard-scaffold leftover, not a public-site affordance. Not filed as a defect since a custom public 404 is presumably later scope, but flagging so it isn't forgotten before UAT. | `week1/evidence/` screenshot of `/en/events` (plain white MudBlazor-style card). | Add a branded public 404 Razor Page before UAT/launch. |

No other layout, RTL, contrast, console, or network defects found across the 12-screenshot matrix.

## Baseline defects — status

**F1 (P0, header language-toggle label wrong) — FIXED.**
Confirmed via a11y snapshot on both locales:
- `/en`: `link "التبديل إلى العربية" [ref=e22]` with visible text "العربية", `href="/ar"`.
- `/ar`: `link "Switch to English" [ref=e14]` with visible text "English", `href="/en"`.
- `html[lang]`/`dir` verified by `eval`: `/en` → `{lang:"en", dir:"ltr"}`; `/ar` → `{lang:"ar", dir:"rtl"}`.
This is now server-rendered per-locale markup (no `localStorage` involved, no client-side
re-render race). Toggle correctly always offers the *other* language, in both directions.

**F2 (P1, 1024×768 header overflow) — FIXED.**
Measured `{scrollWidth, clientWidth}` via `eval` at 1024×768 on both locales:
- `/en`: `{scrollWidth: 1024, clientWidth: 1024}` — no overflow.
- `/ar`: `{scrollWidth: 1024, clientWidth: 1024}` — no overflow.
Screenshots `week1/en/home-en-dark-1024.png` and `week1/ar/home-ar-dark-1024.png` (and their light-theme
counterparts) show the full nav, theme toggle, and language-toggle pill fully visible and unclipped
in both directions — the previous 54 px overflow and off-screen/clipped toggle in Arabic is gone.

**F3 (P2, Arabic letter-spacing / proto banner) — FIXED.**
- `eval` on `.hero-kicker`: `getComputedStyle(el).letterSpacing === "normal"` (was `.05em`).
- `document.querySelector('.proto-note')` → `null` (banner element removed entirely from the
  ported site, confirmed on `/en` and `/ar`).
Visual crop `week1/evidence/hero-kicker-ar-crop.png` shows continuous Arabic glyph joining with
no CSS-introduced letter gaps (the very slight visual gaps in the crop are the Tajawal font's own
letterforms, not a spacing property — confirmed by the computed-style check above).

## Pass/fail per flow

| Flow | Result |
|---|---|
| `/` → `/en` redirect (302, default) | Pass |
| `/` → `/ar` redirect with `Accept-Language: ar` (302) | Pass |
| `html[lang]`/`dir` correct per locale (server-rendered) | Pass |
| Language toggle label always offers the *other* language (F1) | **Pass — FIXED** |
| Header responsive layout at 1024×768, both locales (F2) | **Pass — FIXED** |
| Arabic letter-spacing / no leftover proto banner (F3) | **Pass — FIXED** |
| Theme toggle (dark default / `?theme=light`) + persistence across navigation via `localStorage` | Pass (confirmed theme carried from `/en?theme=light` through to `/ar` navigation in the same session) |
| Theme toggle `aria-label` language-correct (`التبديل إلى الوضع الفاتح`/`...الداكن` on `/ar`, `Switch to light/dark mode` on `/en`), and label matches actual current state (checked after clearing `localStorage`) | Pass |
| RTL mirroring — header, hero, discipline strip, stat band, event cards, footer | Pass |
| Arabic Gregorian dates with Arabic-Indic numerals in event cards (e.g. "٣١ أكتوبر", "١٧ أكتوبر", "٢٦ سبتمبر") | Pass |
| Self-hosted fonts only — no `fonts.googleapis.com`/`fonts.gstatic.com` requests, no 404 font files | Pass (10 font files across en/ar, all 200/304) |
| Stat counters animate correctly on scroll into view (1,284+ / 42 / 24 / 18,650+) | Pass |
| `prefers-reduced-motion` respected in CSS (`@media (prefers-reduced-motion:no-preference)` gates all animation blocks in `site.css`) | Pass (source-verified; browser automation in this environment doesn't support forcing the reduced-motion emulation flag, so this is a static-CSS check, not a rendered one) |
| Focus order / visible focus ring, `/en` and `/ar` | Pass (logical order: skip-link → logo → nav items in visual order both LTR and RTL; visible blue focus outline on nav links, confirmed by screenshot) |
| Mobile (390) burger menu opens/closes | Pass, functionally — see N1 for a pre-existing (prototype-inherited) visual bleed-through defect |
| Mobile (390) EN header/burger ~10 px overflow | **Present, pre-existing carry-over** — `scrollWidth: 400` vs `clientWidth: 390` in EN dark and light; not present in AR (`scrollWidth === clientWidth === 390` in both themes). Per task scope this is a known prototype issue, not filed as a new/regression defect. |
| Nav links to not-yet-built pages 404 | Expected (Week 1 scope) — see N4 |
| Footer heading hierarchy | **Fail — N2** (minor, h2→h4 skip) |
| Favicon present | **Fail — N3** (404) |

## Console / network

- Console errors/warnings across every page load checked (`/en`, `/ar`, `?theme=light` variants,
  post-navigation reloads): **0 errors, 0 warnings** in every capture.
- Network (`playwright-cli network --static`, which surfaces fonts/scripts/styles in addition to
  documents): all requests **200 or 304**, zero 4xx/5xx, across `/en` and `/ar` loads. No requests
  to `fonts.googleapis.com` or `fonts.gstatic.com` — all font files served from `/fonts/*.woff2`
  on the app's own origin. One 404 found outside the automated network capture: `favicon.ico`
  (N3), found via direct `curl`.
- Lighthouse: not run — not requested for this pass and no `npx lighthouse` invocation was made;
  do not infer performance numbers from this report.

## Repro commands

```powershell
# Server (from the published snapshot directory, not the repo)
cd "<scratchpad>/qa-snapshot"
$env:DOTNET_ENVIRONMENT='Development'; $env:ASPNETCORE_URLS='http://localhost:5080'; dotnet .\Triathlon.Web.dll

# Optional prototype side-by-side (repo root)
cd "<repo>"
python -m http.server 8012
```

```bash
cd <scratchpad>/qa
playwright-cli -s=qa open "http://localhost:5080/en"
playwright-cli -s=qa eval "() => ({lang: document.documentElement.lang, dir: document.documentElement.dir})"
playwright-cli -s=qa snapshot                       # look for link "العربية" -> /ar
playwright-cli -s=qa resize 1024 768
playwright-cli -s=qa eval "() => ({scrollWidth: document.documentElement.scrollWidth, clientWidth: document.documentElement.clientWidth})"
playwright-cli -s=qa screenshot
playwright-cli -s=qa goto "http://localhost:5080/ar"
playwright-cli -s=qa eval "() => ({lang: document.documentElement.lang, dir: document.documentElement.dir})"
playwright-cli -s=qa snapshot                       # look for link "English" -> /en
playwright-cli -s=qa network --static
playwright-cli -s=qa console
playwright-cli -s=qa close

# Root redirects
curl -s -o /dev/null -D - http://localhost:5080/
curl -s -o /dev/null -D - -H "Accept-Language: ar" http://localhost:5080/
```
