# Saudi Triathlon Federation — Website Prototype

A modern, fully bilingual (Arabic / English, with RTL) website prototype for the
Saudi Triathlon Federation, built to the Federation's August 2026 redevelopment
brief. Static site — no build step, no backend — hosted on GitHub Pages.

> **Design prototype.** All figures, documents, results and names are illustrative
> placeholders. The registration and dashboard demos store data in the visitor's
> browser only (`localStorage`) and never transmit anything.

**Live:** https://osamamicro.github.io/triathlon-sa/

## What's inside

| Page | Purpose |
|------|---------|
| `index.html` | Landing page — hero, live stat counters, upcoming events, quick paths |
| `events.html` | Events & calendar with a competition / community split and filters |
| `timeline.html` | Interactive season timeline wired to an SVG map of the Kingdom |
| `event.html` | Per-event template — info, registration, gallery, results |
| `join.html` | Become an athlete — steps, categories, clubs, national-team pathway |
| `training.html` | Beginner training guide (12-week plan) |
| `rules.html` | Rules & regulations with downloadable PDFs |
| `governance.html` | Governance & a documents library filterable by category and year |
| `stats.html` | Federation statistics dashboard |
| `register.html` | Online athlete registration flow (demo) |
| `admin.html` | **Content-management dashboard** — add / edit / delete events, statistics, documents, rules and clubs; every change flows to the public pages |

## How it works

- **Design system** — `assets/css/main.css`. A dark "Night Race" identity with a
  swim / bike / run colour code, plus a light "Race Day" theme toggled from the
  header (sun/moon). Theme and language choices persist per visitor; append
  `?theme=light` to force light mode.
- **Content source** — `assets/js/data.js` holds every editable value (events,
  statistics, documents, rules, clubs). In production this single file is replaced
  by a headless CMS / dashboard API.
- **Dashboard overlay** — the dashboard writes a `localStorage` overlay that
  `data.js` merges over the base content via `STF.mergeOverlay`, so edits made in
  `admin.html` appear across the whole site (in that browser).
- **App shell** — `assets/js/app.js` injects the shared header/footer, handles the
  language and theme toggles, animations, and event-card rendering.

## Production roadmap

See `PLAN.md`. In short: **Phase A** (this static prototype) → **Phase B**
(headless CMS / dashboard API replacing the storage layer) → **Phase C** (athlete
accounts, online registration and payments, live results).

## Local preview

```bash
python3 -m http.server 8000
# then open http://localhost:8000/
```

## Deployment

GitHub Pages, served from the `main` branch root
(Settings → Pages → Deploy from a branch → `main` / `root`). A `.nojekyll`
file keeps the `assets/` folders served as-is.
