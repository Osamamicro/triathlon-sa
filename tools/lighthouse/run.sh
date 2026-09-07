#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Lighthouse mobile budget gate for the public site.
#
# Runs Lighthouse against the four representative public pages in both
# cultures — home, the events index, one event detail page and governance —
# and fails if any of them is below the Weeks 2-3 gate: performance >= 85 and
# accessibility >= 95, both on the simulated mobile profile.
#
# Simulated throttling is noisy, so every page is measured twice and the
# better of the two runs is the one that is judged. Both numbers are printed
# so a borderline page is visible rather than hidden behind a lucky run.
#
# Usage:
#   tools/lighthouse/run.sh [BASE] [OUT]
#     BASE  base URL of a running server   (default http://localhost:5080)
#     OUT   directory for the JSON reports (default $PWD/lighthouse)
#
# Environment:
#   RUNS         measurements per page (default 2)
#   MIN_PERF     performance floor       (default 85)
#   MIN_A11Y     accessibility floor     (default 95)
#   CHROME_PATH  browser binary; defaults to Playwright's Chromium, then to
#                CHROME_PATH/msedge/chrome already on the machine
#   PAGES        space-separated path list, overrides the default eight
#
# Serve a Release publish, not `dotnet run` — the development build ships
# unminified assets and the browser-refresh script and measures nothing real:
#
#   dotnet publish src/Triathlon.Web -c Release -o /tmp/publish
#   cd /tmp/publish && ASPNETCORE_ENVIRONMENT=Production \
#     ASPNETCORE_URLS=http://localhost:5080 \
#     Database__MigrateOnStartup=true Database__SeedContent=true \
#     Seed__AdminEmail=admin@triathlon.sa Seed__AdminPassword='<a strong one>' \
#     dotnet Triathlon.Web.dll
# ---------------------------------------------------------------------------
set -uo pipefail

if [ "${1:-}" = "-h" ] || [ "${1:-}" = "--help" ]; then
  sed -n '2,40p' "$0" | sed 's/^# \{0,1\}//'
  exit 0
fi

BASE="${1:-http://localhost:5080}"
OUT="${2:-$PWD/lighthouse}"
RUNS="${RUNS:-2}"
MIN_PERF="${MIN_PERF:-85}"
MIN_A11Y="${MIN_A11Y:-95}"

BASE="${BASE%/}"
mkdir -p "$OUT" || { echo "cannot create $OUT" >&2; exit 1; }

DEFAULT_PAGES="en en/events en/events/riyadh-sprint-2026 en/governance ar ar/events ar/events/riyadh-sprint-2026 ar/governance"
read -r -a PAGE_LIST <<< "${PAGES:-$DEFAULT_PAGES}"

# Lighthouse needs a real Chromium. Playwright's is the one the rest of the QA
# tooling already uses, so prefer it and fall back to whatever is installed.
find_chrome() {
  if [ -n "${CHROME_PATH:-}" ] && [ -x "${CHROME_PATH}" ]; then echo "$CHROME_PATH"; return 0; fi
  local pw
  pw=$(node -e "try{console.log(require('playwright').chromium.executablePath())}catch(e){}" 2>/dev/null)
  if [ -n "$pw" ] && [ -f "$pw" ]; then echo "$pw"; return 0; fi
  for c in \
    "/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe" \
    "/c/Program Files/Google/Chrome/Application/chrome.exe" \
    "$(command -v google-chrome 2>/dev/null)" \
    "$(command -v chromium 2>/dev/null)"; do
    [ -n "$c" ] && [ -f "$c" ] && { echo "$c"; return 0; }
  done
  return 1
}

CHROME="$(find_chrome)" || { echo "no Chromium found — set CHROME_PATH or 'npm i playwright && npx playwright install chromium'" >&2; exit 2; }
echo "browser: $CHROME"
echo "base:    $BASE"
echo "out:     $OUT"
echo

# Fail early and clearly rather than reporting eight zeroes against a dead port.
if ! curl -fsS -o /dev/null "$BASE/en"; then
  echo "$BASE/en is not answering — start the published site first" >&2
  exit 2
fi

fail=0
printf '%-38s %-7s %-7s %-7s %-9s %-7s %-7s\n' page run perf a11y LCP CLS TBT
printf '%.0s-' {1..80}; echo

for page in "${PAGE_LIST[@]}"; do
  slug="${page//\//_}"
  best_perf=-1; best_a11y=-1
  for run in $(seq 1 "$RUNS"); do
    report="$OUT/$slug.run$run.json"
    npx --yes lighthouse "$BASE/$page" \
      --form-factor=mobile \
      --screenEmulation.mobile \
      --throttling-method=simulate \
      --only-categories=performance,accessibility \
      --chrome-path="$CHROME" \
      --chrome-flags="--headless=new --no-sandbox --disable-dev-shm-usage" \
      --output=json --output-path="$report" --quiet >/dev/null 2>&1

    if [ ! -s "$report" ]; then
      echo "  $page run $run: lighthouse produced no report" >&2
      fail=1
      continue
    fi

    line=$(REPORT="$report" node -e '
      const r = JSON.parse(require("fs").readFileSync(process.env.REPORT, "utf8"));
      const s = c => Math.round((r.categories[c]?.score ?? 0) * 100);
      const a = id => r.audits[id]?.displayValue ?? "-";
      console.log([s("performance"), s("accessibility"),
        a("largest-contentful-paint"), a("cumulative-layout-shift"),
        a("total-blocking-time")].join("|"));') || { fail=1; continue; }

    IFS='|' read -r perf a11y lcp cls tbt <<< "$line"
    printf '%-38s %-7s %-7s %-7s %-9s %-7s %-7s\n' "/$page" "$run" "$perf" "$a11y" "$lcp" "$cls" "$tbt"
    [ "$perf" -gt "$best_perf" ] && best_perf="$perf"
    [ "$a11y" -gt "$best_a11y" ] && best_a11y="$a11y"
  done

  status="ok"
  if [ "$best_perf" -lt "$MIN_PERF" ]; then status="FAIL perf < $MIN_PERF"; fail=1; fi
  if [ "$best_a11y" -lt "$MIN_A11Y" ]; then status="${status/ok/} FAIL a11y < $MIN_A11Y"; fail=1; fi
  printf '%-38s %-7s %-7s %-7s %s\n\n' "/$page" "best" "$best_perf" "$best_a11y" "$status"
done

if [ "$fail" -ne 0 ]; then
  echo "budget not met" >&2
  exit 1
fi
echo "all pages meet performance >= $MIN_PERF and accessibility >= $MIN_A11Y"
