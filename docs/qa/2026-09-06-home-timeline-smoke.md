# Visual QA smoke run — `index.html` + `timeline.html`

**Date:** 2026-09-06
**Scope:** prototype only, two pages, full matrix (en/ar × dark/light × 390×844 / 1024×768 / 1440×900) = 24 screenshots, all read.
**Server:** `python -m http.server 8012 --directory "<repo>"`
**Driver:** `playwright-cli -s=qa`, session cwd `<scratchpad>/qa`, screenshots under `<scratchpad>/qa/index/` and `<scratchpad>/qa/timeline/`, crops under `<scratchpad>/qa/evidence/`.

## Findings

| ID | Sev | Page | Locale/Theme/VP | What | Evidence | Suggested fix |
|---|---|---|---|---|---|---|
| F1 | **P0** | index.html, timeline.html (all pages sharing `assets/js/app.js`) | ar / dark+light / all VP | After `localStorage stf-lang=ar` + reload, header language-toggle button reads **"العربية"** instead of the expected **"English"**. `html[lang]`/`dir` flip correctly to `ar`/`rtl`, only the button label is wrong — so the control tells the user to switch *to* Arabic while already in Arabic, and offers no visible way back to English. | a11y snapshot on both pages: `button "العربية" [ref=e24]` while `html.lang="ar"`, `html.dir="rtl"` (confirmed via `eval`). Screenshots: `index/index-ar-dark-1440.png`, `timeline/timeline-ar-dark-1024.png`(button visible at top). Root cause in `assets/js/app.js`: `boot()` calls `applyLang(...)` at line 249 **before** `buildHeader()` injects the `.lang-toggle` button at line 257 — `applyLang`'s `document.querySelectorAll(".lang-toggle").forEach(...)` finds nothing, so the hardcoded default text `'العربية'` from the `buildHeader()` template (line 181) is never overwritten. Line 259 only re-runs `applyTheme`, not `applyLang`. | Call `applyLang(currentLang())` again after `headerHost.innerHTML = buildHeader()` (same pattern already used for `applyTheme` on line 259), or move the initial `applyLang` call to after header injection. |
| F2 | **P1** | index.html, timeline.html | en+ar / dark+light / **1024×768 only** | Header overflows horizontally: `document.documentElement.scrollWidth` = 1078 vs `clientWidth` = 1024 (54px overflow) at the tablet breakpoint, which sits just above the 920px point where `.main-nav` collapses to the hamburger menu. In **English** this clips the `.lang-toggle` pill at the right edge (shows "لعربية" with the leading ع cut off). In **Arabic** (RTL) it's worse — the nav list is clipped on the left and the language-toggle button is pushed fully off-screen, not visible at all. | Crop `evidence/header-clip-1024.png` (EN, pill text clipped to "لعربية"); `index/index-ar-dark-1024.png` and `timeline/timeline-en-dark-1024.png` full screenshots; `eval` result `{scrollWidth:1078, clientWidth:1024}` measured on both index.html (EN and AR). | The 8-item nav + theme toggle + lang toggle + hidden burger don't fit between 920px (hamburger cutover) and ~1080px needed. Either raise the hamburger breakpoint (e.g. `max-width:1100px`) or shrink nav item padding/font at a new 921–1099px range so `.main-nav` + toggles fit within `clientWidth`. |
| F3 | **P2** | index.html hero chip / top demo banner, all pages via shared CSS | ar / all themes / all VP | Letter-spacing is not fully zeroed for Arabic mono/uppercase elements — `.proto-note` keeps `letter-spacing:.02em` and `.hero-kicker` keeps `letter-spacing:.05em` in `html[lang="ar"]` overrides (down from `.12em`/`.2em`, but not `0`). Visually this still puts a faint, uneven gap between joined Arabic letters inside words (e.g. "نموذج", "السلسلة"), making words look like separated glyph clusters instead of a continuous cursive run — same problem the design already fixed to `0` for `h1`, `h2`, `.brand-name`, `.main-nav a`, `.btn`, etc. (all `letter-spacing:0` in their `html[lang="ar"]` overrides). | `assets/css/main.css:583-587` (`.proto-note` / its ar override) and `:247-251` (`.hero-kicker` / its ar override) — `grep letter-spacing assets/css/main.css`. Zoomed crops: `evidence/proto-note-ar-crop.png` (5760×154 native banner, cropped+3x upscaled) and `evidence/hero-chip-ar-crop2.png` (312×25 native chip, 5x upscaled) both show visible inter-letter gaps in the Arabic word shapes. | Set `letter-spacing:0` for `html[lang="ar"] .proto-note` and `html[lang="ar"] .hero-kicker`, matching the pattern already used everywhere else in the stylesheet (10+ other selectors already do this correctly). |

No other layout/RTL/contrast defects found in the 24 screenshots: RTL mirroring (nav side, map/timeline rail, icon order, `الاتحاد السعودي...` brand block) is correct, light-theme contrast looks fine, Gregorian Arabic-Indic numerals render correctly via `ar-SA-u-ca-gregory` (e.g. "١٤ مارس ٢٠٢٦"), and the one past-dated timeline event (14 March 2026, "COMPLETED") correctly shows as completed relative to today (2026-09-06). Mobile (390px) header is tight against the right edge but does **not** overflow (`scrollWidth === clientWidth === 390`), so no bug filed for that.

## Pass/fail per flow

| Flow | Result |
|---|---|
| Language toggle sets `html[lang]`/`dir` correctly | Pass |
| Language toggle button label reflects state | **Fail — F1** |
| Theme toggle (dark default / `?theme=light`) | Pass |
| Header responsive layout at 1024×768 | **Fail — F2** |
| RTL mirroring (nav, hero, timeline rail, map, brand block) | Pass |
| Arabic numeral/date formatting (`ar-SA-u-ca-gregory`) | Pass |
| Arabic glyph joining under letter-spacing (banner/hero chip) | **Fail — F3** |
| Past event shown as completed (timeline) | Pass |

## Console / network

- Console errors/warnings across all 8 page loads checked (index EN/AR × dark/light, timeline EN/AR × dark/light): **0**.
- Network log: 0 non-200 entries captured (playwright-cli's `network` command only captures requests made after each `console`/`network` invocation point, not full page-load history — no 4xx/5xx observed at any manual check).
- Lighthouse: not run (static prototype smoke run, out of scope per task; command available would be `npx lighthouse http://localhost:8012/index.html`).

## Repro commands

```bash
cd "<repo>"
python -m http.server 8012 --directory "<repo>"

cd <scratchpad>/qa
playwright-cli -s=qa open "http://localhost:8012/index.html"
playwright-cli -s=qa localstorage-set stf-lang ar
playwright-cli -s=qa goto "http://localhost:8012/index.html"
playwright-cli -s=qa resize 1024 768
playwright-cli -s=qa eval "() => ({scrollWidth: document.documentElement.scrollWidth, clientWidth: document.documentElement.clientWidth})"
playwright-cli -s=qa snapshot   # look for button "العربية" under html[lang=ar]
playwright-cli -s=qa screenshot
playwright-cli -s=qa close
```
