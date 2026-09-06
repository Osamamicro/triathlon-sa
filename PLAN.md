# Saudi Triathlon Federation — Website Redevelopment Plan

Prepared from the Federation's redevelopment proposal (August 2026, EN + AR editions).
This folder contains the plan **and** a working, fully-designed prototype that implements it.

---

## 1. Goals (from the proposal)

Make triathlon.sa professional, organized, and comparable in structure to the
World Triathlon and Asia Triathlon websites, with special focus on UX and mobile
browsing, serving athletes, the public, clubs, coaches, organizers, media, and partners.

## 2. Requirement → Solution map

| # | Proposal requirement | Where it lives in this prototype |
|---|---|---|
| 01 | Layout & design — modern, professional, standardized | Shared design system (`assets/css/main.css`), one header/nav/footer across all pages |
| 02 | Governance & transparency + documents library | `governance.html` — board, committees, and a documents library filterable by **category and year** |
| 03 | Join the federation / become an athlete | `join.html` — registration steps, categories, clubs, and the athlete pathway to the national team |
| 04 | Rules & regulations, downloadable PDFs | `rules.html` — regulation cards; every download delivers a real (placeholder) PDF from `assets/docs/` |
| 05 | Training guide | `training.html` — beginner-to-race guide, extensible for future guides |
| 06 | Federation statistics, dynamic figures | `stats.html` + home stat band — all numbers come from one editable source (`assets/js/data.js`), the prototype's stand-in for a dashboard-driven API |
| 07 | Events & calendar reorganization, page per event | `events.html` (past events separated automatically by date) + `event.html?id=…` detail template with info, date, location, registration, results |
| 08 | Creative event timeline + Kingdom map | `timeline.html` — interactive season timeline wired to an SVG map of Saudi regions |
| 09 | Calendar split: competition / community | Toggle on `events.html` and `timeline.html`; every event is typed `competition` or `community` in the data source |
| 10 | UX & mobile | Mobile-first layout, bilingual EN/AR with full RTL, fast static pages, no build step |

## 3. Information architecture

```
Home
├── Events            (competition ⁄ community split, upcoming ⁄ past, filters)
│   ├── Season Timeline  (creative interactive timeline + Kingdom map)
│   └── Event page       (template: info · date · location · registration · results)
├── Join              (become an athlete, clubs, athlete pathway)
├── Training          (training guide)
├── Rules             (regulations, downloadable PDFs)
├── Governance        (board · transparency · documents library)
└── Statistics        (federation KPI dashboard)
```

## 4. Design system — "Night Race"

Futuristic but institutional; modeled on the content discipline of World Triathlon,
not on sci-fi clichés.

- **Ground**: deep navy `#060B14`, glass surfaces, 1px luminous borders, faint grid.
- **Tri-discipline color code** (the signature): swim `#26C6E8` · bike `#2EE68A` · run `#FFA245`.
  The swim→bike→run gradient is used structurally — section rules, timeline legs,
  discipline chips — so color always *means* something.
- **Type**: Rajdhani (display, sports-timing feel) / IBM Plex Sans (body) /
  IBM Plex Mono (telemetry: dates, distances, results) — Arabic: Tajawal + IBM Plex Sans Arabic.
- **Motion**: animated stat counters, timeline leg drawing, pulsing map markers —
  all gated behind `prefers-reduced-motion`.
- **Themes**: dark "Night Race" is the default identity; a light "Race Day" theme
  is available via the header sun/moon toggle (persisted per visitor, and forceable
  with `?theme=light`). Every colour is a token, so both themes stay legible —
  discipline hues are deepened in light mode for contrast on white.

## 5. Bilingual strategy

- One page, two languages: every string exists in EN and AR (`.en` / `.ar` elements,
  `{en, ar}` fields in data). The toggle flips `<html lang dir>`; the choice persists.
- Layout uses CSS **logical properties** throughout, so RTL needs no separate stylesheet.

## 6. Content management path (production phase)

The prototype keeps *all* editable content — events, statistics, documents, clubs —
in `assets/js/data.js`. In production that file is replaced 1-to-1 by a headless CMS
or dashboard API (the proposal's "easily updatable from the dashboard"):

1. **Phase A (delivered)** — static site, data file, GitHub Pages hosting.
2. **Phase B (working demo delivered)** — `admin.html` is a full content-management
   dashboard: a sidebar back-office with **add / edit / delete** on events, statistics,
   the governance documents library, rules & guides, and clubs, plus a registrations
   viewer and an overview with live KPI cards. Every change is written to a single
   `localStorage` overlay (`stf-overrides`) that `data.js` merges over the base
   content, so edits appear across the whole public site. Production swaps the
   storage layer for a headless CMS / dashboard API — the UI and data flow are proven.
3. **Phase C (working demo delivered)** — `register.html` is the online athlete
   registration flow: validation, category/club/event selection, demo license
   issuance; submissions appear in the dashboard's registrations table. Production
   adds identity verification, payment, and a real backend.

## 7. Out of scope for the prototype

Real document content (branded placeholder PDFs are provided so every download works),
payment processing, identity verification, server-side CMS integration, and real
photography. All figures shown are illustrative placeholders, and the registration
and dashboard demos store data in the visitor's browser only — nothing is transmitted.
