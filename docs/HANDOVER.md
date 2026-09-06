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
> Read `docs/HANDOVER.md`, `CLAUDE.md`, Week 4 of the plan. Branch `feat/week-4-dashboard`. Expand
> 3.1–3.2 to step level, execute. Use `stf-dashboard-ux` for the Blazor/MudBlazor screens and
> `stf-visual-qa` (logged in, LTR + RTL, 1024 + 1440). Gate: editor publishes an event with gallery
> and results PDF, a document, a KPI change, a news post, reorders navigation; each visible on the
> public site within 5 s; activity log shows before/after; Editor role cannot open Users. Chain (§7).

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

## 10. After Week 1

Week 2–3 (public site) tasks are at task level in the plan; expand each into step-level with
`superpowers:writing-plans` on the day it starts. Client review 1 is at the end of Week 3 on the
staging site. The Arabic proposal build is independent of the code and can run in parallel the
moment the English pair is confirmed.
