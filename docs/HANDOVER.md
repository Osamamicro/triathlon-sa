# Handover — platform build (session chain)

Written 2026-09-06 at the end of the planning session; §1 and §9 are refreshed at the end of every
stage (last: 2026-09-08, end of Weeks 2–3). Read this first, then `CLAUDE.md`, then the plan.

## 1. Where things stand

| Item | State |
|---|---|
| Static prototype (repo root) | Approved look & feel, live on GitHub Pages. Do not restyle it; it becomes the source for the Razor port. |
| Design spec | `docs/superpowers/specs/2026-09-06-triathlon-platform-design.md` (v2) — decisions final unless the client changes hosting/DB |
| Implementation plan | `docs/superpowers/plans/2026-09-06-triathlon-platform-ultra-plan.md` — Weeks 1–3 done. Step-level plans for the finished stage: `docs/superpowers/plans/2026-09-07-task-2.{1,2,3}-*.md`. Week 4 is at task level; expand with `superpowers:writing-plans` before coding. |
| Production code (Weeks 1–3) | `src/Triathlon.Web` + `tests/Triathlon.Tests` (solution `Triathlon.slnx`). 215 tests green on PostgreSQL and SQL Server (CI runs both). Every public page renders from the database in `en` + `ar` via `Bi(en, ar)` (single-culture markup): home (CMS `home` page + KPIs + events + news), events list/detail/guest entry, season timeline + Kingdom map, join, athlete registration (Pending athlete + email, Turnstile when configured), training + guides, rules, governance + documents library (facets, counting downloads), statistics, news, contact, branded 404/500. Minimal APIs: `/api/timeline`, `/api/calendar.ics`, `POST /api/events/{slug}/register`, `POST /api/register`, `/documents|rules|training/{id}/download`, `/sitemap.xml`, `/robots.txt`. Security headers + CSP `script-src 'self'`, per-scope DB provider resolution, migration bundles in CI, bilingual dashboard sign-in (cookie culture). Redesign around the Federation brand (`docs/design/2026-09-07-redesign.md`). Lighthouse mobile perf 96–99 / a11y 100 on the gate pages (`docs/qa/lighthouse/2026-09-07-summary.md`). |
| Proposals (EN) | v3 sent to the client via the agency; awaiting confirmation. Files live in `docs/proposal/` (gitignored — public repo) |
| Proposals (AR) | **Not built yet.** Build only after the client confirms the English pair. Generator `docs/proposal/build-proposal-ar.js` exists but still carries the v1 content and navy theme; it must be reworked to the v3 brief and green theme. |
| QA | Weeks 2–3: `docs/qa/2026-09-07-week2-3-public-site.md` (full matrix en/ar × dark/light × 390/1024/1440; 0 P0/P1 open; P2-17/18/21 deferred — see §9), design jury `docs/qa/2026-09-07-design-critique-redesign.md` (12/14 P0+P1 fixed in the polish round, the rest in the QA fix round), Lighthouse `docs/qa/lighthouse/2026-09-07-summary.md`. Week 1: `docs/qa/2026-09-07-week1-public-shell.md` (all carry-overs closed). |
| Agents | `.claude/agents/stf-*` + `/stf-polish`. Loaded automatically in a new session. Model policy: Fable for planning/rulings/adversarial review, sonnet implementers, never haiku. |
| Branch | `feat/week-4-dashboard` (from `main` after PR #5), pushed, **no PR yet — Week 4 is mid-stage**. Plan 3.1 (dashboard foundations) is complete and final-reviewed at `178ddfc`; plan 3.2 Task A (page chrome + Events/Cities screens) is implemented at `a8666b9` and awaits its task review. 320 tests green on PostgreSQL. Resume with the "Week 4 — resume" prompt in §8. |

## 2. Decisions already made (do not reopen without a reason)

- One ASP.NET Core 10 web app (`src/Triathlon.Web`): Razor Pages public site + Blazor (interactive server, MudBlazor) dashboard. No SPA framework, no Node build in production.
- EF Core with provider switch: PostgreSQL default, SQL Server supported. Migrations kept for both.
- Bilingual = paired `*En` / `*Ar` properties; URL culture segment `/en/` `/ar/`; Gregorian dates in both.
- Output caching by tag, evicted on publish. Hangfire for jobs. Local disk file store behind `IFileStore` (blob later).
- Roles: `SuperAdmin`, `Editor`, `CrmOfficer`.
- Brand: Federation green `#008C3D` (sampled from the official lock-up on triathlon.sa; the `#008250` in earlier notes was an estimate) + the official swirl mark, fetched from the live site into `src/Triathlon.Web/wwwroot/img/brand/` (raster; still ask the client for the vector). The site is a redesign of the brand's presence, not a copy of the Wix site: green is the ground and the only general accent in both themes, Tajawal is the display face in both scripts, and the swim/bike/run colours are a strict code used only where a discipline is named. Rationale and token table: `docs/design/2026-09-07-redesign.md`.
- Scope guardrails: fixed page block types (no page builder), no payments, no live timing, email only (no SMS), staging + production as two sites on one server.

## 3. Open decisions to get from the client at kick-off

1. Database: PostgreSQL or SQL Server? (Default PostgreSQL if no answer.)
2. Hosting: their server (IIS or Linux) or Azure App Service? Who owns the account?
3. Official logo files and brand guide, photography, approved Arabic copy.
4. Wix admin access or a content export for migration (news, About, EPP pages, coaching framework, FAQ, gallery).
5. Who the dashboard users are (names, roles).

Until 1–2 are answered, build with PostgreSQL via Testcontainers/Docker locally; nothing in Week 1 depends on the hosting choice.

## 4. First commands for the new session

```bash
git fetch origin && git checkout docs/platform-plan-and-agents   # or main after PR #1 merges
dotnet --list-sdks          # need 10.0.x
docker --version            # for Testcontainers (Postgres) in tests
playwright-cli --version    # visual QA tooling already installed
```

Then follow the plan, Task 1.1 → 1.5, one task per commit. Each task lists files, interfaces,
the failing test, and the acceptance check. Expand any task that feels under-specified with
`superpowers:writing-plans` before coding, do not improvise the structure.

## 5. Suggested kick-off prompt for the new session

> Read `docs/HANDOVER.md`, `CLAUDE.md`, and the Week 1 section of
> `docs/superpowers/plans/2026-09-06-triathlon-platform-ultra-plan.md`. Create branch
> `feat/week-1-foundation` from the current branch. Execute Tasks 1.1 through 1.5 with
> `superpowers:subagent-driven-development`, one commit per task, tests green before each commit.
> Use PostgreSQL locally. When Task 1.4 (public layout port) is done, run `stf-visual-qa` on `/en`
> and `/ar` at 390/1024/1440 and confirm the three baseline defects from
> `docs/qa/2026-09-06-home-timeline-smoke.md` are gone. Stop at the Week 1 gate and report.

## 6. Things that bit us this session (avoid repeating)

- New `.claude/agents` definitions load only at session start; a freshly written agent cannot be dispatched in the same session.
- `playwright-cli` writes a `.playwright-cli/` folder in the cwd — run it from the scratchpad; it is gitignored anyway.
- LibreOffice and `pdftoppm` are not installed; Word COM (PowerShell/pywin32) and PyMuPDF are the working substitutes for docx → pdf → png.
- Word treats `jc=right` on bidi paragraphs as "end"; use `AlignmentType.START` for Arabic docx-js paragraphs.
- The repo is public. Never commit anything under `docs/proposal/` or the client PDFs at root; `.gitignore` enforces it.
- Commit messages: plain prose, no AI attribution trailers (user's global rule).
- Docker Desktop on this machine crashes at start-up with "The file cannot be accessed by the system" on stale AF_UNIX socket files (`%LOCALAPPDATA%\Docker\run\dockerInference`, `%LOCALAPPDATA%\docker-secrets-engine\engine.sock`). Fix: stop Docker, rename the parent folder (`run` → `run.stale.*`), start again. Testcontainers then works.
- The dashboard auth cookie is `Secure`-only: run the app with the **https** launch profile (or `--urls https://localhost:7052`) or the seeded login will not stick over plain http.
- `dotnet-ef` 10.0.5 warns about the 10.0.11 runtime on every command; harmless. Migrations: `dotnet ef migrations add <Name> --context PostgresDbContext -o Data/Migrations/Postgres` and the same with `SqlServerDbContext` / `SqlServer` — always add both.
- The bootstrap the `dotnet new blazor` template vendors (60k lines) was added in Task 1.1 and deleted in 1.3; per-task review diffs that include `wwwroot/lib` are unreadable — exclude that path from review packages.
- Every dashboard mutation later must call `IActivityLogger.LogAsync`; it saves the shared scoped `AppDbContext`, so call it as the single save of the unit of work (or refactor to Add-only first).
- Deferred review findings from Week 1 (media/decode test fixture, rate-limit "unknown" bucket, EvictAsync partial failure, backup.ps1 layout, Shared.cs resx marker naming, public 404 page, favicon) are listed in the Week 1 PR description — pick them up in Weeks 2–3 where the touched area comes up.
- Run the suite against SQL Server with `STF_TEST_DB=SqlServer dotnet test` (CI does this on every push); Postgres remains the default.

## 7. Session chain protocol (every session must do this)

The build runs as a chain of sessions, one per plan stage, so context never overflows and each
stage starts fresh from written state. A session is **not finished** until all four steps are done:

1. **Gate check** — the stage's exit criteria in the plan are met with evidence (tests green,
   `stf-visual-qa` report in `docs/qa/`, screenshots read). If the gate is not met, do not chain;
   report what is missing.
2. **Write state** — update the table in §1 of this file ("Where things stand"), add a dated
   "Stage N done" line to §9 below with the commit SHA and the QA report path, and refresh the
   memory file `triathlon-session-state` so the next session knows the stage.
3. **Commit + push** on the stage branch (`feat/week-N-…`) and open or update its PR to `main`.
   Plain-prose commit messages, no attribution trailers.
4. **Spawn the next session** with the `spawn_task` tool: title `Week N+1: <stage name>`,
   prompt = the next stage's kick-off text from §8, self-contained (paths, branch names, gate).
   The user clicks the chip to start it. If `spawn_task` is unavailable, print the kick-off
   prompt as the last message so the user can paste it.

Stages and their kick-off prompts are in §8. A session that finishes early may continue into the
next stage in the same session only if context is still small; otherwise chain.

## 8. Stage kick-off prompts

**Week 1 — Foundation** (Tasks 1.1–1.5): the prompt in §5.

**Weeks 2–3 — Public website** (Tasks 2.1–2.3):
> Read `docs/HANDOVER.md`, `CLAUDE.md`, and the Weeks 2–3 section of the plan. Branch
> `feat/week-2-public-site` from `main` (after the Week 1 PR merged) or from the Week 1 branch.
> First expand Tasks 2.1–2.3 into step level with `superpowers:writing-plans` (one file per task
> under `docs/superpowers/plans/`), then execute with `superpowers:subagent-driven-development`.
> Port every prototype page to Razor Pages reading from the database; seed from `assets/js/data.js`.
> Use `stf-frontend-designer` for the green re-skin (Week 1 design direction) and `stf-visual-qa`
> for the full matrix on all 11 pages. Gate: all 10 brief sections in ar + en, Lighthouse mobile
> perf ≥ 85 / a11y ≥ 95 on home, events, event, governance. Then run the session chain protocol (§7).

**Week 4 — Content dashboard** (Tasks 3.1–3.2):
> Read `docs/HANDOVER.md` (§1, §2, §6, §7, §9 — the Week 4 carry-overs), `CLAUDE.md`, Week 4 of
> the plan, and skim the three Weeks 2–3 step plans for the domain/services you will edit. Branch
> `feat/week-4-dashboard` from `main` once the Weeks 2–3 tail PR has merged (else from
> `feat/week-2-public-site`). Expand 3.1–3.2 to step level with `superpowers:writing-plans`, execute
> with `superpowers:subagent-driven-development` (Fable for planning/rulings/reviews, sonnet
> implementers, never haiku). Use `stf-dashboard-ux` for the Blazor/MudBlazor screens and
> `stf-visual-qa` (logged in, LTR + RTL, 1024 + 1440). First commits, before any screen: (1) the
> eviction contract — every dashboard save evicts its tags (`site` for navigation/settings,
> `page:{slug}`, `events` + `event:{slug}`, `stats`, `news`, `documents`, `rules`, `guides`,
> `clubs`) with `page:{slug}`/`site` eviction tests; (2) `OutputCacheSetup` base policy
> `SetVaryByQuery([])` so only pages that declare `VaryByQueryKeys` vary; (3) HTML sanitiser on
> save for every `Html.Raw` field (block bodies, the home hero title and lead, news bodies, guide
> chapters); (4) file-path validation on save (site-relative `/docs/` or `/media/` only) and a
> local-redirect guard on the download endpoints; (5) `DeleteAsync` with the ADR 0001 cascade and
> restore for Page, TrainingGuide, NewsPost, Document, RuleOrGuide, Club, Athlete, with a test
> each; (6) a `PublicSite.ReservedSlugs` list shared by routing, sitemap and slug validation. Also
> in scope: dashboard i18n beyond the sign-in pages; nav `aria-current` longest match; the
> confirmation name via TempData instead of the query string; athlete dedupe by email; the
> `PublicApi`/test-helper/category-map consolidation; footer year from `TimeProvider`; ICS
> `DTEND`/`DTSTAMP`; the `Kpi.Source = Computed` nightly job; email via a Hangfire job; a
> structural seed (navigation, pages, committees, KPI keys) separate from the demo seed so
> production boots with a header. Gate: editor publishes an event with gallery and results PDF,
> a document, a KPI change, a news post, reorders navigation; each visible on the public site
> within 5 s; activity log shows before/after; Editor role cannot open Users. Chain (§7).

**Week 4 — resume (mid-stage, written 2026-09-08)**:
> Read `docs/HANDOVER.md` (§1, §6, §7, §9 last line), `CLAUDE.md`, and the two Week 4 plans
> `docs/superpowers/plans/2026-09-08-task-3.1-dashboard-foundations.md` (its "Decisions" section) and
> `docs/superpowers/plans/2026-09-08-task-3.2-content-screens.md`. Check out `feat/week-4-dashboard`
> (worktree `.claude/worktrees/optimistic-jemison-ed0907` if it still exists, else the main checkout).
> Plan 3.1 is done. Continue plan 3.2 with `superpowers:subagent-driven-development` from its ledger
> `.superpowers/sdd/2026-09-08-task-3.2-content-screens/progress.md` (gitignored; if missing, recreate
> it from this section): Task A is committed at `a8666b9` and needs its task review (diff package
> `review-178ddfc..a8666b9.diff` in that folder, brief `task-A-brief.md`, report `task-A-report.md`);
> then Tasks B (Documents/Rules/Guides/Media screens), C (Pages blocks editor, News, Governance,
> Navigation, Statistics, Settings), D (Overview, featured event card P2-18, `stf-dashboard-ux` polish),
> E (`stf-visual-qa` logged in LTR+RTL 1024+1440 → `docs/qa/2026-09-08-week4-dashboard.md`, both-provider
> test run, migration sync, gate evidence, then §7 chain: HANDOVER §1/§9, memory, PR to `main`, spawn
> "Week 5: CRM + admin"). Model policy for this stage (user, efficient mode): sonnet implementers and
> re-reviews, opus for task reviews of pattern-setting work (A, C) and the final whole-branch review,
> never haiku. Rules that bind the screens (from the 3.1 final review and ledger): every save runs in
> its own DI scope (`Areas/Dashboard/DashboardScope.cs`); refusals render as `L[ex.Key, ex.Arguments]`
> on `ex.Field`; `Delete*/Restore*` are silent on unknown ids (re-read the list); `SlugField
> Reserved="false"` for events/news/rules/guides; `@key` on every `BilingualRichText`; the Pages screen
> must not offer `governance`/`statistics` slugs; the Statistics screen parks a KPI (uncheck
> `ShowOnHome`) before moving another onto its slot; category chips reject commas;
> `MediaService.ListAsync(kind, deletedOnly, skip, take)`. Strip any `Co-Authored-By` trailer a subagent
> adds (`git commit --amend`) before pushing. Gate (unchanged): an Editor publishes an event with
> gallery and results PDF, a document, a KPI change, a news post, reorders navigation; each visible on
> the public site within 5 s; activity drawer shows before/after; Editor cannot open Users; `dotnet
> test` green on both providers; both migration sets in sync.

**Week 5 — CRM + admin** (Tasks 4.1–4.2):
> Read `docs/HANDOVER.md`, `CLAUDE.md`, Week 5 of the plan. Branch `feat/week-5-crm`. Expand
> 4.1–4.2, execute. Gate: public registration → Pending athlete → approve → licence email with PDF
> card; event registrations export to Excel with Arabic intact; bulk email to a filtered group;
> invite/disable users. `stf-design-critic` reviews public + dashboard before client review 2. Chain (§7).

**Week 6 — Go-live** (Tasks 5.1–5.3):
> Read `docs/HANDOVER.md`, `CLAUDE.md`, Week 6 of the plan. Branch `feat/week-6-golive`. Produce
> `deploy/` for the client's chosen host, backup + restore drill documented in `deploy/README.md`,
> `docs/user-guide/{en,ar}.pdf`, `docs/HANDOVER-CLIENT.md`. Run `claude-security:scan` and fix
> highs. Gate: production checklist in Task 5.1 complete. This is the final stage: close the chain
> with a final summary instead of spawning.

**Parallel track — Arabic proposals** (only after the user says the English pair is confirmed):
> Rework `docs/proposal/build-proposal-ar.js` to the v3 brief in `docs/proposal/proposal-brief.md`
> and the green theme used by `build-proposal-en.js`; produce `العرض_الفني_…docx` and
> `العرض_المالي_…docx` + PDFs; verify every page rendered (RTL, no name, no VAT, totals). Do not commit
> (public repo).

## 9. Stage log

- 2026-09-06 — Planning done. Commits `bd0843e`, `b85d3ab` on `docs/platform-plan-and-agents` (PR #1). No code yet.
- 2026-09-07 — **Stage 1 (Week 1 — Foundation) done.** Branch `feat/week-1-foundation`, code head `0e6fa84` (Tasks 1.1–1.5 + two review fix waves), PR to `main` open. 80 tests green against Testcontainers Postgres; live gate verified (`/health` 200, `/en` ltr + `/ar` rtl, seeded SuperAdmin login → `/dashboard`, Hangfire `/dashboard/jobs` admin-only, WebP variants, CI file, both migration sets in sync). QA: `docs/qa/2026-09-07-week1-public-shell.md` (F1/F2/F3 fixed). Not done: staging deployment (no server yet). Carried into Weeks 2–3: public 404 page, single-culture server markup (`@Bi(en, ar)`), bilingual login page before client review 1, SQL Server Testcontainers CI job, `site.js` innerHTML hardening, soft-delete cascade decision, migration bundles in CI, security headers + `AllowedHosts`, QA N2/N3, dashboard i18n in Week 4.

- 2026-09-08 — **Stage 2 (Weeks 2–3 — Public website) done.** Branch `feat/week-2-public-site`; commits through `e93cee1` merged in PR #4; the perf/QA tail (`4bd2812..f13e3fe`, message-rewritten, no trailers) goes to `main` in PR #5 (https://github.com/Osamamicro/triathlon-sa/pull/5). 215 tests green on PostgreSQL and SQL Server; both migration sets in sync (`Events`, `Documents`, `Content`, `Statistics`); Lighthouse mobile perf 96–99 / a11y 100 on home, events, event, governance in both cultures; QA matrix 0 P0/P1 open. Gate met per the whole-branch final review. Decisions recorded in the three step plans (2.1 §Decisions 1–13, 2.2 §Decisions 1–9) plus: full brand redesign instead of a token re-skin (user request); forms live on uncached pages and POST to minimal APIs; invalid posts round-trip via TempData (`FormRoundTrip`); DB provider resolved per scope; JSON-LD emitted through one `Html.Raw` with `<` escaped and `Markup.HasInlineScript` as the CSP test contract; `Kpi.HomeOrder`; `EventCategories` display labels; ADR 0001 soft-delete cascade; integration tests run on a pinned clock (`WebAppFixture.FixedNow` = 2026-09-07 09:00 Riyadh). Carried into Week 4 (in the §8 prompt): eviction contract + tests, base cache policy `SetVaryByQuery([])`, HTML sanitiser on save, file-path validation + local-redirect guard, `DeleteAsync` cascades for the new aggregates, reserved slugs, dashboard i18n, `aria-current` longest match, TempData for the confirmation name, athlete dedupe, `PublicApi` consolidation, footer year via `TimeProvider`, ICS `DTEND`, computed KPIs job, email via Hangfire, licence uniqueness (Week 5), structural vs demo seed split, featured-event layout (QA P2-18). Ask the client for review 1: board member names/terms (P2-17), contact channels — phone, hours, form (P2-21), Cloudflare Turnstile keys (go-live prerequisite: without them registration mail is rate-limited only), brand vector + photography, staging host/`AllowedHosts`/proxy answer, SMTP credentials, real club list and content (production runs with `SeedContent=false`; staging for review 1 must run with `Database__SeedContent=true`). Not done: staging deployment (no host yet).

- 2026-09-08 — **Stage 3 (Week 4 — Content dashboard) IN PROGRESS**, paused mid-stage. Branch `feat/week-4-dashboard`, head `a8666b9` (pushed). Plan 3.1 complete and whole-branch-reviewed at `178ddfc`: reserved slugs + `Slugs` rule, `ContentGuard` (HtmlSanitizer allow-list, `/docs|/media` path guard, download guard), `ContentCommit`/`Audit` unit of work (single save through `IActivityLogger`, then tag eviction; `Audit.Snapshot` includes child count + hash), write sides with delete/restore for pages, blocks, navigation, committees, clubs, settings, news, events, cities, documents, rules, guides, media, KPIs, regions, growth, athletes; `SiteSettings` and `MediaAssets` migrations (both providers); `ComputedKpisJob` (nightly, `0 23 * * *` UTC) and `EmailJob` via Hangfire (`InlineJobClient` in tests); structural seed (`Database:SeedStructure`, default true) split from the demo seed; footer year from `TimeProvider`; TempData confirmation name; ICS `DTEND` + generation-time `DTSTAMP`; `aria-current` longest match; `FormPosts` consolidation; bilingual MudBlazor shell (`MudRTLProvider`, IA groups, 130+ resx keys) and the component kit (`BilingualField`, `BilingualRichText` on vendored Quill 2.0.3, `SlugField`, `MediaPicker`, `FileUpload`, `PublishBar`, `ConfirmDialog`, `ActivityDrawer`) with bUnit 2.9.0. Plan 3.2 Task A (`EntityTable`, `EditShell`, `DashboardScope`, Events + Cities screens) implemented at `a8666b9`, review pending; Tasks B–E not started. 320 tests green (PostgreSQL; SQL Server last confirmed at Task F, 298). Decisions: plan 3.1 "Decisions" 1–19 plus the rulings in the ledger (exception `Validation_*` resx keys, `deletedOnly`, validate-before-mutate, one `DeletedAt` per aggregate delete, `IgnoreQueryFilters` on every uniqueness and seed guard, per-save DI scope, `CompanionPageSlugs` = home/rules/training, opus/sonnet review split). Week 5 carry-overs from the 3.1 final review: `target=_blank` without `rel=noopener` in sanitised HTML; `ContentGuard` as singleton; contact settings cannot be blanked; comma in event categories (chips reject it); coverage gaps (child upsert-by-id, `RestoreCityAsync`, `SetPublishedAsync(false)`, multi-day timed ICS, `SlugField`/`ConfirmDialog`/diff parser); `Validation_File` interpolates the English store message; home-band KPI swap needs a reorder action; recurring job registered inside `MapAppJobsDashboard`; `EmailJob` argument carries applicant PII in Hangfire storage (enqueue the athlete id instead); `ActivityQuery.LatestAsync` unfiltered by policy; `BilingualRichText.DisposeAsync` catches only `JSDisconnectedException`; Quill loads on the login page; media hard-delete/orphan sweep; per-input error anchoring in `EditShell`; "Save draft" label on publish-less screens. Deploy note for Week 6: migrate before first start (the structural seed runs on every boot). Client asks unchanged from Stage 2.

## 10. After Week 1

Week 2–3 (public site) tasks are at task level in the plan; expand each into step-level with
`superpowers:writing-plans` on the day it starts. Client review 1 is at the end of Week 3 on the
staging site. The Arabic proposal build is independent of the code and can run in parallel the
moment the English pair is confirmed.
