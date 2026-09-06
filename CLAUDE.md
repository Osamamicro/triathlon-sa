# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

Saudi Triathlon Federation (triathlon.sa) website work. Two layers live here:

1. **Static prototype** (repo root) — the approved design/UX mock, hosted on GitHub Pages
   (`main` branch root, `.nojekyll` present). No build step, no backend, no dependencies.
   Live: https://osamamicro.github.io/triathlon-sa/
2. **Production planning** (`docs/`) — design spec, implementation plan, and the client
   technical/financial proposal for the real platform (public site + CMS dashboard + CRM +
   admin panel). See `docs/superpowers/specs/` and `docs/superpowers/plans/`.

The client brief is the two PDFs at repo root (EN + AR, identical 10-point content):
layout/design, governance & documents library, join/become an athlete, rules (PDF downloads),
training guide, dynamic statistics, events & per-event pages, creative timeline + Kingdom map,
competition/community calendar split, mobile-first UX. `PLAN.md` maps each point to a prototype page.

## Commands (prototype)

```bash
python3 -m http.server 8000      # serve locally, open http://localhost:8000/
```

No lint, test, or build tooling exists for the prototype. Deployment = push to `main`.
Force light theme for screenshots with `?theme=light`.

## Prototype architecture (read these three files first)

- `assets/js/data.js` — the single content source. Exposes global `STF` with `stats`,
  `regionAthletes`, `growth`, `cities` (SVG map coordinates), `events`, `documents`, `rules`,
  `clubs`. Every bilingual value is `{ en, ar }`. Events are typed `competition | community`
  and past events are filtered by date at render time, not by hand.
- `assets/js/app.js` — shared app shell IIFE. Injects header/footer into every page, owns the
  EN⇄AR toggle (flips `<html lang dir>`, persisted in `localStorage` key `stf-lang`), the
  dark/light theme toggle (`stf-theme`), scroll reveal, stat counters, and `renderEventCard`.
- `assets/css/main.css` — design system ("Night Race" dark default, "Race Day" light).
  Uses CSS logical properties everywhere so RTL needs no separate stylesheet. Discipline colour
  code: swim `#26C6E8`, bike `#2EE68A`, run `#FFA245`. All colours are tokens.

**Bilingual convention:** each string exists twice in markup as `<span class="en">` /
`<span class="ar">`; CSS shows one based on `html[lang]`. Data uses `{en, ar}` objects and the
`bi()` helper in `app.js` renders both spans. Keep this pattern when touching prototype pages.

**Dashboard overlay:** `admin.html` writes edits to `localStorage` key `stf-overrides`;
`data.js` merges it over base content via `STF.mergeOverlay`, so admin edits appear site-wide
in that browser only. `register.html` writes demo registrations the same way. Nothing is
transmitted anywhere — this is the stand-in for the production API.

`assets/docs/*.pdf` are branded placeholder PDFs so every download link works.

## Production platform (planned, not yet in this repo)

Decided stack (v2, see the spec for rationale): **one ASP.NET Core 10 web app** — Razor Pages
for the public site (ports the prototype's HTML/CSS/JS), Blazor + MudBlazor for the dashboard
(CMS + CRM + admin), EF Core with PostgreSQL or SQL Server by config, output caching evicted on
publish, Hangfire for jobs. Deploys with one `dotnet publish` to IIS, Linux or App Service;
staging and production are two sites on the Federation's server. Production code goes under
`src/Triathlon.Web` and `tests/`; the prototype stays at root until cut-over. Timeline 6 weeks.

## Project agents & polish loop

`.claude/agents/` defines the design/architecture team (dispatch with the Agent tool by name;
new definitions load at session start):

- `stf-frontend-designer` — award-level public-site design + implementation, Playwright evidence
- `stf-dashboard-ux` — CMS/CRM/admin panel UX + implementation
- `stf-architect` — .NET/Next.js architecture, ADRs in `docs/adr/`, blueprints; docs only
- `stf-visual-qa` — Playwright matrix (en/ar × dark/light × 3 viewports), reports to `docs/qa/`
- `stf-design-critic` — adversarial jury, Awwwards-style scoreboard, top-5 moves

`/stf-polish <target>` orchestrates designer → QA → critic rounds. Browser automation uses the
`playwright-cli` binary (sessions via `-s=<name>`); run it from the scratchpad so its
`.playwright-cli/` output stays out of the repo (also gitignored).

## Client facts worth knowing

Live site triathlon.sa is Wix; brand colour is green `#008250` with a swirl mark
(`docs/proposal/assets/stf-mark.png`). The prototype's navy/teal palette is a design exploration,
not the Federation's identity. Existing content to migrate and extra CMS types (news, gallery,
FAQ, media center, high-performance/EPP pages, coaching framework) are listed in the spec §9b.

## Conventions

- Commit messages: plain human prose, no AI attribution trailers (user's global rule).
- Arabic copy is first-class, not a translation afterthought — every new user-facing string
  ships in both languages in the same change.
- Dates render Gregorian in Arabic too (`ar-SA-u-ca-gregory`), matching the prototype.
