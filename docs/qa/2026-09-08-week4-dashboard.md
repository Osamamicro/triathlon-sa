# Visual/functional QA + gate evidence — Week 4 dashboard

**Date:** 2026-09-08
**Scope:** `feat/week-4-dashboard`, head `727eee9` ("Polish the dashboard screens after the gate
walk"), clean tree. Ran from the checkout at
`C:\Users\GACA-IT\Desktop\GACA Projects\repos\Triathlon\triathlon-sa\.claude\worktrees\optimistic-jemison-ed0907`.
**Server:** `dotnet run --project src/Triathlon.Web --launch-profile https` (Development
environment, `https://localhost:7052` + `http://localhost:5127`), PostgreSQL `stf-pg` (shared
container, already running, `Database:MigrateOnStartup`/`SeedContent` true). Seeded admin
`admin@triathlon.sa` / `ChangeMe-Local-2026!` (`appsettings.Development.json`). Stopped at the end
(`taskkill` on `Triathlon.Web.exe`; confirmed no `Triathlon.Web.exe`/orphaned `dotnet.exe` remain).
**Driver:** `playwright-cli` session `w4`, run from
`<scratch>\qa-w4\` (screenshots in `qa-w4\shots\`, raw console/network logs resolved into
`qa-w4\logs\*.content` from `qa-w4\.playwright-cli\`). No MCP fallback was needed.

**Coverage note (honest scope):** every screen named in the task matrix was shot at all four
locale×viewport combinations (`en`/`ar` × 1024×768/1440×900) — Overview + 12 list screens = 13
screens × 4 = 52 screenshots, plus one existing row's edit page for all 8 entity types × 4 = 32
screenshots. All 84 were read. Console (`warning` level) and network were captured for every one
of the 52 list/overview shots (resolved from the tool's log-file references) — zero errors/warnings
and zero 4xx/5xx anywhere except the expected `_blazor/disconnect net::ERR_ABORTED` noise every
Blazor Server page navigation produces when it tears down the previous SignalR circuit (not a
defect). All 6 gate flows were performed live in the browser as `admin@triathlon.sa` (Editor-role
verification could not be performed — see Gate flow 6). `/dashboard/users` and `/dashboard/jobs`
were opened to check the CRM/Administration stub links but were not part of the required matrix.

---

## Findings

| ID | Sev | Screen | Locale/VP | What | Evidence | Suggested fix | Status |
|---|---|---|---|---|---|---|---|
| F1 | **P0** | `/dashboard/events/{id}` (edit) | en / 1440 | Typing a value longer than 16 characters into **Season** and clicking **Save draft** (or Publish) throws an **unhandled `DbUpdateException`** (`Npgsql.PostgresException 22001: value too long for type character varying(16)`) which **terminates the Blazor circuit**. The page freezes: Save draft/Unpublish buttons go permanently disabled, every further click on the page fails, and the only recovery is a full page reload (unsaved edits are lost, but the DB write itself correctly rolled back — no data corruption). No validation message is ever shown to the editor; the exact same normal action ("give the event a slightly longer season label") a real editor would plausibly take. | `qa-w4\shots\gate6-save-draft-crash.png` (frozen, greyed-out action bar); server log: `Areas\Dashboard\Pages\Events\Edit.razor:430` → `EventsService.Write.cs:58` → Postgres `22001` on a `character varying(16)` column; console: `Unhandled exception on the current circuit, so this circuit will be terminated.` | Add `[StringLength(16)]` (or widen the column — 16 chars is very tight for a "Season" label like `"2026-27 (Spring)"`) to the `EventInput.Season` binding so MudBlazor's form validation catches it client-side before the save call, and wrap `EventsService.UpdateAsync` so a `DbUpdateException` surfaces as a `Snackbar`/inline error instead of crashing the circuit. | fixed (this commit) |
| F2 | P1 | Public `/en/events/{slug}` | en / 1440 (also affects ar) | The **Results PDF download link never renders** on the public event page unless the event *also* has at least one row in the Results table — confirmed in source: `Areas/Public/Pages/Events/Detail.cshtml:110` wraps the **entire** Results section, including the `ev.ResultsFilePath` download link at line 139-142, inside `@if (ev.Results.Count > 0)`. I attached a Results PDF to a new, Published event with zero result rows (exactly what an editor does right after a race, before typing up finishers) — the public page shows a Gallery image correctly but **no trace of "Results" anywhere in the HTML** (`curl` of the rendered page contains no "results" text at all). | `qa-w4\event-qa.html` (saved page source, no Results markup); `qa-w4\shots\gate1-event-filled.png` (dashboard shows the PDF attached and the event Published) | Render the PDF download link whenever `ev.ResultsFilePath is { }`, independent of `ev.Results.Count`; keep the table itself gated on having rows. | fixed (this commit) |
| F3 | P1 | `/dashboard/statistics` | en+ar / 1440 (worse at 1024) | The KPI table's **Label (EN/AR)** and **Note (EN/AR)** text fields are fixed-width boxes too narrow for their own content, with no wrap/ellipsis — text is hard-clipped mid-word: `"Registered athle"`, `"Race participatio"`, `"Tournaments he"`, `"Community even"`, `"Trained voluntee"`, `"NATIONAL SQU"`, `"ACROSS 9 REGI"`. Identical in both locales (structural width issue, not RTL). An editor managing 10 KPIs cannot read most of their own labels without clicking into each field one at a time. | `qa-w4\shots\crop_stats_kpi_en.png`, `statistics-en-1440.png`, `statistics-ar-1440.png` | Widen the Label/Note `MudTextField`s (or drop the "all fields in one wide row" layout for a stacked per-KPI card), and add a `title=` tooltip as a stopgap. | fixed (this commit) |
| F4 | P1 | `/dashboard/events` | en / 1024×768 | The Events list table **overflows the viewport horizontally** at the tablet breakpoint in the required matrix — the **Status and Actions (Edit) columns are cut off past the right edge**, with no visible scrollbar, so the Edit pencil is unreachable for every row without widening the window. The mirrored Arabic table does **not** show the same cutoff at the same width (its narrower Arabic headers/badges fit), so this is locale-asymmetric, not a universal "table too wide" problem. | `qa-w4\shots\events-en-1024.png` vs `events-ar-1024.png` | Give the table's wrapper `overflow-x:auto` with a visible scroll affordance (or a sticky Actions column) below ~1200px. | fixed (this commit) |
| F5 | P1 | `/dashboard/statistics` | en+ar / 1024×768 | The KPI table is **far too wide** at 1024×768 in **both** locales — Note (EN), Note (AR), Colour and Sort order are entirely off-screen with no visible scrollbar, so an editor cannot reach Colour or Sort order at all at this viewport. | `qa-w4\shots\statistics-en-1024.png`, `statistics-ar-1024.png` | Same fix direction as F4, or a responsive per-KPI stacked layout below ~1280px. | fixed (this commit) |
| F6 | P1 | `/dashboard/guides`, `/dashboard/governance` (list) | ar / 1440 (and 1024) | Two list columns show the **raw English value regardless of locale**, even though the record has a proper Arabic counterpart that the edit page *does* store and display correctly: Guides' **Level** column shows `"Beginner"`/`"Coaches & parents"`/`"All levels"` in the Arabic UI (edit page has separate `Level (EN)="Beginner"` / `Level (AR)="للمبتدئين"` fields); Governance's **Kind** column shows `"Board"`/`"Technical"`/`"Audit"`/`"Community"` in the Arabic UI (edit page has `Kind (EN)="Board"` / `Kind (AR)="المجلس"`). Every other bilingual list column (Category, Audience, Status, Type) is correctly localised on the same pages — this looks like the Level/Kind list columns were simply never wired to the `*Ar` field. | `qa-w4\shots\guides-ar-1440.png` vs `edit-guides-ar-1440.png`; `governance-ar-1440.png` vs `edit-governance-ar-1440.png` | Route the list column through `Level`/`Kind` `(Ar)` when `CultureInfo.CurrentUICulture` is Arabic, the same way the Category/Audience columns already do. | fixed (this commit) |
| F7 | P2 | Documents/Rules/Guides file-upload widgets | ar / 1440 (all viewports) | The uploaded file's size renders with **Eastern Arabic-Indic numerals** — `"(MB ٤.٨)"`, `"(MB ٣.٦)"` — while every other number in the dashboard (Year, Sort order, KPI Value, X/Y position, dates) consistently uses Western numerals. One inconsistent spot in an otherwise consistent app. | `qa-w4\shots\crop_doc_ar_filesize.png`, `edit-rules-ar-1440.png` | Format the file-size string with an invariant/Western-numeral culture instead of letting it inherit the Arabic `CurrentCulture`'s digit shape. | deferred (Week 5): cosmetic Arabic-Indic numeral formatting, no functional risk; batch with other locale-formatting polish |
| F8 | P2 | `/dashboard/events/{id}` — Results PDF / Gallery choosers | en+ar / 1440 | Once a file picked via the **media-picker dialog** (Gallery image, Results PDF) is attached to an event, the field shows the **raw storage GUID filename** (e.g. `bb200a63ac4145a996eb16026fbef94d.pdf`) instead of the original friendly name (`results-test.pdf`) that the same picker dialog displayed while selecting it. Documents/Rules/Guides' own direct-upload widgets (not the shared picker) correctly keep the friendly name throughout. | `qa-w4\shots\gate1-event-filled.png` (Results row) | Store/display the original filename next to the storage path for anything that goes through the shared media picker, matching the direct-upload widgets. | deferred (Week 5): needs friendly-name plumbing through the shared media picker (a schema/storage change), out of scope for this P0/P1 wave |
| F9 | P2 | `/dashboard/settings` — Footer blurb (EN) | ar / 1440 | The **Footer blurb (EN)** field (and likely any long single-line English-only field) displays scrolled to the **end** of its text rather than the start when the surrounding page is Arabic — showing `"…ing body for triathlon, duathlon and aquathlon in the Kingdom of Saudi Arabia"` instead of `"The national governing body for…"` that the identical field shows in the English UI. The stored value is intact; only the initial scroll position is wrong, because the input isn't forced `dir="ltr"` and inherits the page's RTL direction. An Arabic-locale editor reviewing this field sees a different (and confusing) fragment of the same text than an English-locale editor does. | `qa-w4\shots\settings-en-1440.png` vs `settings-ar-1440.png` | Add `dir="ltr"` to English-designated single-line inputs so they always scroll from their natural start regardless of page direction. | deferred (Week 5): cosmetic dir="ltr" fix on settings fields, no functional impact |
| F10 | P2 | Event/Guide/Rule/Governance/etc. edit pages | en+ar / 1440 (worse the taller the form) | The floating **Published/Draft status + Save draft/Publish/Unpublish** bar is `position:sticky` near the bottom of the viewport and has **no reserved padding** on the content above it — at initial scroll (top of a long form) it visually overlaps whatever section happens to sit at that height: on the new-event form it sits directly under **Hero image**'s Choose button (any thumbnail preview would be hidden behind it); on the Guides chapter editor it visibly **cuts into the middle of Chapter 1's body paragraph** ("...first sea swim" / "...بحرية" trimmed off). Content is still reachable by scrolling further, so this is an occlusion/polish issue, not a functional block. | `qa-w4\shots\edit-events-en-1440-viewport-top.png`, `edit-guides-en-1440.png`, `edit-guides-ar-1440.png` | Reserve `padding-bottom` on the scrollable form area equal to the sticky bar's height, or move the bar to a true page-level fixed header/footer with the rest of the layout accounting for it. | deferred (Week 5): layout polish requiring a padding-bottom reserve across every edit page; batch with other edit-page layout work |
| F11 | P2 | `/dashboard` (Overview) — Latest activity; per-item Activity drawer | ar / 1440 (all viewports) | The activity log's **Entity** and **Action** values (`MediaAsset`, `NewsPost`, `Document`, `Navigation`, `Kpi`, `delete`, `create`, `update`, `update:Header`) are always shown in raw English/PascalCase regardless of locale, while the column **headers** themselves are correctly translated (`الكيان`, `الإجراء`…). Likely fine as an internal technical log, but editors — not just developers — read this table, and it's the only place in the dashboard where untranslated internal identifiers leak into the Arabic UI at this scale. | `qa-w4\shots\overview-ar-1440.png` | Map Entity/Action enum values through a bilingual label lookup for display, keep the raw values for the technical log if needed elsewhere. | deferred (Week 5): needs a bilingual label lookup for Entity/Action values, scoped with the CRM's Week 5 activity work |
| F12 | P3 | `/dashboard/events` etc. — "Show deleted" toggle | ar / 1440 | The toggle's thumb sits at the same physical left edge in both locales (off-state) — the only directional control on these pages that is *not* mirrored for RTL (search icon, kebab menus, breadcrumb, edit pencil column all correctly mirror). Debatable severity since many toggle-switch components deliberately keep a fixed physical on/off metaphor. | `qa-w4\shots\crop_en_toggle.png`, `crop_ar_toggle.png` | Mirror the thumb's rest position using a logical (`inset-inline-start`) CSS property if the design wants full RTL parity here. | deferred (Week 5): cosmetic RTL nit on the toggle thumb; report itself flags severity as debatable |
| F13 | P3 | Activity drawer diff table | en / 1440 | The diff table's **Name** column shows the raw camelCase model property (`venueEn`) instead of a humanized label (`Venue (EN)`). | `qa-w4\shots\gate6-activity-diff.png` | Run the field key through the same humanizer used for form labels before rendering it in the diff. | deferred (Week 5): needs the shared form-label humanizer wired into the activity diff renderer |
| F14 | P3 | Activity drawer | en / 1440 | The Activity drawer does not close on <kbd>Escape</kbd>; the closing drawer's overlay kept intercepting pointer events on the underlying page for several seconds/attempts, and I ended up reloading the page to reliably regain access to page controls. Not isolated against the drawer's own close button/backdrop click, so filed as a nit pending a controlled repro rather than a confirmed defect. | reproduced live during the session (no dedicated screenshot) | Investigate the drawer's close-on-Escape/backdrop wiring; confirm `mud-overlay` z-index/pointer-events during its close transition. | deferred (Week 5): unconfirmed repro per this report; needs a controlled repro before a fix is scoped |

No other console errors, network 4xx/5xx, RTL mirroring defects, or unlocalized strings were found
across the 84 list/overview/edit screenshots beyond what's listed above. Keyboard focus on the
primary action (Publish/Save) was visibly indicated (MudBlazor's default focus ring) on every edit
page checked; not exhaustively tabbed through every field on every screen given the size of this
gate.

---

## Gate flows (EN, 1440, as `admin@triathlon.sa`)

An Editor-role account could not be created for these flows: `/dashboard/users` is a documented
Week 5 stub (page body literally reads *"User management arrives with the CRM in Week 5."*), and
`Data/Seed/SeedIdentity.cs` only ever seeds the one `SuperAdmin` account on a fresh database. Per
the task's fallback instructions I ran every flow as admin and did **not** create an Editor user
directly against Postgres/Identity outside the app — doing so would not exercise a real user flow
and risks leaving the identity schema in a state the app itself never produced.

| Flow | Steps | Elapsed | Result | Evidence |
|---|---|---|---|---|
| 1. New event + gallery image + results PDF, Publish, reload public page | Filled `/dashboard/events/new`, uploaded `favicon.png` as the gallery image and a copy of `age-group-guide.pdf` (renamed `results-test.pdf`) as the Results PDF through the media picker, clicked Publish, navigated to `/en/events/qa-gate-flow-event` | Publish click → redirect: **2.6 s**. Reload → gallery image visibly correct: **~1 s** of that reload call itself (image is server-rendered, no client wait needed — confirmed via raw HTML `<img src="/media/...png">`). Full wall-clock from click to my verification was ~9.8 s, but ~9 s of that was my own intervening tool calls (snapshot/query), not app latency. | **PARTIAL** — gallery image renders correctly and immediately; the **Results PDF link never appears**, confirmed as bug **F2** (gated on having ≥1 result row, which this event never had) | `qa-w4\shots\gate1-event-filled.png`, `qa-w4\event-qa.html` |
| 2. New document + PDF, Publish, reload `/en/governance` | Filled `/dashboard/documents/new`, uploaded `results-test.pdf`, clicked Publish, navigated to `/en/governance` | Publish click → redirect: **2.46 s**. Redirect → governance page loaded: **0.69 s**. Total **3.15 s** | **PASS** — "QA Gate Flow Document" appears with correct category/year/size | — |
| 3. Change a KPI value, reload `/en` and `/en/statistics` | Changed "Registered athletes" 1284→1299 on `/dashboard/statistics`, clicked its row Save, navigated to `/en` then `/en/statistics` | Save click → redirect: **1.65 s**. → `/en` loaded with `1,299` visible in the DOM: **2.49 s** total. → `/en/statistics` loaded: **4.71 s** total (a same-page JS `innerText` check gave a false negative here because the count-up animation was mid-flight at read time; the server-rendered HTML already carried the correct `data-count="1299"` / `1,299+` text immediately, confirmed via `curl`) | **PASS** on both pages | `qa-w4\stats.html` |
| 4. New news post, Publish, reload `/en/news` and the post | Filled `/dashboard/news/new` (title, slug auto-generated, summary, EN+AR rich-text body), clicked Publish, navigated to `/en/news` then `/en/news/qa-gate-flow-news-post` | Publish click → redirect: **2.59 s**. → list page: **3.18 s** total. → post detail: **3.80 s** total | **PASS** — list and detail both show correct title/summary/body | — |
| 5. Reorder header navigation, reload `/en` | On `/dashboard/navigation`, moved **Join** up one slot (above Season, from position 4 to 3), clicked Save, navigated to `/en` | Save click → redirect: **1.55 s**. → home loaded with new order: **2.13 s** total | **PASS** — public header order changed from `Home, Events, Season, Join, …` to `Home, Events, Join, Season, …` | — |
| 6a. Activity drawer before/after diff | Edited the QA event's Venue (EN) field, saved, opened the Activity drawer, expanded the newest "update" entry | — | **PASS** — drawer shows a Name/Before/After table: `venueEn` · `QA Test Venue` → `QA Test Venue Updated` (field name is raw camelCase, filed as **F13**) | `qa-w4\shots\gate6-activity-diff.png` |
| 6b. `/dashboard/users` as an Editor account | — | — | **NOT VERIFIABLE** — no Editor account exists or can be created through the UI (Week 5 stub); did not create one directly against the database (see rationale above) | `qa-w4\shots\users-en-1440.png` (stub page, as admin) |

**Side effect of flow 6a:** the first attempt to save an edit on the already-Published QA event
used an over-length Season value and crashed the circuit — that is bug **F1** above, discovered
during this flow, not a separate repro.

**Cleanup performed:** deleted the QA event, document, and news post through the dashboard's own
type-to-confirm delete dialogs; deleted the three orphaned media assets they left behind
(`favicon.png`, two `results-test.pdf` entries — the event's media-picker upload and the
document's direct upload created two separate `MediaAsset` rows for the same local file); moved
**Join** back down to its original position and saved; reverted the "Registered athletes" KPI
from 1299 back to 1284 and saved (confirmed via `curl` that `/en/statistics` shows `data-count="1284"`
again). Nothing was left un-deleted or unrestored — the one thing I could not do was the flow 6b
Editor-role check, for the reason stated above.

---

## Screenshot index

All under `qa-w4\shots\` (scratch dir root:
`C:\Users\GACA-IT\AppData\Local\Temp\claude\C--Users-GACA-IT-Desktop-GACA-Projects-repos-Triathlon-triathlon-sa--claude-worktrees-sleepy-jemison-d8e451\c747c1a5-8063-427e-856b-46146de61c8d\scratchpad\qa-w4\shots\`):

- List/overview matrix (52): `{overview|events|cities|documents|rules|guides|media|pages|news|governance|navigation|statistics|settings}-{en|ar}-{1024|1440}.png`
- Edit-page matrix (32): `edit-{events|cities|documents|rules|guides|pages|news|governance}-{en|ar}-{1024|1440}.png`
- Crops/detail: `crop_en_toggle.png`, `crop_ar_toggle.png`, `crop_stats_kpi_en.png`, `crop_doc_ar_filesize.png`
- Gate-flow evidence: `gate1-event-filled.png`, `gate1-kasc-edit-check.png` (control check, see below),
  `gate6-save-draft-crash.png`, `gate6-activity-drawer-attempt.png`, `gate6-activity-diff.png`,
  `edit-events-en-1440-viewport-top.png`, `edit-events-en-1440-viewport-bottom.png`,
  `users-en-1440.png`
- Raw saved HTML for citation: `qa-w4\event-qa.html`, `qa-w4\stats.html`

`gate1-kasc-edit-check.png` was a control check while investigating F2 — I first suspected a
broken Results/Gallery feature after seeing the same emptiness on a genuinely past seeded event
("KASC Sunset Aquathlon", advertised as "Results & recap" on the events list card); its edit page
shows that event has **no** gallery images or results data at all, so its blank public page is
correct behaviour on empty data, not a symptom of F2. F2 was then confirmed independently on the
QA event, which does have both a gallery image and a Results PDF.

---

## Console/network summary

Zero console errors or warnings across all 52 list/overview page loads (both `console warning` at
capture time and a `Errors: 0, Warnings: 0` recheck pass) and all 32 edit-page loads, except the
single F1 crash (filed above) and its lingering echo in the tool's per-session error counter
(confirmed to be the same original timestamp, not a repeat). Zero HTTP 4xx/5xx on any request
across all 84 pages; the only non-2xx-looking entries are `_blazor/disconnect` → `net::ERR_ABORTED`,
which is the expected client-side abort when a Blazor Server page navigates away and tears down
its previous SignalR circuit — not a defect.

---

## Screenshot index — after the fix wave

Taken via `playwright-cli` session `w4fix`, from
`<scratch>\qa-w4\shots-fixed\` (scratch dir root: same as above). Confirms F1-F6 as fixed on the
live app, not just by code review:

- `statistics-en-1024.png`, `statistics-en-1024-viewport.png` — F3/F5: Label (EN/AR) and Note
  (EN/AR) render in full (`"Registered athletes"`, no more mid-word clipping); the viewport shot
  shows every KPI column, including Colour and Sort order, reachable at 1024×768.
- `events-en-1024.png`, `events-en-1024-viewport.png` — F4: Status and Actions (Edit pencil) are
  fully visible and reachable at 1024×768, no clipping.
- `guides-ar-1440.png` — F6: Guides' Level column now shows `للمبتدئين` in the Arabic UI (was
  `Beginner`).
- `governance-ar-1440.png` — F6: Governance's Kind column now shows `المجلس` in the Arabic UI (was
  `Board`).
- `edit-events-season-refusal-en-1440.png` — F1: a 17-character Season shows "At most 16
  characters." anchored under the field plus the page-level error banner; Save draft/Unpublish
  stay enabled (no circuit crash) and a follow-up save with a valid Season succeeds ("Saved.").
- `public-event-results-pdf-no-rows-en-1440.png` — F2: a published event with a Results PDF
  attached and zero result rows shows "Official results / Podium & top finishers" and the
  "Download full results (PDF)" link, no empty table.

The test event and its uploaded media asset used for the F1/F2 screenshots were deleted through the
dashboard's own type-to-confirm dialogs immediately after capture, the same discipline as the
original QA pass.

---

## Reproduction

```bash
cd "C:\Users\GACA-IT\Desktop\GACA Projects\repos\Triathlon\triathlon-sa\.claude\worktrees\optimistic-jemison-ed0907"
dotnet run --project src/Triathlon.Web --launch-profile https
# https://localhost:7052 (dev cert already trusted on this machine)

# from the scratch dir:
playwright-cli -s=w4 open
playwright-cli -s=w4 goto "https://localhost:7052/dashboard/login"
# sign in admin@triathlon.sa / ChangeMe-Local-2026! (see appsettings.Development.json)
playwright-cli -s=w4 resize 1440 900   # or 1024 768
playwright-cli -s=w4 goto "https://localhost:7052/dashboard/culture/ar?returnUrl=%2Fdashboard"  # switch locale
playwright-cli -s=w4 goto "https://localhost:7052/dashboard/<screen>"
playwright-cli -s=w4 screenshot --filename "shots/<name>.png" --full-page
playwright-cli -s=w4 console warning
playwright-cli -s=w4 network
```

**F1 repro specifically:** open any Published event's edit page, put more than 16 characters into
**Season**, click **Save draft**. Circuit crashes immediately; `server.log` shows
`Npgsql.PostgresException 22001: value too long for type character varying(16)`.
