# Saudi Triathlon Platform — Implementation Plan (v2, single ASP.NET Core app, 6 weeks)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Week 1 is written at bite-size; weeks 2–6 at task granularity with exact files, interfaces and acceptance checks — expand each week into a bite-size plan with superpowers:writing-plans on the Monday it starts.

**Goal:** Ship triathlon.sa as one deployable ASP.NET Core application — bilingual public site, content dashboard, small CRM, admin — in 6 weeks on a fixed budget, reusing the approved static prototype.

**Architecture:** One web project. `Areas/Public` = Razor Pages rendering the prototype's HTML with data from EF Core, output-cached and tag-evicted on publish. `Areas/Dashboard` = Blazor (interactive server) with MudBlazor for editing, CRM and admin. A handful of minimal-API endpoints serve the public JS (timeline data, registration POST, ICS). PostgreSQL by default, SQL Server by config.

**Tech Stack:** .NET 10 · Razor Pages · Blazor Web App (server) · MudBlazor · EF Core 10 (Npgsql / SqlServer) · ASP.NET Core Identity · Hangfire · MailKit · ImageSharp · QuestPDF · xUnit + Testcontainers · playwright-cli · GitHub Actions · IIS or Linux or Azure App Service.

**Spec:** `docs/superpowers/specs/2026-09-06-triathlon-platform-design.md` (v2)

## Global Constraints

- .NET SDK 10.0.x; `TreatWarningsAsErrors=true`; nullable on.
- DB provider chosen by `Database:Provider = Postgres | SqlServer`; migrations kept for both.
- Bilingual = paired `*En` / `*Ar` properties, both required unless marked optional; UI strings via `.resx` (`en`, `ar`).
- URL culture segment: `/en/...` and `/ar/...`; `dir` on `<html>` from culture; Gregorian dates in both.
- Public pages output-cached with tags; every publish evicts its tag. No page may read `DateTime.Now` uncached without `VaryByValue`.
- Every dashboard mutation writes `ActivityLog(User, Entity, Id, Action, At, Diff)`.
- Soft delete on all aggregates; global query filter.
- Public POSTs: antiforgery + rate limit 10/min/IP + Turnstile on registration.
- Budgets: LCP < 2.5 s (4G), public JS < 100 KB gz, Lighthouse a11y ≥ 95.
- Commits: plain prose, no AI attribution.

---

## Repository layout (target)

```
triathlon-sa/
├── src/Triathlon.Web/
│   ├── Triathlon.Web.csproj
│   ├── Program.cs
│   ├── Areas/Public/Pages/            # Index, Events/{Index,Detail,Timeline}, Join, Register, Training/{Index,Guide},
│   │                                  # Rules, Governance/{Index,Documents}, Statistics, News/{Index,Post}, Contact, Error
│   ├── Areas/Public/Shared/           # _Layout.cshtml, _Header, _Footer, _EventCard, _StatBand, _KingdomMap.svg
│   ├── Areas/Dashboard/               # App.razor, Routes.razor, Layout/, Pages/{Overview,Events,Cities,Documents,Rules,
│   │                                  # Guides,Statistics,Pages,News,Governance,Navigation,Media,Crm/*,Users,Activity,Settings}
│   ├── Areas/Dashboard/Components/    # BilingualField, RichTextEditor, MediaPicker, PublishBar, DataGrid wrappers
│   ├── Api/                           # MapPublicApi(): /api/timeline, /api/register, /api/events/{slug}/register, /api/calendar.ics
│   ├── Domain/                        # entities per module folder (Content, Events, Documents, Stats, Crm, Media, Identity)
│   ├── Data/                          # AppDbContext, configurations, Migrations/Postgres, Migrations/SqlServer, Seed/
│   ├── Services/                      # one service per aggregate + IFileStore, IEmailSender, CacheTags, LicenceNumbers
│   ├── Jobs/                          # StatsSnapshotJob, ArchiveEventsJob, SendEmailJob
│   ├── Resources/                     # Shared.en.resx, Shared.ar.resx
│   └── wwwroot/                       # css/site.css (from assets/css/main.css), js/site.js (from assets/js/app.js), fonts/, media/
├── tests/Triathlon.Tests/             # xUnit; Testcontainers Postgres; WebApplicationFactory
├── tools/playwright/                  # matrix.sh (playwright-cli screenshot matrix)
├── .github/workflows/ci.yml           # build, test, publish artifact
├── deploy/                            # web.config (IIS), triathlon.service (systemd), backup.ps1 / backup.sh, README
├── (prototype at root until cut-over; then removed)
└── docs/
```

## Endpoints & pages (summary)

| Public (Razor Pages, `/ {culture}/…`) | Data source | Cache tag |
|---|---|---|
| `/` | KPIs (showOnHome), next 3 events by type, latest news | `home`, `events`, `stats`, `news` |
| `/events?type=&scope=` | Events upcoming/past by type | `events` |
| `/events/{slug}` | Event + gallery + results + registration mode | `event:{slug}` |
| `/events/timeline?season=` | `/api/timeline` JSON consumed by `site.js` | `events` |
| `/join`, `/register` | Page blocks; register POST → `/api/register` | `page:join` |
| `/training`, `/training/{slug}` | TrainingGuide | `guides` |
| `/rules?audience=` | RuleOrGuide | `rules` |
| `/governance`, `/governance/documents?category=&year=` | People, Committees, Documents (+facets) | `governance`, `documents` |
| `/statistics` | KPIs, RegionStats, Growth | `stats` |
| `/news`, `/news/{slug}` | NewsPost | `news` |
| `/documents/{id}/download` | 302 to file, increments counter | — |

Dashboard (Blazor, `/dashboard/...`, cookie auth): one list + one edit page per aggregate; CRM
pages `athletes`, `athletes/{id}`, `clubs`, `registrations?event=`, `contacts`, `email`; admin
pages `users`, `activity`, `settings`.

## Week gates

| Week | Exit criteria |
|---|---|
| 1 | Staging site online (both DB providers migrate), login with seeded SuperAdmin, home page renders from DB with prototype look, CI green |
| 2–3 | All 10 sections live in ar + en from DB; Playwright matrix clean; Lighthouse mobile perf ≥ 85 / a11y ≥ 95 on home, events, event, governance — **client review 1** |
| 4 | Editor creates/edits/publishes every content type; publish visible ≤ 5 s; activity log records it |
| 5 | Registration → Pending athlete → approve → licence email; event entries export to Excel with Arabic intact; bulk email to a filtered group; users/roles/settings — **client review 2** |
| 6 | Production site live on Federation's server, backups verified by restore, training done, user guide (ar/en) delivered, warranty starts |

---

# Week 1 — Foundation (bite-size)

### Task 1.1: Solution, project, shared primitives

**Files:** Create `Triathlon.sln`, `Directory.Build.props`, `src/Triathlon.Web/Triathlon.Web.csproj`, `tests/Triathlon.Tests/Triathlon.Tests.csproj`, `src/Triathlon.Web/Domain/Common/{BaseEntity,PagedResult,Bilingual}.cs`

- [ ] **Step 1: Scaffold**

```bash
dotnet new sln -n Triathlon
dotnet new blazor -n Triathlon.Web -o src/Triathlon.Web --interactivity Server --auth Individual --all-interactive false
dotnet new xunit -n Triathlon.Tests -o tests/Triathlon.Tests
dotnet sln add src/Triathlon.Web tests/Triathlon.Tests
dotnet add tests/Triathlon.Tests reference src/Triathlon.Web
dotnet add src/Triathlon.Web package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Triathlon.Web package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/Triathlon.Web package MudBlazor
dotnet add src/Triathlon.Web package Hangfire.AspNetCore Hangfire.PostgreSql Hangfire.SqlServer
dotnet add src/Triathlon.Web package MailKit SixLabors.ImageSharp QuestPDF
dotnet add tests/Triathlon.Tests package Testcontainers.PostgreSql Microsoft.AspNetCore.Mvc.Testing
```

The `blazor` template with `--auth Individual` gives Identity + `ApplicationDbContext` + Razor
components; Razor Pages for the public area are added with `builder.Services.AddRazorPages()`
and `app.MapRazorPages()`.

- [ ] **Step 2: Directory.Build.props** — `net10.0`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`.
- [ ] **Step 3: Primitives**

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public sealed record PageQuery(int Page = 1, int PageSize = 20, string? Sort = null, string? Q = null)
{
    public int Skip => (Math.Max(Page, 1) - 1) * Take;
    public int Take => Math.Clamp(PageSize, 1, 100);
}
```

- [ ] **Step 4: Test** `PageQueryTests` (0→skip 0 take 20; 3,500→200,100; 2,0→1,1). Run `dotnet test` → PASS. **Commit** "Scaffold solution".

### Task 1.2: DbContext with provider switch, soft delete, stamps, both migration sets

**Files:** `Data/AppDbContext.cs`, `Data/StampInterceptor.cs`, `Data/DbSetup.cs` (`AddAppDatabase(config)`), `appsettings.json` (`Database:Provider`, `ConnectionStrings:Default`), `Data/Migrations/Postgres`, `Data/Migrations/SqlServer`

- [ ] **Step 1:** `AddAppDatabase`: read provider; `UseNpgsql(cs, o => o.MigrationsAssembly(..).MigrationsHistoryTable(..))` or `UseSqlServer`; add `StampInterceptor`. Migrations folders per provider via `MigrationsAssembly` + separate `IDesignTimeDbContextFactory` per provider (`PostgresContextFactory`, `SqlServerContextFactory`) so `dotnet ef migrations add X -o Data/Migrations/Postgres -- --provider Postgres` works.
- [ ] **Step 2:** `StampInterceptor` (Added → CreatedAt/By; Modified → UpdatedAt/By; Deleted → state Modified + DeletedAt). Global `DeletedAt == null` filter for all `BaseEntity` types in `OnModelCreating`.
- [ ] **Step 3:** Test with Testcontainers Postgres: `Deleting_hides_entity_and_sets_DeletedAt` using a small `Probe` entity registered only in the test context subclass. **Commit** "Database context with provider switch and soft delete".

### Task 1.3: Identity, roles, seed admin, activity log

**Files:** `Domain/Identity/{AppUser,ActivityLog}.cs`, `Services/ActivityLogger.cs`, `Data/Seed/SeedIdentity.cs`, dashboard `Layout/MainLayout.razor` (MudBlazor drawer with role-filtered menu), `Areas/Dashboard/Pages/Account/Login.razor` (template's Identity pages trimmed to Login/Logout/Manage password)

- [ ] Roles `SuperAdmin`, `Editor`, `CrmOfficer`; policies `Content` (SuperAdmin|Editor), `Crm` (SuperAdmin|CrmOfficer), `Admin` (SuperAdmin). Seed from `Seed:AdminEmail/AdminPassword` env only when no users. Lockout 5/15 min, password ≥ 12. `IActivityLogger.Log(entity, id, action, before?, after?)` writes JSON diff. Test: login page returns 200; `/dashboard` redirects anonymous to login; seeded admin can sign in (WebApplicationFactory + antiforgery cookie flow). **Commit** "Identity, roles and activity log".

### Task 1.4: Public area shell from the prototype

**Files:** `Areas/Public/Shared/_Layout.cshtml`, `_Header.cshtml`, `_Footer.cshtml`, `wwwroot/css/site.css` (copy of `assets/css/main.css`), `wwwroot/js/site.js` (copy of `assets/js/app.js` minus header/footer injection and minus `STF` data merge), `wwwroot/fonts/*`, `Program.cs` (request localization: cultures `en`, `ar`, `RouteDataRequestCultureProvider` on `{culture}` segment), `Areas/Public/Pages/Index.cshtml(.cs)` (static content for now), `Resources/Shared.{en,ar}.resx`

- [ ] Layout sets `<html lang="@culture" dir="@(culture=="ar"?"rtl":"ltr")" data-theme>`; language toggle links to the same route under the other culture (no localStorage needed); theme toggle stays client-side. Fix the two prototype bugs found by QA while porting: toggle label after `applyLang` (`docs/qa/2026-09-06-home-timeline-smoke.md` F1) and 1024 px header overflow (F2); zero Arabic letter-spacing on `.proto-note`/`.hero-kicker` (F3) — `.proto-note` is removed in production anyway.
- [ ] Playwright: `/en` and `/ar` render, `dir` correct, header identical to prototype at 390/1024/1440. **Commit** "Public layout ported from prototype".

### Task 1.5: Output caching, media store, email, Hangfire, health, CI, staging

**Files:** `Services/CacheTags.cs` (constants + `IOutputCacheStore.EvictByTagAsync` helper), `Program.cs` (`AddOutputCache` policies: `Public` 60 s tagged; `AddResponseCompression`; `MapStaticAssets`; rate limiter `public-post`; `AddHangfire` + dashboard under `Admin` policy; `AddHealthChecks` at `/health`), `Services/{IFileStore,LocalFileStore,ImageVariants}.cs`, `Services/{IEmailSender,SmtpEmailSender}.cs`, `.github/workflows/ci.yml` (build, test, `dotnet publish` artifact), `deploy/{web.config,triathlon.service,backup.sh,backup.ps1,README.md}`

- [ ] `LocalFileStore.SaveAsync(stream, name, kind)` → `media/{yyyy}/{MM}/{guid}.{ext}` + WebP variants 480/960/1600 for images; PDFs ≤ 25 MB; MIME sniffed. Test: upload PNG fixture → 4 files. `/health` 200. Deploy staging as second site on the Federation's server (or a temporary VPS if the server is not ready) with Basic-auth middleware when `Site:Staging=true`. **Commit** "Caching, media, email, jobs, CI and staging deploy".

**Week 1 exit:** gate row 1. Tag `v0.1.0`.

---

# Weeks 2–3 — Public website

### Task 2.1: Events domain + pages + timeline API + ICS
**Files:** `Domain/Events/{Event,City,EventRegistration,enums}.cs`, `Data/Configurations/Events*.cs`, `Data/Seed/SeedEvents.cs` (port 10 events + 8 cities from `assets/js/data.js`), `Services/EventsService.cs`, `Areas/Public/Pages/Events/{Index,Detail,Timeline}.cshtml(.cs)`, `Areas/Public/Shared/_EventCard.cshtml`, `_KingdomMap.cshtml` (SVG from `timeline.html`), `Api/PublicApi.cs` (`GET /api/timeline?season=`, `GET /api/calendar.ics?type=`, `POST /api/events/{slug}/register`), `wwwroot/js/timeline.js` (from prototype)
**Interfaces:** `EventsService.Upcoming(type?, page)`, `Past(type?, page)`, `BySlug(slug, culture)`, `Timeline(season)` → `{ cities[], events[] }`; `Register(slug, GuestRegistration)` → `Confirmed|Waitlist` by capacity. Cache tags `events`, `event:{slug}`.
**Tests:** upcoming/past boundary in Riyadh time; type filter; ICS has one VEVENT per event; capacity 2 → third is Waitlist; antiforgery/rate-limit 429 on 11th POST.

### Task 2.2: Documents, Rules, Training guides, Statistics, Governance, Pages, News
**Files:** `Domain/{Documents,Stats,Content}/*.cs`, configurations, seeds (from `data.js` + prototype copy), `Services/{DocumentsService,StatsService,ContentService}.cs`, pages `Governance/Index`, `Governance/Documents`, `Rules`, `Training/{Index,Guide}`, `Statistics`, `Join`, `Register`, `News/{Index,Post}`, `Contact`, `Error`, `Areas/Public/Shared/_StatBand.cshtml`, `_Blocks/*.cshtml` (hero, richText, steps, cards, faq, cta), `Api/PublicApi.cs` (`POST /api/register` athlete application → CRM Pending, Turnstile verified)
**Interfaces:** `DocumentsService.Query(category?, year?, page)` + `Facets()`; `Download(id)` increments and returns path; `StatsService.HomeKpis()`, `All()`; `ContentService.Page(slug, culture)` returns ordered blocks; `NewsService`.
**Tests:** facets counts; download increments; `showOnHome` ordering; page blocks validate both languages; register POST creates Pending athlete and sends email (fake sender).

### Task 2.3: Home page, SEO, perf hardening, Playwright matrix
**Files:** `Areas/Public/Pages/Index.cshtml(.cs)`, `Areas/Public/Shared/_Seo.cshtml` (title/description per culture, `hreflang` alternates, OG image, JSON-LD `SportsEvent` on event pages), `Areas/Public/Pages/Sitemap.cshtml` (`/sitemap.xml`), `robots.txt`, `tools/playwright/matrix.sh`
**Acceptance:** week 2–3 gate numbers; matrix (en/ar × dark/light × 390/1024/1440 × 11 pages) clean; report in `docs/qa/`.

**Client review 1** on staging. Tag `v0.2.0`.

---

# Week 4 — Content dashboard

### Task 3.1: Dashboard component kit
**Files:** `Areas/Dashboard/Components/{BilingualField,BilingualRichText,MediaPicker,FileUpload,PublishBar,ConfirmDialog,ActivityDrawer}.razor`, `wwwroot/js/editor.js` (Quill interop), `Areas/Dashboard/Layout/MainLayout.razor` (RTL toggle via MudBlazor `RightToLeft`)
**Interfaces:** `BilingualField(Label, @bind-En, @bind-Ar, Required)`; `MediaPicker(Kind, @bind-AssetId)`; `PublishBar(Status, OnSaveDraft, OnPublish, PreviewUrl, LastPublished)`.
**Tests:** bUnit: `BilingualField` shows validation when Ar empty and Required.

### Task 3.2: Content screens
**Files:** `Areas/Dashboard/Pages/{Events,Cities,Documents,Rules,Guides,Statistics,Pages,News,Governance,Navigation,Media,Settings}/{Index,Edit}.razor`, `Services/*` write methods (`Create/Update/Delete/Publish/Unpublish` + `ActivityLogger` + `CacheTags.Evict`)
**Acceptance (week 4 gate):** editor publishes an event with gallery + results PDF, a document, a KPI change, a news post, reorders navigation; each visible on the public site ≤ 5 s; activity log shows before/after; Editor role cannot open Users.

---

# Week 5 — CRM + admin

### Task 4.1: CRM domain + services
**Files:** `Domain/Crm/{Athlete,Club,Contact,ContactNote,EmailBlast,enums}.cs`, configurations (`NationalId` via `ValueConverter` + `IDataProtector` purpose `crm.nationalId`), `Services/{AthletesService,ClubsService,ContactsService,LicenceNumbers,LicenceCardPdf,EmailBlastService,ExcelExport}.cs` (ClosedXML; UTF-8 Arabic-safe), `Jobs/SendEmailJob.cs`
**Interfaces:** `AthletesService.Approve(id)` → licence `KSA-TRI-{yyyy}-{seq:00000}` + email with PDF; `Reject(id, reason)`; `Search(PageQuery, status?, club?, category?)`; `ExcelExport.Registrations(eventId)` → stream; `EmailBlastService.Send(filter, subject, bodyEn, bodyAr)` batches 100/min via Hangfire.
**Tests:** national ID encrypted at rest; 10 parallel approvals → unique sequential numbers; export opens with Arabic (UTF-8 check on sheet cell); blast skips `unsubscribed`.

### Task 4.2: CRM + admin screens
**Files:** `Areas/Dashboard/Pages/Crm/{Athletes,AthleteDetail,Clubs,Registrations,Contacts,ContactDetail,Email}.razor`, `Pages/{Users,Activity,Settings}.razor`, `Pages/Overview.razor` (pending applications, upcoming events fill, last blast)
**Acceptance (week 5 gate / client review 2):** approve a pending athlete and receive the card email; export an event's registrations to Excel; send an email to "athletes in Riyadh clubs"; invite a user with Editor role; disable a user. Tag `v0.3.0`.

---

# Week 6 — Go-live

### Task 5.1: Production site on the Federation's server
- IIS site or systemd unit from `deploy/`; production DB; `Site:Staging=false`; HTTPS (Let's Encrypt via win-acme / certbot, or Cloudflare); Turnstile keys; SMTP account; DNS cut-over (TTL lowered a day before); GitHub Pages prototype → redirect.
- **Acceptance:** `/ar` and `/en` 200 over HTTPS; SSL Labs A; uptime ping configured.

### Task 5.2: Backups, restore drill, runbook
- `deploy/backup.*` nightly (pg_dump or `BACKUP DATABASE`) to a second disk/blob, 30-day retention; restore once into a scratch DB and compare counts; `deploy/README.md` documents deploy, rollback, restore, add admin user.

### Task 5.3: Fixes, content load, training, handover
- Client review fixes; load final content; one 90-minute training session; `docs/user-guide/{en,ar}.pdf`; `docs/HANDOVER.md` (accounts, vendors, renewal dates). Tag `v1.0.0`. 60-day warranty log starts.

---

## Effort (internal)

Hour estimates per week and their mapping to the commercial packages live in
`docs/proposal/internal-effort.md` (not committed — the repository is public). Roughly 30 h/week
for 6 weeks for one developer. Feasible because the public site is a port of a finished prototype
and the dashboard uses MudBlazor grids/forms rather than custom UI. If the Federation's server is
late, staging runs on a temporary VPS for ≤ 2 weeks.

## Self-review against spec v2

- Brief points 01–10 → Tasks 1.4, 2.1–2.3 (public) and 3.2 (management). ✔
- Dashboard: content 3.1–3.2; CRM 4.1–4.2; admin (users, activity, settings) 4.2. ✔
- Flows §7: publish/evict (3.2 + 1.5), athlete registration (2.2 + 4.1), event entry (2.1 + 4.2), stats manual/computed (2.2 + `StatsSnapshotJob` in 1.5 jobs folder — add job in 2.2), documents counter (2.2). ✔
- Provider switch (Postgres/SqlServer) 1.2; staging + prod on one server 1.5/5.1; hosting paid by client — no cloud tasks. ✔
- Out-of-scope items appear nowhere. ✔
- Naming: `RegistrationMode` (Event), `RegistrationStatus` (EventRegistration), `AthleteStatus` (Athlete) — keep distinct.
