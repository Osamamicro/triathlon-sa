# Handover — start Week 1 of the platform build

Written 2026-09-06 at the end of the planning session. Read this first, then `CLAUDE.md`, then
the plan. Everything below is true as of the last commit on branch `docs/platform-plan-and-agents`
(PR #1 to `main`).

## 1. Where things stand

| Item | State |
|---|---|
| Static prototype (repo root) | Approved look & feel, live on GitHub Pages. Do not restyle it; it becomes the source for the Razor port. |
| Design spec | `docs/superpowers/specs/2026-09-06-triathlon-platform-design.md` (v2) — decisions final unless the client changes hosting/DB |
| Implementation plan | `docs/superpowers/plans/2026-09-06-triathlon-platform-ultra-plan.md` — **Week 1 is at step level, start there** |
| Proposals (EN) | v3 sent to the client via the agency; awaiting confirmation. Files live in `docs/proposal/` (gitignored — public repo) |
| Proposals (AR) | **Not built yet.** Build only after the client confirms the English pair. Generator `docs/proposal/build-proposal-ar.js` exists but still carries the v1 content and navy theme; it must be reworked to the v3 brief and green theme. |
| QA baseline | `docs/qa/2026-09-06-home-timeline-smoke.md` — 3 confirmed prototype defects (F1 toggle label, F2 1024 px overflow, F3 Arabic letter-spacing). Fix them during the Week 1 port, not in the prototype. |
| Agents | `.claude/agents/stf-*` + `/stf-polish`. Loaded automatically in a new session. |
| Branch | Work on `docs/platform-plan-and-agents` until PR #1 merges, then branch from `main` per week: `feat/week-1-foundation`, … |

## 2. Decisions already made (do not reopen without a reason)

- One ASP.NET Core 10 web app (`src/Triathlon.Web`): Razor Pages public site + Blazor (interactive server, MudBlazor) dashboard. No SPA framework, no Node build in production.
- EF Core with provider switch: PostgreSQL default, SQL Server supported. Migrations kept for both.
- Bilingual = paired `*En` / `*Ar` properties; URL culture segment `/en/` `/ar/`; Gregorian dates in both.
- Output caching by tag, evicted on publish. Hangfire for jobs. Local disk file store behind `IFileStore` (blob later).
- Roles: `SuperAdmin`, `Editor`, `CrmOfficer`.
- Brand: Federation green `#008250` + swirl mark (`docs/proposal/assets/stf-mark.png`, local only — ask the client for the official vector). The prototype's navy/teal palette is an exploration; Week 1 design direction re-skins to green while keeping the layout, motion and discipline colour code as secondary accents.
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

## 7. After Week 1

Week 2–3 (public site) tasks are at task level in the plan; expand each into step-level with
`superpowers:writing-plans` on the day it starts. Client review 1 is at the end of Week 3 on the
staging site. The Arabic proposal build is independent of the code and can run in parallel the
moment the English pair is confirmed.
