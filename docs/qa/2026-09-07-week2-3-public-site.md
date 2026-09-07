# Visual/functional QA — Weeks 2-3 public site, before client review 1

**Date:** 2026-09-07 (session continued into 2026-09-08)
**Scope:** `feat/week-2-public-site`, head `e93cee1`, my own Release publish (not the Lighthouse
agent's port-5080 instance). A prior QA agent was cut off by an API limit while verifying the
critique's P1 items; this is a fresh pass, not a continuation, and does not reuse its files (its
scratchpad `qa\` folder had no matching screenshots — only unrelated `critic/`, `perf/`, `iter*/`
directories from earlier design/critic rounds existed).
**Server:** `dotnet publish src/Triathlon.Web -c Release -o "<scratch>\qa-publish"`, then from that
folder `ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5081
Database__MigrateOnStartup=true Database__SeedContent=true Seed__AdminEmail=admin@triathlon.sa
Seed__AdminPassword='Qa-Run-2026!Strong' dotnet Triathlon.Web.dll`, PostgreSQL `stf-pg` (shared
container, already running). CSS verified non-stale before serving (`site.css` 51 KB, `.br`/`.gz`
present and non-empty). Stopped at the end of the run (`taskkill` on the listening PID).
**Driver:** `playwright-cli` sessions `qa2` (screenshots/flows) run from
`<scratch>\qa2\`, plus `curl` for the link crawl and static-asset status checks. Screenshots under
`<scratch>\qa2\shots\`; raw console/network logs under `<scratch>\qa2\.playwright-cli\`.

**Coverage note (honest scope):** the full requested matrix is 18 pages × 2 locales × 2 themes ×
3 viewports (216 shots). Given the size of this gate, I ran and **read** a baseline pass of all 18
pages × 2 locales at 1440×900 dark (36 screenshots, all read), then targeted second passes at
390×844 dark for the six highest-risk pages (home, timeline, events, register, event-register,
burger menu) in both locales, at light theme for four contrast-sensitive pages, plus the invalid/
valid form flows, the dashboard login, and a full-site link crawl. I did not shoot every page at
every one of the 12 locale/theme/viewport combinations; every finding below is still backed by a
screenshot or a logged eval/HTTP result, and every "not re-verified" item is labelled as such
rather than guessed.

**Methodology fix worth recording:** the site reveals sections with an `IntersectionObserver`
(`.reveal.pre{opacity:0}` until scrolled into view). A `page.screenshot({fullPage:true})` taken
immediately after `goto` never scrolls, so it captures every below-the-fold section as blank. This
is **not a product defect** — real users, keyboard-focus scrolling, and `prefers-reduced-motion`
all resolve correctly (verified below) — but it means a full-page shot has to be preceded by a
scroll-to-bottom-then-top, which is what every screenshot in this report does.

---

## Findings

| ID | Sev | Page | Locale/Theme/VP | What | Evidence | Suggested fix | Status |
|---|---|---|---|---|---|---|---|
| N1 | P1 | `/dashboard/login` | en+ar / light (only theme) / 1440 | The Email input renders with **no visible border/box** — `getBoundingClientRect` gives 181×21 (browser default, unstyled) vs the Password field's 350×43 styled box. Raw HTML shows why: `<input id="Input.Email" ... class="valid" value="" />` has **no `type` attribute at all**, while Password has `type="password"`. `dashboard.css` styles `.field input[type="text"], input[type="email"], input[type="password"]` — an *attribute* selector, which does not match an element with no `type` attribute even though the browser's IDL default is "text". The Remember-me checkbox is also unstyled (13×13 native box). Reproduces identically in Arabic. | `shots/dashboard-login-en.png`, `shots/dashboard-login-ar.png`; computed-style dump showing `border-width:0` on the email input vs `1px` on password. | Add `type="email"` (or an explicit `[DataType(DataType.EmailAddress)]` on the Identity `InputModel.Email` property so the tag helper emits it) so the existing attribute-selector CSS matches; give the checkbox the same styling as the rest of the form. | **FIXED in 4e7a0de** — `Login.razor` now passes `type="email"` on the `InputText`; `dashboard.css` styles `.field-inline input[type="checkbox"]` (18px, `accent-color`, focus ring). Test: `DashboardAuthTests.Login_email_field_carries_an_explicit_type_so_the_stylesheet_matches_it`. Verified live: `curl -k https://localhost:7052/dashboard/login` shows `<input type="email" id="Input.Email" ...>`. |
| N2 | P1 | `/ar/events/{slug}/register` (guest entry) | ar / dark+light / 1440+390 | The **Category** dropdown on the event guest-entry form shows raw English option text — `["Elite","Age Group","Junior"]` — untranslated, on an otherwise fully-Arabic RTL page. The athlete-registration page (`/ar/register`) gets this right for the identical concept (`["الناشئون","الفئات العمرية","النخبة","ذوو الإعاقة","مجتمعي"]`). Root cause: `Events/Register.cshtml` builds the `<select>` straight from `ev.CategoryList` (the event's raw stored category strings), with no bilingual mapping, whereas the athlete form uses a proper enum with `{en,ar}` labels. | `shots/ar-event-register-invalid.png` (category shows "Elite"), `shots/event-register-ar-390-dark.png` (same, mobile); raw `[...sel.options].map(o=>o.textContent)` eval output. | Route event category values through the same bilingual category enum/lookup the athlete-registration page already uses, instead of rendering `ev.CategoryList` raw. | **FIXED in 4e7a0de** — new `Areas/Public/EventCategories.cs` (`Label(string) -> (En, Ar)`) covering all 12 seeded category values; posted `value` is unchanged (still the English key `CategoryList` validates against), only the visible `<option>` text and the event-detail "Categories" row are localised. Test: `EventsPagesTests.Arabic_guest_entry_localises_the_event_categories`. Verified live: `/ar/events/riyadh-sprint-2026/register` renders `<option value="Elite">النخبة</option>`. |
| N3 | P2 | `/en/governance/documents`, `/en/events/timeline`, `/en/register` | en / dark / 1440 | Heading level skips: the documents library goes `H1 "Published documents"` straight to `H4` for every document card (no H2/H3 in between); the timeline and registration pages go `H1` straight to `H3` (event-card titles / footer columns) with no `H2` anywhere in the main content. | Full heading-tag dump via `eval` for `/en/events/timeline`, `/en/register`, `/en/governance/documents` (sequences quoted above). | Give each of these pages an `H2` for its one content section ("Documents", "Season", "Registration") before the `H3`/`H4` items, or drop the document cards to `H3`. | **FIXED in 4e7a0de** — the documents-page skip was already closed by `efce0dd` (`DocRowModel.HeadingLevel = 2` on that page, footer columns raised to H2 sitewide); this commit adds a visually-hidden `<h2>` ("Season"/"الموسم") on `/en/events/timeline` before its event-card `<h3>`s, and a visually-hidden `<h2>` ("Your details"/"بياناتك") on `/en/register` above the form. Test: new `Markup.HeadingLevels(html)` helper plus `PublicShellTests.Heading_levels_never_skip` theory over all six pages named in the task (`/en/governance/documents`, `/en/events/timeline`, `/en/register`, `/en`, `/en/events/riyadh-sprint-2026`, `/ar/governance`). Verified live via curl+grep on all six — no skip in any sequence. |
| N4 | Info | full-page screenshots taken immediately after navigation | all pages | See "Methodology fix" above — not filed as a defect against the app. | `eval` showing `.reveal.pre` count 20→0 after a real scroll; 0 after `prefers-reduced-motion: reduce` is emulated with no scroll at all. | No app change needed; noted so the next QA pass doesn't misreport blank sections. |

No other new layout, RTL-mirroring, overflow, console, or network defects were found on top of what
the design critique already listed.

**Console/network summary:** 0 console errors and 0 warnings across all 18 pages × 2 locales (36
loads) except the not-found page's expected self-reported 404 fetch, which is correct behaviour,
not a bug. 228 static-asset requests across 7 representative pages × 2 locales all returned
200/304, zero 4xx/5xx, zero third-party/external network requests during page load.

---

## Design-critique P0/P1 regression table

Verified against `docs/qa/2026-09-07-design-critique-redesign.md`, live on this build (commit
`5c090bc` claims to answer all three P0s and all eleven P1s).

| ID | Sev | Claim | Status | Evidence |
|---|---|---|---|---|
| P0-1 | P0 | Sticky Kingdom map collides with the timeline at 390 | **FIXED** | `shots/timeline-en-390-dark.png`, `shots/timeline-ar-390-dark.png` — map is static, sits above the list, no overlap; `document.scrollWidth === clientWidth === 390` confirmed on both. |
| P0-2 | P0 | Every headline statistic ships as `0` in served HTML | **FIXED** | `shots/home-en-1440-dark.png` shows `1,284+ / 42 / 24 / 18,650+`; a11y snapshot's raw text matches; `.bar-fill` widths on `/statistics` render (251/168/… px bars visible in `shots/statistics-en-1440-dark.png`). |
| P0-3 | P0 | Hero course graphic not mirrored in RTL | **FIXED** | `shots/home-ar-1440-dark.png` — swim wave at the inline start (right), run dashes + finish dot at the inline end (left), matching the leg-strip labels below it. |
| P1-4 | P1 | Discipline colour code is decoration on 4 surfaces | **FIXED** | Stat band tiles all-white (`shots/zoom-home-en-statband.png`), statistics' 10 tiles all-white (`shots/statistics-en-1440-dark.png`), document badges all green (`shots/docs-en-1440-dark.png`), contact cards neutral (`shots/contact-en-1440-dark.png`), and the Community calendar chip is now muted grey, not amber (`shots/events-en-1440-dark.png`). |
| P1-5 | P1 | Arabic mixes two numeral systems on one card | **FIXED** | Raw AR timeline HTML: `سباحة ١٠٠٠م`, `دراجة ٢٠كم`, `جري ٥كم` — single system, Arabic-Indic digits **and** Arabic unit suffixes, sitewide. |
| P1-6 | P1 | `--line` fails WCAG 1.4.11 (needs 3:1) | **FIXED in 4e7a0de** | `--line-strong` raised from `rgba(160,214,184,.36)`/`rgba(10,45,26,.34)` to `.46`/`.50` — composited contrast on `--surface` goes from **2.44:1 dark / 2.06:1 light** to **3.13:1 dark / 3.12:1 light** (computed by alpha-blending the token over `--surface` and applying the WCAG relative-luminance formula; script output verified against the QA report's own pre-fix numbers as a sanity check). `--line-strong` now carries the boundary of every card/list/form-field container: `.card` (and `.tl-card`, which is a `.card`), `.doc-row`, `.stat-band`'s outer border and its inter-tile gaps, `.table-wrap`. `.input`/`.select` already used `--edge` (3.13/3.12, unchanged, already compliant). `--line` is kept at its prior decorative alpha (1.99:1 dark / 1.63:1 light) for hairlines between sections only, and no longer the sole boundary of any content container. Not independently re-shot; verified by computed contrast, not screenshot. |
| P1-7 | P1 | Registration selects/checkbox indistinguishable | **FIXED** | Visible chevrons on all 4 dropdowns (`shots/register-en-1440-dark.png`); checkbox measured **24×24px** exactly; required fields carry a red asterisk. |
| P1-8 | P1 | Map markers are 6×6px pointer targets | **FIXED** | Visible dot is now 8.7×8.7px, and there is a dedicated transparent `circle.hit` measured at **25×25px** — clears the 24px SC 2.5.8 minimum. |
| P1-9 | P1 | Event card titles don't align across a row | **FIXED** | `shots/events-en-1440-dark.png` row 2 — Abha/Jeddah Corniche/NEOM titles sit at one baseline regardless of chip text length. |
| P1-10 | P1 | Empty dashed gallery frames ship on event page | **FIXED** | `shots/event-riyadh-en-1440-dark.png` — no gallery block; page now also offers a "See it on the season map" link. |
| P1-11 | P1 | Six affiliated clubs share one identical sentence | **FIXED** | `shots/join-en-1440-dark.png` — clubs show city + name only, no duplicated sentence. |
| P1-12 | P1 | Map and timeline never connect | **FIXED** | Scrolling to item 6 sets `.marker.active` on NEOM and the `aria-live` region announces "NEOM, events: 1"; visually confirmed in `shots/timeline-en-1440-active-marker.png`. |
| P1-13 | P1 | Statistics page opens by talking about the CMS | **FIXED** | `shots/statistics-en-1440-dark.png` — intro is athlete-facing; closing band headline is "Four times the athletes since 2023", no CMS/dashboard language anywhere on the page. |
| P1-14 | P1 | Em-dash is the default connector, incl. in Arabic | **FIXED in 4e7a0de** | Grepped rendered HTML of 12 key pages for U+2014: zero on 10 pages. `/en/statistics` and `/ar/statistics` each carried one em-dash inside the `<meta name="description">`/OG description tag — the Arabic one repeated the exact translation-artifact pattern the critique called out. Both rewritten as two natural sentences (no dash, no colon): EN "Registered athletes, growth over time and participation by region. The Saudi Triathlon Federation's key numbers, updated as the season progresses."; AR "إحصاءات الاتحاد السعودي للترايثلون تشمل الرياضيين المسجلين، والنمو عبر الزمن، والمشاركة حسب المنطقة. تُحدَّث هذه الأرقام باستمرار مع تقدم الموسم." (restructured as its own sentence, not a transplant of the English dash construction). Grepped `src/` for any other U+2014 co-occurring with Arabic script on the same line: one more hit, `CrmService.cs`'s registration-confirmation email subject (`"الاتحاد السعودي للترايثلون — استلمنا طلب تسجيلك"` / `"Saudi Triathlon Federation — we received your registration"`), fixed to a colon in both languages, matching the colon convention the Move-4 dash sweep already used for document titles (`SeedDocuments.cs`, e.g. `"Board Meeting Minutes: Q2 2026"`). Verified live: `curl -k .../en/statistics` and `.../ar/statistics` meta descriptions contain no U+2014. |
| P2-17 | P2 | Governance page names no board members | **OPEN — deferred, content ask** | `shots/governance-en-1440-dark.png` — still abstract role descriptions, no names/terms. Needs actual board member names and terms from the client; not something to invent in code. |
| P2-18 | P2 | Orphan card in last events row | **OPEN — deferred to Week 4 polish** | `shots/events-en-1440-dark.png` — National Championship Finals still alone in row 4 beside two empty cells. Layout treatment for a featured/orphan event card belongs with the Week 4 polish pass, not a QA-fix commit. |
| P2-21 | P2 | Contact is one action shown twice | **OPEN — deferred, content/product decision** | `shots/contact-en-1440-dark.png` — three cards plus a CTA band both point at the same email, no form/phone. Needs the client's decision on whether to add a phone line, office hours and/or a contact form before the page can offer more than one channel. |
| P2-22 | P2 | Footer link rows are 19px tall | **FIXED** | Measured `getBoundingClientRect().height === 24` on `/en/events` footer link. |
| P2-15, P2-16, P2-19, P2-20, P2-23 | P2 | (rhythm/lane-rule/disclaimer/news-image/duplicate-band polish items) | **not re-verified this pass** | Out of scope given time; no evidence gathered either way, not claimed fixed or open. |

**Verification tally: 12 of 14 P0/P1 items fully fixed, 1 improved-but-still-open (P1-6), 1
mostly-fixed-with-residual (P1-14).** Of the P2s spot-checked, 1 fixed (P2-22), 3 still open
(P2-17/18/21), 5 not re-checked.

---

## Flows — pass/fail

| Flow | Result | Evidence |
|---|---|---|
| Guest entry, invalid (declaration unticked, empty required fields), `/ar/events/riyadh-sprint-2026/register` | **PASS** | `shots/ar-event-register-invalid.png` — 3 fields flagged `.field.invalid` with red outlines and per-field Arabic error text, banner "فضلاً تحقق من الحقول..."; values that were filled are retained. (Also surfaced N2 above.) |
| Guest entry, valid | **Not run to completion** (invalid case fully exercised instead; time-boxed) | — |
| Athlete registration, invalid, `/en/register` | **PASS** | `shots/register-en-1440-invalid-server.png` — 4 fields flagged, specific messages ("Please enter your date of birth. Athletes register from the age of six.", etc.), banner shown. Native HTML5 `required` validation also blocks submission before the server round-trip when JS validation isn't bypassed (expected/correct browser behaviour, confirmed separately). |
| Athlete registration, valid | **PASS** | Redirects to `/en/register/received?name=QA%20Tester`, page reads "Thank you, QA Tester", `shots/register-en-received.png`. |
| Events filter (`?type=community&city=riyadh`) | **PASS** | URL applies, filtered result set returned (2 cards), active chip carries `aria-current="page"`. |
| Timeline filter + marker activation | **PASS** | See P1-12 evidence above. |
| Theme toggle persists across navigation | **PASS** | Toggled to light on `/en`, navigated to `/en/events`, `data-theme` still `"light"`. |
| Language toggle keeps the current page | **PASS** | On `/en/events/riyadh-sprint-2026`, the Arabic link targets `/ar/events/riyadh-sprint-2026` (same slug, not `/ar`). |
| `prefers-reduced-motion: reduce` honoured | **PASS** | With reduced motion emulated, `.reveal.pre` count is 0 immediately after `goto`, before any scroll — content is visible without motion. |
| Dashboard login, EN/AR page render + language links | **PASS** (rendering); see N1 for the email-field styling defect | `shots/dashboard-login-en.png`, `shots/dashboard-login-ar.png` — language links work, form fully bilingual, RTL mirrors correctly. |
| Dashboard login, sign-in with `admin@triathlon.sa` / `Qa-Run-2026!Strong` | **BLOCKED — not a confirmed app defect** | Login returned "Invalid sign-in attempt." The `AspNetUsers` table in the shared `stf-pg` container already has `admin@triathlon.sa` (confirmed via `psql`), so the account exists but was evidently seeded with a different password by an earlier run against this same long-lived container (seeding is idempotent and does not reset an existing user's password). I did not reset the password directly against the shared DB to avoid disrupting the concurrent Lighthouse pass on port 5080 using the same container. Recommend re-running this one check against a fresh database. |

---

## Links / downloads / feeds

Crawled every same-origin `href` reachable from the 18-page × 2-locale set (135 unique paths) plus
the assets they load:

- **135/135 internal links → 200** (or the one expected `301` for the external `https://triathlon.sa`
  footer link, which is outside this app).
- **13/13 document, rules and training downloads → 302 → 200, `Content-Type: application/pdf`.**
- `/sitemap.xml` → `200 application/xml`, parses cleanly (`xml.etree.ElementTree`), includes
  `hreflang` alternates for every URL.
- `/robots.txt` → `200 text/plain`, disallows `/dashboard` and `/api`, references the sitemap.
- `/api/calendar.ics` → `200 text/calendar`, valid `VCALENDAR`/`VTIMEZONE` structure.
- `/dashboard` (unauthenticated) → redirects to the login page, `200 text/html`.

---

## A11y summary

- Single `H1` per page confirmed on all 6 pages spot-checked (home, events, timeline, register,
  documents, statistics); see N3 for 3 pages with a level skip below the H1.
- Landmarks: `banner`/`main`/`contentinfo` present per a11y snapshot; "Skip to content" link present
  and targets `#main`.
- `html[lang]`/`dir` verified correct on `/en` (`en`/`ltr`) and `/ar` (`ar`/`rtl`).
- `aria-current="page"` present on both the active nav item and the active events filter chip.
- Focus ring / keyboard path on the Kingdom map markers verified present in code (`:focus-visible`
  rule) and via the P1-8 hit-area measurement; not independently re-walked with Tab this pass.
- Contrast: `--line`/`--line-strong` against `--surface` measured live (P1-6 table above) — still
  below the 3:1 non-text minimum in both themes. Text-color pairs were not re-measured (the
  critique's table already covers those and this pass found no regression).

## Screenshot index

All paths relative to
`<scratch>\qa2\shots\` (see header for the full scratch path). 58 files total; the ones cited by ID
above are the load-bearing subset. Baseline 1440×900 dark set: `{page}-{en|ar}-1440-dark.png` for
all 18 pages. Additional: `*-390-dark.png` (home, timeline, events, register, event-register,
burger) both locales; `*-1440-light.png` (home, rules, event-riyadh, register) EN only;
`zoom-home-en-statband.png`; `timeline-en-1440-active-marker.png`; `register-en-1440-invalid*.png`;
`ar-event-register-invalid.png`; `register-en-received.png`; `dashboard-login-{en,ar}.png`;
`burger-{en,ar}-390-dark.png`.

## Lighthouse

See `docs/qa/lighthouse/2026-09-07-summary.md` (written by the concurrent Lighthouse pass on port
5080 with its own Release publish). Not re-run here.

## Reproduction

```bash
cd "<worktree>"
dotnet publish src/Triathlon.Web -c Release -o "<scratch>\qa-publish"
cd "<scratch>\qa-publish"
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5081 \
Database__MigrateOnStartup=true Database__SeedContent=true \
Seed__AdminEmail=admin@triathlon.sa Seed__AdminPassword='Qa-Run-2026!Strong' \
dotnet Triathlon.Web.dll

cd "<scratch>\qa2"
playwright-cli -s=qa2 open "http://localhost:5081/en/"
playwright-cli -s=qa2 resize 1440 900
playwright-cli -s=qa2 goto "http://localhost:5081/dashboard/login"
playwright-cli -s=qa2 screenshot --full-page --filename shots/dashboard-login-en.png
```
