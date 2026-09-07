# Design critique - public site redesign, before client review 1

Jury pass on `feat/week-2-public-site` @ `48d6e7c`, judged against `docs/design/2026-09-07-redesign.md`,
the ten-point client brief in `CLAUDE.md`, and spec 9b.

All evidence is my own capture, taken from a locally built and run app
(`dotnet build`, then `dotnet run --project src/Triathlon.Web --urls http://localhost:5127`),
never the designer's sets. Screenshot root:

```
C:\Users\GACA-IT\AppData\Local\Temp\claude\C--Users-GACA-IT-Desktop-GACA-Projects-repos-Triathlon-triathlon-sa--claude-worktrees-sleepy-jemison-d8e451\6c55dd16-241d-410d-877f-66e8090e8ff7\scratchpad\critic\
```

Every filename below is relative to that root. Contrast figures are computed in the live page with
the sRGB relative-luminance formula, alpha composited over the actual backdrop, not read off the
rationale's table.

Photography is a known gap and is not scored as a defect. The photo plan is section 7.

---

## 1. Scoreboard

| Axis | /10 | Evidence, one line |
|---|---|---|
| Design | 6 | Type pairing and the green ground are genuinely resolved, but the discipline colour code, the rationale's own central rule, is decoration on four surfaces (`en-home-1440-stats.png`, `en-stats-1440-dark.png`, `en-docs-1440-dark.png`, `en-contact-1440-dark.png`) and `--line` measures 1.37:1 light / 1.54:1 dark, so no card, row or input has a visible edge. |
| Usability | 5 | Nav, filters-with-counts and focus rings are right, but at 390 the sticky map permanently eats 389 px of an 844 px viewport and its legend renders garbled underneath the event cards (`ar-timeline-390-sticky-overlap.png`, `en-timeline-390.png`). |
| Creativity | 5 | One real idea exists (course line, leg strip, bib date block, lane rule); it is broken in RTL (`ar-home-1440-hero.png`) and the map and the timeline never speak to each other, despite `.marker.active` already being styled and never set (`en-timeline-1440-scrolled.png`). |
| Content | 5 | English copy is well written, but the served HTML ships `0` for every headline statistic, six affiliated clubs share one identical sentence (`en-join-1440-dark.png`), and the statistics page opens with two sentences about the CMS (`en-stats-1440-dark.png`). |
| Institutional fit | 6 | Documents and rules pages are exactly what a federation should publish (`en-docs-1440-dark.png`, `en-rules-1440-light.png`); the governance page names no board member and the event page ships three empty dashed gallery frames (`en-event-1440-light.png`). |
| Arabic-first quality | 5 | Mirroring and Tajawal are the right calls, but the hero course graphic runs left to right under right-to-left labels, dates use Arabic-Indic digits while distances on the same card use Latin digits with English units, and the registration date field reads `mm/dd/yyyy` (`ar-home-1440-hero.png`, `ar-timeline-1440-dark.png`, `ar-register-1440-light.png`). |

**Verdict: NOT YET.** The award threshold is every axis at 8 or above. No axis reaches it, and no
axis is being carried by another; the failures are independent.

The gap is not ambition. The redesign's argument is correct, and where it is executed it works. The
gap is that the argument is not enforced: the rules the rationale writes down are broken on the
surfaces the client will look at first.

---

## 2. Refuted praise

What `docs/design/2026-09-07-redesign.md` claims, against what the screens deliver.

**Claim 1.1 / 3: "The three discipline colours stop being decoration and become a strict code that
appears only where a discipline is actually named."**
Refuted on four surfaces:

- Home stat band: swim cyan on "Elite athletes", bike green on "Tournaments held", run orange on
  "Race participations" (`en-home-1440-stats.png`).
- Statistics page: ten tiles cycle swim/bike/run down "Affiliated clubs", "Active regions",
  "Trained volunteers", "Women participation", "Under-19 athletes" (`en-stats-1440-dark.png`). This
  is precisely the crime the rationale indicts in section 6 ("cycling the discipline hues down six
  regions made the colour carry no information") and then commits one section up the same page.
- Documents library: PDF badges are cyan for board minutes, orange for financials, green for
  governance (`en-docs-1440-dark.png`).
- Contact: cyan on "Email", green on "Social", orange on "Address" (`en-contact-1440-dark.png`).

Net effect: orange currently means run, community, financial and postal address. The code carries no
information, which is the exact failure the redesign was written to fix. Only "Race day at a glance"
on the rules page uses it honestly (`en-rules-1440-light.png`).

**Claim 6, home: "the stat band becomes a timing board (hairline-divided cells, no card, no side
stripe)."**
Refuted. `en-home-1440-stats.png` shows a rounded bordered card with a filled surface, and every
cell carries a coloured stripe on its top edge. It is a card with side stripes.

**Claim 3, the contrast table.**
The table is thorough for text and for `--brand` as a graphic, and those pairs hold up. It omits
`--line`, the token that draws every card edge, every list-row rule, every leg-strip divider and
every form input. Measured live: `--line` on `--surface` is 1.37:1 in light and 1.54:1 in dark;
WCAG 1.4.11 needs 3:1 for a meaningful non-text boundary. In light theme `--surface` against
`--ground` is 1.11:1, so a card is separated from the page by a 1.37:1 hairline over a 1.11:1 tone
step (`en-rules-1440-light.png`, `en-event-1440-light.png`, `ar-register-1440-light.png`).

**Claim 5, motion: "The map's marker halos ... pulse on hover, focus and selection, which is
feedback rather than ambient noise."**
Half refuted. Hover and focus work. Selection never fires: nothing in the timeline sets
`.marker.active`, so scrolling from September to March highlights no city
(`en-timeline-1440-scrolled.png`). The CSS for the best idea on the site is written and dead.

**Claim 6, season timeline: "The map gets more width and a stronger land fill."**
Width, yes: 526x513 and correctly sticky at 1440. Fill, no: `--map-land` `#17402A` against
`--surface` measures 1.32:1. The Kingdom does not read as a landmass, only as a 2.5 px green outline.

**Claim 6b.1: "The statistics charts rendered no data ... fixed with display:block."**
Confirmed fixed, not refuted. Bars measure 251 / 168 / 105 / 48 px with `display:block` and the
accent fill (`en-stats-1440-charts-scrolled.png`). Recorded because my first full-page capture
appeared to contradict it and did not; see section 8.

**Claims 6b.2 and 6b.3 (mobile menu bleed, 390 header overflow).**
Confirmed fixed. `document.scrollWidth` equals 390 at 390 with no overflowing element, and the
burger, language and theme controls all sit inside the safe area in both cultures
(`ar-home-390-hero-v.png`, `en-timeline-390.png`).

**Claim 4, type: "English and Arabic now share one set of rules."**
Confirmed, and it is the strongest thing in the redesign. Tajawal 800 next to the official bilingual
lockup is the correct call and it reads as one voice in both scripts (`en-home-1440-hero.png`,
`ar-home-1440-hero.png`).

---

## 3. Defects

Severity: **P0** blocks client review 1. **P1** will be raised by the client or by a jury.
**P2** is the polish that separates good from award-level.

### P0

**P0-1 - Sticky Kingdom map collides with the timeline at 390, in both cultures.**
Pages `/en/events/timeline`, `/ar/events/timeline`. Viewport 390x844, both themes.
Evidence: `ar-timeline-390-sticky-overlap.png`, `en-timeline-390.png`, `ar-timeline-390.png`.
`site.css:550` sets `.map-wrap{position:sticky;top:calc(var(--nav-h) + 20px)}` with no mobile media
query. At 390 the map card is 389 px tall and pins for the entire 6 900 px page, so 46 % of the
viewport is permanently a static map and only one event card is ever readable. Worse, the card stack
paints over the map's lower half: the legend and the caption collide with the month heading into
unreadable overlapping text, and in English the caption is clipped to two-letter fragments. This is
the client's own brief point 8, the creative timeline plus Kingdom map, rendering as a visual bug on
the device the brief calls mobile-first.
Smallest fix: `@media (max-width:900px){.map-wrap{position:static;margin-block-end:24px}}`.

**P0-2 - Every headline statistic ships as `0` in the served HTML.**
Pages `/en`, `/ar`, `/en/statistics`, `/ar/statistics`. Both viewports, both themes.
Evidence: `en-home-1440-dark-FULL.png` (band reads `0+ / 0 / 0 / 0+`), `ar-home-1440-dark-FULL.png`
(reads a bare Arabic-Indic zero), plus the raw response:

```
class="stat-value mono" data-count="1284">0<span class="plus">+</span>
class="stat-value mono" data-count="42">0
class="stat-value mono" data-count="24">0
```

`Areas/Public/Shared/_StatBand.cshtml` renders `@Number(0)` and leaves the real figure in
`data-count`, so the number only exists after JavaScript runs and the band intersects. Anything that
does not scroll or does not run JS - the non-JS crawl pass, a social or AI scraper, a printed page, a
screenshot - reports that the Saudi Triathlon Federation has zero registered athletes and has held
zero tournaments. The same pattern applies to `.bar-fill`, whose width lives in `data-w`, so the
statistics charts are blank without JS. Brief point 6 is dynamic statistics; the statistics are
currently zero at the source.
Smallest fix: render `@Number(k.Value)` server-side and have `animateCounter` write `0` into the
element as its first act before animating; give `.bar-fill` an inline `width` server-side and let
`setBarWidth` reset to `0%` then re-apply on intersection.

**P0-3 - The hero course graphic is not mirrored in RTL.**
Page `/ar`. All viewports, both themes.
Evidence: `ar-home-1440-hero.png`, `ar-home-390-hero-v.png`.
The leg strip mirrors correctly, swim on the inline start (right), bike centre, run on the inline
end (left), but the SVG above it does not. The cyan swim wave sits on the far left, directly above
the run cell; the orange run dashes and the finish dot sit on the far right, above the swim cell.
T1 and T2 are in Latin reading order. So the one graphic that carries the site's single idea tells
every Arabic reader that the race starts with running and finishes with swimming, and it contradicts
the labels 60 px below it. On mobile the strip stacks vertically with swim first while the graphic
still runs the other way, which makes the contradiction louder, not quieter.
Smallest fix: wrap the paths in a `course-legs` group and add
`html[dir="rtl"] .course-legs{transform:scaleX(-1);transform-origin:50% 50%}`, with a matching
counter-flip on the T1 and T2 text nodes so the labels stay unreversed.

### P1

**P1-4 - The discipline colour code is decoration on four surfaces.**
Pages `/en` (stat band), `/en/statistics` (ten tiles), `/en/governance` and
`/en/governance/documents` (PDF badges), `/en/contact` (three cards). 1440 and 390.
Evidence: `en-home-1440-stats.png`, `en-stats-1440-dark.png`, `en-docs-1440-dark.png`,
`en-contact-1440-dark.png`. Detail in section 2.
The compounding problem is the calendar chip: COMMUNITY is run amber, so on the Riyadh Community
Aquathlon card an amber COMMUNITY chip sits 30 px above an amber Run 2.5km pill. Two different
meanings, one colour, one card (`en-events-1440-dark.png`).
Smallest fix: delete the `--stat-c` cycling from `_StatBand.cshtml` and let every tile take
`--brand`; give the PDF badge and the contact card a single `--accent`; give the calendar chip an
outline in `--muted` and let the word do the work, so amber only ever means run.

**P1-5 - Arabic mixes two numeral systems inside a single card.**
Pages `/ar`, `/ar/events`, `/ar/events/timeline`, `/ar/events/{slug}`. Both viewports.
Evidence: `ar-timeline-1440-dark.png`, `ar-home-1440-events.png`, plus the raw response:

```
<span class="d-swim">سباحة 1000m
<span class="d-bike">دراجة 20km
<span class="d-run">جري 2.5km
```

`PublicText.Digits()` correctly converts dates, times and KPI values to Arabic-Indic, but
`_Distances.cshtml` prints the seeded string raw, so the same card carries an Arabic-Indic date
beside a Latin-digit distance with an English unit. The hand-written seed adds a third convention:
`SeedPages.cs:399` uses Latin digits with an Arabic unit. Three conventions on one page.
Smallest fix: add `PublicText.Distance(string)` that runs `Digits()` over the numeric part and maps
the m and km suffixes to their Arabic forms, and call it from `_Distances.cshtml`. Then pick one
system and enforce it. My recommendation is Latin digits everywhere including Arabic, because these
are race distances and times that also appear in results, in start lists and on the English site,
and both World Triathlon and Asia Triathlon publish them that way. Whichever the client picks,
shipping both is the defect.

**P1-6 - `--line` fails WCAG 1.4.11, so nothing has a visible edge.**
Every page, both themes, both viewports. Worst in light.
Evidence: `en-rules-1440-light.png`, `en-event-1440-light.png`, `ar-register-1440-light.png`,
`en-home-1440-light-FULL.png`.
Measured live: `--line` composited on `--surface` is 1.37:1 in light and 1.54:1 in dark; 3:1 is
required. In light, `--surface` (#FFFFFF) against `--ground` (#EFF4F0) is 1.11:1. The rules-page
middle band and the whole registration form read as one flat sheet.
Smallest fix: take `--line` to about `rgba(10,45,26,.28)` light and `rgba(160,214,184,.30)` dark,
which lands both near 3:1, and give light-theme cards a tinted shadow instead of relying on the tone
step alone.

**P1-7 - Four registration dropdowns are visually indistinguishable from text inputs.**
Pages `/en/register`, `/ar/register`. Both viewports, both themes.
Evidence: `ar-register-1440-light.png`, `ar-register-390-dark.png`.
Measured on the live controls: city, category, club and event all compute `appearance: none` with
`background-image: none`, so no chevron is drawn. They render as boxes with text in them, identical
to the name and email fields. The documents-page year filter does have a chevron, so the site
contradicts itself about what a select looks like. The same form has a 17 x 17 px consent checkbox
and no required marking on four required fields.
Smallest fix: give `select` the same inline SVG chevron the filter select uses, positioned with a
logical property so it mirrors; grow the checkbox to 24 px; mark required labels.

**P1-8 - The map markers are 6 x 6 px.**
Pages `/en/events/timeline`, `/ar/events/timeline`. Both viewports.
Evidence: `en-timeline-1440-scrolled.png`; measured `getBoundingClientRect` on
`.ksa-map .marker circle.core` is 6 x 6 at 1440, smaller at 390.
These are the only pointer targets of the site's signature feature and they miss WCAG 2.2 SC 2.5.8
(24 px) by a factor of four. The keyboard path and the focus ring are correctly implemented, which
makes the pointer target the only broken half.
Smallest fix: add a transparent 14-unit-radius circle as a hit area inside each marker group; the
visible dot stays 6 px.

**P1-9 - Event card titles do not align across a row.**
Pages `/en/events`, `/ar/events`. 1440.
Evidence: `en-events-1440-dark.png` (rows 2 and 3), `en-timeline-390.png` (cards 1 vs 2).
The chip row sits beside the bib date block when the chips are short (OPENS SOON) and wraps onto its
own line when they are long (REGISTRATION OPEN). Inside one row, Abha Highlands Youth Race and NEOM
Duathlon Challenge sit at one baseline while Jeddah Corniche Olympic Triathlon sits 30 px lower.
Three cards, two layouts, one component.
Smallest fix: put the chip row on its own grid line under the date block unconditionally.

**P1-10 - Empty dashed gallery frames ship on the event page.**
Page `/en/events/{slug}`. 1440 and 390, both themes.
Evidence: `en-event-1440-light.png`, `en-event-1440-dark.png`.
Three dashed rectangles labelled SWIM, BIKE and RUN occupy 160 px of the conversion page. Shipping a
visible empty state to client review 1 reads as unfinished, not as a considered placeholder. The
page is otherwise thin: two paragraphs, a leg strip, a key-information table, no course map, no wave
times, no race-director contact.
Smallest fix: hide the gallery block when the event has no images, and use the space for the Kingdom
map focused on the host city instead of a link that sends the user away to the season map.

**P1-11 - Six affiliated clubs share one identical sentence.**
Pages `/en/join`, `/ar/join`. 1440.
Evidence: `en-join-1440-dark.png`.
The line about coached swim, bike and run sessions for beginners repeats verbatim under Riyadh Tri
Club, Jeddah Waves, Eastern Endurance, Yanbu Open Water, Asir Peaks and NEOM Multisport. Six
identical strings in one grid is the fastest way to make real content look like lorem ipsum.
Smallest fix: drop the line and let city plus name carry the card, until the Federation supplies one
real sentence per club.

**P1-12 - The map and the timeline never connect.**
Pages `/en/events/timeline`, `/ar/events/timeline`. 1440.
Evidence: `en-timeline-1440-scrolled.png`, scrolled to December with no marker highlighted.
`site.css` already defines the active marker radius, the active label colour, the active halo and a
Selected legend entry. Nothing sets `.active` from scroll position. Brief point 8 asks for a creative
timeline and a Kingdom map; what ships is a list and a picture in adjacent columns. This is the
single biggest reason Creativity scores 5.

**P1-13 - The statistics page opens by talking about the CMS.**
Pages `/en/statistics`, `/ar/statistics`. 1440.
Evidence: `en-stats-1440-dark.png`.
The intro says the figures are managed from the federation dashboard and update the moment they are
published. The closing band, at the largest headline weight in the lower half of the page, is titled
Numbers that update themselves and explains that every figure reads from a single data source
without touching the code. Two pieces of agency-to-client copy on a public page. An athlete does not
care where the number comes from.
Smallest fix: replace the band headline with the story the numbers tell, for example four times the
athletes since 2023, and cut the CMS sentence from the intro.

**P1-14 - The em-dash is the default connector, including in Arabic prose.**
Every page, both cultures.
Evidence: `en-docs-1440-dark.png`, `en-governance-1440-dark.png`, `en-rules-1440-light.png`,
`ar-home-1440-hero.png`; 295 instances across the `.cs` and `.cshtml` sources, including
hand-written Arabic seed strings at `SeedPages.cs:176`, `:181`, `:402`, `:408`, `:414`, `:473`.
In English it is a tic that repeats four times on the governance page alone, and it stands in for a
colon inside four document titles. In Arabic it is a translation artifact: Arabic prose reaches for a
comma, a colon or a second sentence, not a dash carried over from the English draft.
Smallest fix: sweep the seed. English gets a colon, a comma or a period; Arabic gets its own comma
or a new sentence; document titles get a colon.

### P2

**P2-15 - Templated section rhythm.** `/en`, 1440, `en-home-1440-dark-FULL.png`. Six sections, six
identical heads: mono uppercase eyebrow, large sentence-case headline, pill link at the far inline
end (FEDERATION IN NUMBERS, NEXT START LINES, FROM THE NEWSROOM, 2026-27 SEASON, FOR EVERY ROLE,
plus the hero). Three card grids follow: 3-up events, 3-up news, 4-up roles. The page plays one
rhythm six times, which is what makes it read as competent rather than authored. Drop the eyebrow
from at least three sections and let two of the grids become something other than a grid.

**P2-16 - The lane rule is orphaned.** `/en/statistics`, 1440,
`en-stats-1440-charts-scrolled.png`. The device itself is good, brand-owned and RTL-native, but at
this scale it appears as a 130 px green dash at the far inline start of a 1440 px band with 200 px
of empty ground above and below it and nothing on its line. It reads as a stray tick, not as a lane
marker. Tighten the section padding around it, or run it full-bleed with the solid segment at the
inline start.

**P2-17 - The governance page names no one.** `/en/governance`, 1440,
`en-governance-1440-dark.png`. Four cards describe what a board, a technical committee, an audit
committee and an athletes commission are, in the abstract. No member names, no roles, no terms, no
committee composition. Names need no photography. For a national federation this is the biggest
institutional-fit gap on the site and it is a pure content ask of the client.

**P2-18 - Orphan card in the last events row.** `/en/events`, 1440, `en-events-1440-dark.png`.
Ten upcoming events in a 3-up grid leaves National Championship Finals, the most important race of
the season, alone in row four beside two empty cells, reading as a layout error. Feature it as a
full-width row at the top of the list.

**P2-19 - A full section for one disclaimer.** `/en/rules`, 1440, `en-rules-1440-light.png`. The
sentence saying the summary is informational and the downloadable rules are authoritative gets its
own lane rule, its own 300 px band, and an indent that aligns with nothing else on the page. Make it
a caption under Race day at a glance.

**P2-20 - The news card has no image region.** `/en/news` and `/en`, 1440, `en-news-1440-dark.png`.
Date, title, standfirst, link, and no media slot, so when photography arrives the component has to
be redesigned rather than filled. Add a 16:9 slot now with a brand-mark fallback. The `/news` index
is also identical to the home page news section: same three cards, no filter, no featured story, no
pagination.

**P2-21 - Contact is one action shown twice.** `/en/contact`, 1440, `en-contact-1440-dark.png`.
Three equal cards (email, social handle, postal address), then a band whose CTA is the same email.
No form, no phone, no office hours, no committee routing, despite the copy promising three ways to
get an answer. More than 500 px of the page is empty ground.

**P2-22 - Footer link rows are 19 px tall.** All pages, 390. Measured on `/en/events` at 390:
thirteen footer anchors at 19 px height with roughly 18 px gaps, under WCAG 2.2 SC 2.5.8's 24 px.
Raise the line box to 24 px minimum.

**P2-23 - Two identical drenched-green bands on the home page.** `/en`, 1440,
`en-home-1440-dark-FULL.png`. One season, eight cities and Ready for your first start line are the
same component, the same fill and the same left-text right-button composition, 800 px apart. Give
one of them a different shape.

---

## 4. Top 5 moves before client review 1

Ranked by score movement per hour. Each is stated so a front-end engineer can execute it without
asking a question.

**Move 1 - Render the real numbers server-side. (S, about 1 h. Fixes P0-2. Content 5 to 6, plus
Usability and SEO.)**
In `Areas/Public/Shared/_StatBand.cshtml`, replace both `@Number(0)` calls with `@Number(k.Value)`,
keeping `data-count` as it is. In `wwwroot/js/site.js`, make `animateCounter(el)` write `"0"`, or
`Digits("0")` under `html[lang=ar]`, into the element as its first statement, then run the existing
tween up to `data-count`; guard it with the existing `reduced` flag so reduced-motion users keep the
real value and never see the reset. Do the same for `.bar-fill` on `/statistics`: emit
`style="width:@fill%"` from the Razor view, and in the observer path set `width:0` then re-apply on
the next frame. Acceptance: `curl http://localhost:5127/ar | grep stat-value` shows the real figure,
not a zero, and the count-up still animates in a normal browser.

**Move 2 - Unstick the map below 900 px. (S, about 30 min. Fixes P0-1. Usability 5 to 6.)**
In `wwwroot/css/site.css`, beside the `.map-wrap` rule at line 550, add
`@media (max-width:900px){.map-wrap{position:static;margin-block-end:24px}}`. Then verify at 390 in
both cultures that the map scrolls away, that the legend and the caption are fully visible inside
the card, and that no month heading overlaps it. Acceptance: recapturing
`ar-timeline-390-sticky-overlap.png` shows no overlapping text.

**Move 3 - Mirror the hero course in RTL. (S, about 2 h. Fixes P0-3. Arabic 5 to 6, plus
Creativity.)**
In `Areas/Public/Shared/_HomeHero.cshtml`, wrap the swim path, the bike path, the run dashes and the
start and finish dots in a single `<g class="course-legs">`, leaving the T1 and T2 `<text>` nodes in
their own `<g class="course-marks">`. Add to `site.css`:
`html[dir="rtl"] .course-legs{transform:scaleX(-1);transform-origin:50% 50%}` and the same flip on
`.course-marks`, with a per-node `scaleX(-1)` counter-flip on the two `<text>` elements so the labels
read the right way round at mirrored x positions. If the draw animation uses `stroke-dashoffset`,
confirm it still runs start to finish after the flip. Acceptance: at `/ar` the cyan swim wave sits
directly above the swim cell and the orange finish dot sits at the inline end (left).

**Move 4 - One Arabic numeral system, and kill the em-dash. (S/M, about 3 h. Fixes P1-5 and P1-14.
Arabic 5 to 7.)**
Add `public static string Distance(string raw)` to `Areas/Public/PublicText.cs` that, when
`IsArabic`, runs `Digits()` over the numeric part and maps the `m` and `km` suffixes to their Arabic
forms; call it from `_Distances.cshtml` for all three legs, and fix the hand-written distances at
`SeedPages.cs:399`, `:405` and `:411` to use the same form. Then decide the system once and apply it
through `Digits()` sitewide; my recommendation is Latin digits in Arabic too, for the reason in
P1-5. In the same pass, sweep the em-dash out of every user-visible string in `Data/Seed/*.cs` and
`Areas/Public/Pages/**`, including the four document titles. Acceptance: no page shows two numeral
systems, and grepping the seed for the em-dash returns nothing.

**Move 5 - Wire the map to the timeline. (M, about 4 h. Fixes P1-12. Creativity 5 to 7, and it is
the one memorable idea the brief asked for.)**
In `wwwroot/js/timeline.js`, put an `IntersectionObserver` on every `.tl-item` with
`rootMargin: "-45% 0px -45% 0px"`, so exactly one item is current; on intersection read its city
slug and toggle `.active` on the matching `.ksa-map .marker`, clearing it from the others. The
styles already exist and already handle the halo, the radius and the label colour. Add the reverse
direction: click and Enter on a marker scroll that city's first event into view, with `behavior`
switched to `auto` under `prefers-reduced-motion`. Announce the change on a visually hidden
`aria-live="polite"` node, for example "Riyadh, 2 events". Fall back to click-only when `reduced` is
set. Acceptance: scrolling from September to March lights NEOM, then Taif, then AlUla, then Dammam,
then Riyadh in turn, and the Selected legend entry finally means something.

Runners-up in order, if there is time: enforce the discipline colour rule (P1-4, M, about 2 h,
Design 6 to 7); raise `--line` to 3:1 (P1-6, S, about 1 h); add the select chevron and the 24 px
checkbox (P1-7, S, about 1 h); remove the empty gallery frames (P1-10, S, 15 min); de-duplicate the
six club sentences (P1-11, S, 15 min).

---

## 5. Arabic notes, judged as a first audience

Read as a Saudi federation reader would read it, not as a translation check.

### What is right

Tajawal 700/800 for display is the correct decision and it is the single best move in the redesign;
it sits next to the official bilingual lockup without arguing with it (`ar-home-1440-hero.png`).
Logical properties do their job: nav, hero, cards, the timeline rail, the registration form's
two-column split and the footer all mirror correctly with no separate stylesheet
(`ar-register-1440-light.png`, `ar-timeline-1440-dark.png`). The +6 % size and +0.12 line-height for
Arabic gives the hero paragraph a comfortable measure, roughly 45 to 55 characters at 1440 and about
38 at 390, which is right for Arabic. Gregorian dates in Arabic, with the neutral `ar` culture rather
than `ar-SA` to avoid the Umm al-Qura calendar, is the correct call for a sports calendar, and the
code comment shows it was a decision rather than an accident.

### Typography

- The numeral mix is the headline problem (P1-5). Dates, times and KPIs are Arabic-Indic; distances
  are Latin with English units; one seed string is Latin with an Arabic unit. Three conventions.
- `MonthShort` correctly refuses to abbreviate Arabic month names, but the timeline month heading
  then renders at `.82rem` in the body face while English gets the same heading in mono at `.7rem`
  with 0.2em tracking. The English heading has a structural, calendar-page feel; the Arabic one
  reads as ordinary body text and loses that hierarchy. Give the Arabic heading a rule or a weight
  step so it separates months as clearly as the English does (`ar-timeline-1440-dark.png`).
- The em-dash appears inside hand-written Arabic prose (P1-14). It is the clearest single tell that
  the Arabic was written from the English rather than alongside it.
- The kasra on the third verb of the hero headline sits on a display-weight glyph at 64 px and, at
  390, crowds the full stop that follows it. Either set the word without the diacritic or add a
  little space (`ar-home-390-hero-v.png`).

### Copy tone, formal Saudi register

- `كن رياضياً` as the secondary hero CTA translates "Become an athlete" literally, but in Arabic it
  reads as an exhortation to good sportsmanship rather than an instruction to register. A federation
  would say `سجّل كرياضي` or `انضم إلى الاتحاد`. This is the site's second-most-clicked button.
- `اجرِ` for "Run" is ambiguous: the imperative of جرى is most often read as "carry out" or
  "conduct". `اركض` is the unambiguous sports imperative and is what a Saudi reader expects beside
  `اسبح` and `اركب`.
- The headline reproduces the English staccato full stop between single words. It is legible, but it
  is an English rhetorical shape wearing Arabic. Worth one pass with a native sports copywriter.
- The registration and governance copy is otherwise in the right register: `الاتحاد السعودي
  للترايثلون`, `الجهة الوطنية المنظمة` and `الحوكمة والشفافية` are all correct institutional Arabic,
  and the privacy note referencing the Saudi personal data protection system is exactly right.

### Mirrored components

- The hero course graphic is the one component that does not mirror, and it is the important one
  (P0-3).
- The date-of-birth field is explicitly `dir="ltr"` and renders `mm/dd/yyyy` in an otherwise fully
  RTL form (`ar-register-1440-light.png`). The format string comes from the browser locale, so an
  `ar-SA` browser will differ, but the field ships with no Arabic format hint and is the only LTR
  control among nine. Add a visible `يوم/شهر/سنة` helper under the label, or split it into three
  selects.
- The three-segment footer strip renders swim cyan on the left in both cultures. In RTL it should
  start with swim at the inline start (right), so the strip reads swim, bike, run in the reading
  direction and matches the leg strip directly above it on the home page.
- The registration form's aside sits on the inline end and the form on the inline start, correctly
  mirrored, and the checkmark list mirrors with it. Good.

---

## 6. Do-not-change list

Three things that already work and must survive the next round.

**1. The type system.** Tajawal for display in both scripts against IBM Plex Sans and Plex Sans
Arabic for body, sentence case, `-0.015em`, one shared set of rules with Arabic taking +6 % size and
+0.12 line-height. It is the reason the site reads as one voice in two languages, it deleted three
font files, and it cut `html[lang=ar]` overrides from 21 to 6. Do not reintroduce a condensed display
face and do not re-add uppercase to English headings.
Evidence: `en-home-1440-hero.png`, `ar-home-1440-hero.png`.

**2. The documents library as a filing list.** Hairline rows, mono category and year columns, file
size, a download affordance on hover and focus, and filter chips carrying their own counts (All 9,
Governance 4, Financial 2, Board minutes 3). It is the most institutional thing on the site and it is
exactly what brief point 2 asked for. The only change it needs is the PDF badge colour.
Evidence: `en-docs-1440-dark.png`.

**3. The bib date block and the leg strip.** The SEPT / 26 block is the one piece of visual language
that is unmistakably a race rather than a generic listing, and it repeats correctly across the events
grid, the timeline and the home page. The swim/bike/run leg strip is the one place the discipline
colour code is honest, and it works identically on the home hero, the event page and the rules
page's Race day at a glance.
Evidence: `en-events-1440-dark.png`, `en-event-1440-light.png`, `en-rules-1440-light.png`.

Also worth protecting, though not in the top three: the accessibility baseline is better than most
sites at this stage. One `h1` per page, no image without `alt`, no unlabelled SVG, a 2 px `--accent`
focus ring with a 3 px offset that reads on every surface including the map markers, a genuine no-JS
path for the reveal animation, and a `noscript` submit beside every auto-submitting filter select.
Do not regress any of that while fixing the defects above.

---

## 7. Photo plan (the known gap)

No client photography exists. That is not scored, but the plan should be agreed at review 1, because
two components need to be built to receive it now rather than redesigned later.

**Build the slots before the shoot.**

- The news card has no media region at all (P2-20). Add a 16:9 slot with `object-fit: cover` and a
  brand-mark-on-green fallback, so the component works empty and full.
- The event page's gallery should hide when empty rather than render dashed frames (P1-10).
- Every page hero is currently a half-empty rectangle at 1440. Give the hero an optional full-bleed
  image layer behind the existing green scrim, with the mark watermark as the fallback, so a
  photograph can be dropped in per page without a layout change.

**Shot list, in priority order.**

1. Three hero plates, 2400 x 1350, one per season anchor: an open-water swim start at first light on
   the Red Sea, a closed-road bike leg with the Riyadh skyline, a run finish under the Federation
   arch. Dawn or golden hour, athletes in Federation kit, graded so the greens sit with `#008C3D`
   rather than fighting it. These fix the empty half of the home hero.
2. One landscape plate per calendar city, 1600 x 900, for the eight event pages and the map panel. A
   venue or course frame is enough; it does not need athletes.
3. Board and committee portraits, 1200 x 1500, consistent lighting and background, for the governance
   page. Pair them with the names and roles that are currently missing (P2-17).
4. Six club frames, 1200 x 800, one per affiliated club, to replace the six identical sentences
   (P1-11).
5. A small library of transition, volunteer and first-timer frames for news. Faces of ordinary
   participants, not only elites, because the whole copy argument of the site is that you do not
   need a racing background.

**Interim.** Until the shoot happens, do not fill the gaps with stock triathlon photography and do
not ship visible empty frames. An honest, well-composed green band carrying the mark is better at
client review 1 than either.

---

## 8. How this was verified

App built from `48d6e7c` with `dotnet build`, run at `http://localhost:5127` against the `stf-pg`
PostgreSQL container. Captures with `playwright-cli -s=jury` from the session scratchpad, at
1440x900 and 390x844, in `en` and `ar`, dark (default) and `?theme=light`. Thirty-nine screenshots,
named individually throughout. Contrast, element geometry and computed styles were measured in the
live page with `playwright-cli eval`, not estimated from an image.

Pages covered: `/en`, `/ar`, `/en/events`, `/en/events/riyadh-sprint-2026`, `/en/events/timeline`,
`/ar/events/timeline`, `/en/join`, `/en/register`, `/ar/register`, `/en/training`, `/en/rules`,
`/en/governance`, `/en/governance/documents`, `/en/statistics`, `/en/news`, `/en/contact`.

Two methodological notes, recorded because they nearly produced false findings and because the next
QA round will hit both:

- Full-page Playwright captures do not scroll, so `IntersectionObserver` never fires. Every
  `.reveal` section below the fold appears blank, the counters read zero and the statistics bars
  read empty. The reveal behaviour and the bar behaviour are both correct in a real browser. Only
  the zero in the served HTML is a genuine defect (P0-2), and that was confirmed with `curl`, not
  with a screenshot. Work around it by clearing the `.pre` class with `eval` before capturing, or by
  capturing at scroll offsets.
- `position: sticky` also flattens in full-page captures, which made the timeline map look like a
  postage stamp. Measured live it is 526 x 513 and correctly sticky at 1440, so that finding was
  dropped. The mobile behaviour (P0-1) is a different and real problem, found by scrolling at 390.

Every finding above survived a refutation pass: for each one I asked whether a second juror could
strike it as personal taste, and dropped it if the answer was yes. Dropped on that basis: the
full-page blank sections and the empty statistics bars (capture artifacts), the map being too small
(wrong, it is 526 px and sticky), the run leg being drawn as dashes while swim and bike are drawn as
curves (taste), and the absence of photography (an instructed known gap, addressed in section 7 as a
plan rather than scored as a defect). What remains is measured, reproducible, or visible in a named
screenshot.
