---
name: stf-polish
description: "Orchestrates the Saudi Triathlon design-polish loop — architect blueprint (if needed) → stf-frontend-designer / stf-dashboard-ux implement → stf-visual-qa gate → stf-design-critic jury → fix round. Use when asked to make a page, section, or dashboard screen award-winning, or to run a full polish pass before a client demo. Usage: /stf-polish <target = home|events|timeline|event|join|register|training|rules|governance|stats|admin|all> [rounds=2]"
---

# STF polish loop

You are the orchestrator. You do not design or code yourself; you dispatch the project agents
(Agent tool, `subagent_type` = names below) and keep the loop honest. Effort discipline: designers
and critic on `opus`, QA on `sonnet` (their defaults), max **2 rounds** unless the user raises it.

## Inputs

`target` = one page/screen or `all`; optional `rounds` (default 2). Public pages → designer =
`stf-frontend-designer`; `admin` → designer = `stf-dashboard-ux`; `all` → both, in parallel.

## Steps

1. **Serve** the prototype (`python -m http.server 8000`, background) or the app (`dotnet run --project src/Triathlon.Web`).
   Confirm `http://localhost:8000/` returns 200 before dispatching anyone.
2. **Baseline gate** — dispatch `stf-visual-qa` on the target. Keep its report path; P0s here are
   fixed first by the designer in round 1.
3. **Round N (N = 1..rounds):**
   a. Dispatch the designer(s) with: target, QA report path, critic's "Top 5 moves" from the
      previous round (round 1: none), and the rule "one memorable idea, both locales, evidence".
   b. Dispatch `stf-visual-qa` again on the changed pages. Any new P0/P1 → send back to the
      designer via SendMessage before the jury sees it.
   c. Dispatch `stf-design-critic`. If every screen is AWARD-READY, stop early.
4. **Architecture check** (only if a designer reported "Data/API needs"): dispatch
   `stf-architect` with those needs; attach its ADR path to the summary.
5. **Summary to user:** scoreboard before → after, files changed (from designers' reports),
   remaining Top moves not done, screenshot paths for the best before/after pair, and the
   commands to re-run QA. Do not commit unless asked.

## Rules

- Never let the critic and designer be the same agent instance; fresh dispatch each round.
- Dispatch public-site and dashboard designers in parallel; never two designers on the same file.
- If QA cannot run (browser install fails), stop and report — do not proceed on unverified work.
