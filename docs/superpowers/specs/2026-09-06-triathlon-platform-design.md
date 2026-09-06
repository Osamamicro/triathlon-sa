# Saudi Triathlon Federation Platform — Design Spec (v2)

Date: 2026-09-06 · Status: proposed (v2 after client-side feedback: simpler stack, 6 weeks)
Source brief: `Saudi_Triathlon_Website_Proposal.pdf` / Arabic edition (10 points, Aug 2026)
Prototype: static site at repo root (approved look & feel, all 10 points mocked)
Commercial terms: `docs/proposal/proposal-brief.md` (v2) — what was sold; this spec must not exceed it.

## 1. Goal

Replace the static prototype with a production site the Federation's staff run themselves: a
bilingual public website, a content-management dashboard, a small CRM (athletes, clubs,
registrations, contacts) and an admin panel. Fixed price (see the proposal brief, not committed),
**6 weeks**, one developer, one deployable application, hosting chosen and paid by the Federation.

## 2. Users & roles

| Actor | Needs | Surface |
|---|---|---|
| Public / athletes / media / partners | Find events, register, download rules & reports, learn how to join | Public site (mobile-first, ar/en) |
| Content editor | Publish events, news, documents, stats, guides, board pages | Dashboard → Content |
| Events / membership officer | Athletes, clubs, event registrations, contacts, bulk email | Dashboard → CRM |
| Super admin | Users, roles, settings, activity log | Dashboard → Admin |

Roles: `SuperAdmin`, `Editor`, `CrmOfficer`. Three is enough at this size.

## 3. Scope (in) — the 10 brief points + dashboard

Same mapping as `proposal-brief.md` §2–3. Dashboard = content management (all public content
types, bilingual, draft/publish, media upload), CRM (athletes, clubs, event registrations with
Excel export, contacts with notes, bulk email to a group), admin (users/roles, settings, activity log).

## 4. Scope (out) — priced as add-ons or not offered

Online payments, live results/timing import, Nafath identity, mobile apps, SMS campaigns
(email only in base), segments/campaign analytics, multi-environment cloud setups, KSA-residency
consulting. Hosting/domain/third-party accounts are the Federation's.

## 5. Stack decision (v2) — why one ASP.NET Core app

Options weighed: (a) .NET API + Next.js/React front-ends, (b) headless CMS, (c) **single ASP.NET
Core app with Razor Pages (public) + Blazor (dashboard)**. Client asked for the stack that is
easiest to configure and deploy with high performance, DB SQL Server or PostgreSQL, front-end
free choice. (c) wins: one `dotnet publish`, no Node build pipeline in production, IIS or Linux
or Azure App Service, server-rendered HTML is the fastest thing for a content site, and the
approved prototype's HTML/CSS/JS drops straight into Razor layouts. (a) was v1 of this spec — kept
in git history; it is the right call only if the site later needs a large SPA surface.

| Layer | Decision | Notes |
|---|---|---|
| App | ASP.NET Core 10 (LTS), single web project `Triathlon.Web` | Areas: `Public` (Razor Pages), `Dashboard` (Blazor Web App, interactive server), `Api` (minimal endpoints for the public JS: registration, timeline data, ICS) |
| Data | EF Core 10; provider switch by config: `Npgsql` (PostgreSQL 17, default) or `SqlServer` (SQL Server 2022 / Express) | One schema; migrations per provider in `Migrations/Postgres` and `Migrations/SqlServer`. Bilingual = paired `*En` / `*Ar` columns |
| Auth | ASP.NET Core Identity (cookie auth) for the dashboard; public anonymous | Lockout, password policy, optional TOTP later |
| Rendering perf | Output caching (`[OutputCache]` policies per page, tag-based eviction on publish) + response compression + static asset fingerprinting (`MapStaticAssets`) | No ISR/webhooks needed — same process |
| Files | Local disk under `wwwroot/media` by default (`IFileStore`), Azure Blob / S3 implementation behind the same interface | Images resized to WebP variants with ImageSharp on upload |
| Jobs | Hangfire (same DB) | Email sending, nightly stats snapshot, event auto-archive |
| Email | SMTP (Federation's M365 or any SMTP) via MailKit | No SMS in base |
| Search | DB `LIKE`/full-text per provider | Small data |
| Front-end | Prototype's `main.css` + `app.js` become `wwwroot/css/site.css` + `wwwroot/js/site.js`; Razor partials replace injected header/footer; bilingual by `IStringLocalizer` + `{En,Ar}` entity fields; culture from URL segment `/ar/...` `/en/...` | No SPA framework on the public site |
| Dashboard UI | Blazor + MudBlazor (tables, forms, dialogs, RTL support) | Rich text via a small TipTap/Quill JS interop component |
| Tests | xUnit + Testcontainers (Postgres) for services/endpoints; Playwright (`playwright-cli`) matrix for UI | |
| Deploy | `dotnet publish` → IIS site or Linux systemd/Docker; staging + production as two sites on the same server; GitHub Actions builds the artifact | Backups: nightly `pg_dump`/`sqlcmd BACKUP` to a second disk or blob, 30-day retention |
| Security | HTTPS/HSTS, antiforgery on all forms, rate limiting on public POSTs, Cloudflare Turnstile on the registration form, national ID encrypted at rest (Data Protection), activity log on dashboard mutations | PDPL basics: consent text, delete-on-request |

## 6. Domain model (unchanged in substance, trimmed)

```
Identity : AppUser, AppRole, ActivityLog
Content  : Page(slug, type, blocks: hero|richText|steps|cards|faq|cta), NewsPost, NavItem, SiteSetting, Person, Committee
Events   : Event(type Competition|Community, status, dateStart/End, city, venue, distances, categories, desc, hero, gallery[], resultsFile, registrationMode None|Internal|External, capacity)
           City(key, nameEn/Ar, svgX, svgY), EventRegistration(event, guest fields, category, status Pending|Confirmed|Waitlist|Cancelled)
Documents: Document(title, category, year, file, downloads, published), RuleOrGuide(title, audience[], file), TrainingGuide(title, chapters[])
Stats    : Kpi(key, label, value, suffix, order, showOnHome, source Manual|Computed), RegionStat, GrowthPoint
Crm      : Athlete(person, category, club, licenceNo, licenceStatus, nationalId enc, emergency), Club, Contact(type, tags, notes[]), EmailBlast(audience filter, subject, body, sentAt)
Media    : Asset(kind, path, variants[], alt, size)
```

Soft delete + Created/Updated stamps on all aggregates; activity log on every dashboard mutation.

## 7. Key flows

- **Publish:** editor saves draft → preview (`?preview=1` with dashboard cookie) → publish → output-cache tag evicted (`events`, `page:{slug}`, `stats`, …) → next request renders fresh.
- **Athlete registration:** public form → antiforgery + Turnstile + rate limit → `Athlete` Pending + confirmation email → officer approves in CRM → licence number `KSA-TRI-{yyyy}-{00000}` → email with PDF card (QuestPDF).
- **Event entry:** `Internal` → form on event page → `EventRegistration` (capacity → Waitlist) → CRM list, Excel export. `External` → link.
- **Statistics:** KPIs manual or computed (nightly Hangfire snapshot of athlete/registration counts).
- **Documents:** upload → category/year → publish → public filter grid; download counter.

## 8. Performance & quality budgets

Public pages: server-rendered, output-cached, LCP < 2.5 s on 4G, JS < 100 KB gz (prototype's
`app.js` is ~10 KB), Lighthouse a11y ≥ 95, fonts self-hosted and preloaded, images WebP with
`aspect-ratio`. Dashboard: server-side paging ≤ 50 rows.

## 9. Environments

One server. Two sites: `staging.triathlon.sa` (Basic-auth protected) and `triathlon.sa`, each with
its own DB. Setup effort is inside the "setup, deployment, training" package. Server
cost is the Federation's; a 2 vCPU / 4 GB Linux VPS or an Azure App Service B1 + SQL/Postgres
basic tier is enough for both sites.

## 9b. Current site & brand facts (reviewed 2026-09-06)

- triathlon.sa runs on **Wix** (site id `8ed0d5b3…`, multilingual en/ar, RTL on `/ar`). Home HTML ≈ 1.8 MB.
- Brand colour is **green `#008250`** with a swirl mark (`docs/proposal/assets/stf-mark.png`); the
  prototype's navy/teal "Night Race" palette is *not* the Federation's identity — re-skin during
  week 1 design direction (green primary, dark neutral, discipline hues only as secondary accents).
- Existing pages to migrate: Home, About (vision/mission/values/priorities, founded 2017, first
  board Sept 2021), News (blog posts, RSS `/blog-feed.xml`), Events (empty calendar), National
  Championship Series (banner only), Qualification Standing, Gallery (`/portfolio`), Media Center,
  High Performance (Elite Pathway Program: U15/U17/18+, Category 1/2, EOI form, PDFs), Athlete
  Selection Criteria, EPP Athletes, National Coaching Framework (Foundation / Level 1 / Level 2,
  30 % female target, annual re-accreditation), FAQ, Contact. Socials: Instagram/X/TikTok
  `@triathlonksa`, LinkedIn `company/triathlonksa`, YouTube. HQ: Prince Faisal Bin Fahad Olympic
  Complex, Riyadh.
- Home stats are static text: 19 local championships · 2,202 participants · 49 national medals.
- Missing entirely today: documents library, rules, training guide, per-event pages, athlete registration.
- Content types the CMS must therefore also cover beyond the brief: News, Gallery albums, FAQ,
  Media Center (press releases), High-Performance pages with PDF downloads, Coaching Framework.

## 10. Risks

| Risk | Mitigation |
|---|---|
| Content not ready | Dashboard lets staff load content from week 4; placeholders flagged |
| Scope creep | Add-on list in the financial proposal; change requests estimated first |
| Hosting decision late | App runs on IIS, Linux or App Service unchanged; provider switch is config |
| Arabic/RTL regressions | Logical CSS only (already true in prototype); Playwright matrix per locale |
| 6-week window | Public site is a port of a finished prototype; CMS/CRM kept to fixed content types |
