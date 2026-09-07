#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Full-page screenshot matrix for the public site: every page x both
# cultures x both themes x three viewports, driven by playwright-cli.
#
# Usage:
#   tools/playwright/matrix.sh [BASE] [OUT]
#     BASE  base URL of a running server        (default http://localhost:5080)
#     OUT   directory to write screenshots into  (default $PWD/matrix)
#
# Run this from the scratchpad so the tool's own .playwright-cli/ session
# data does not land in the repo. Requires playwright-cli on PATH.
#
# Environment:
#   SESSION   playwright-cli session name (default matrix)
#   PAGES     space-separated page-path list, overrides the default below
#             (use "" for the home page)
#   CULTURES  space-separated cultures (default "en ar")
#   THEMES    space-separated themes   (default "dark light")
#   VIEWPORTS space-separated WxH sizes (default "390x844 1024x768 1440x900")
#
# Exits non-zero if any navigation or screenshot fails.
# ---------------------------------------------------------------------------
set -uo pipefail

if [ "${1:-}" = "-h" ] || [ "${1:-}" = "--help" ]; then
  sed -n '2,20p' "$0" | sed 's/^# \{0,1\}//'
  exit 0
fi

BASE="${1:-http://localhost:5080}"
OUT="${2:-$PWD/matrix}"
BASE="${BASE%/}"
SESSION="${SESSION:-matrix}"

if [ -n "${PAGES+x}" ]; then
  read -r -a PAGE_LIST <<< "$PAGES"
else
  # "" is the home page.
  PAGE_LIST=("" events events/riyadh-sprint-2026 events/jeddah-opener-2026 events/timeline join register training training/beginner-12-weeks rules governance governance/documents statistics news contact not-found)
fi
read -r -a CULTURE_LIST <<< "${CULTURES:-en ar}"
read -r -a THEME_LIST <<< "${THEMES:-dark light}"
read -r -a VP_LIST <<< "${VIEWPORTS:-390x844 1024x768 1440x900}"

if ! command -v playwright-cli >/dev/null 2>&1; then
  echo "playwright-cli not found on PATH" >&2
  exit 2
fi

mkdir -p "$OUT" || { echo "cannot create $OUT" >&2; exit 1; }

fail=0
shots=0

cleanup() { playwright-cli -s="$SESSION" close >/dev/null 2>&1 || true; }
trap cleanup EXIT

for vp in "${VP_LIST[@]}"; do
  w="${vp%x*}"
  h="${vp#*x}"
  if ! playwright-cli -s="$SESSION" resize "$w" "$h" >/dev/null 2>&1; then
    echo "resize $vp: FAILED (session may not be open yet, continuing)" >&2
  fi

  for culture in "${CULTURE_LIST[@]}"; do
    for theme in "${THEME_LIST[@]}"; do
      for page in "${PAGE_LIST[@]}"; do
        if [ -z "$page" ]; then
          url="$BASE/$culture?theme=$theme"
          slug="home"
        else
          url="$BASE/$culture/$page?theme=$theme"
          slug="${page//\//_}"
        fi

        dir="$OUT/$culture/$theme/$vp"
        mkdir -p "$dir"
        file="$dir/$slug.png"

        if ! playwright-cli -s="$SESSION" goto "$url" >/dev/null 2>&1; then
          echo "NAV FAIL  $culture $theme $vp $page -> $url" >&2
          fail=1
          continue
        fi

        # Re-assert the viewport after navigation in case the new document
        # reset it, then capture the full scrollable page.
        playwright-cli -s="$SESSION" resize "$w" "$h" >/dev/null 2>&1 || true

        if ! playwright-cli -s="$SESSION" screenshot --full-page --filename "$file" >/dev/null 2>&1; then
          echo "SHOT FAIL $culture $theme $vp $page -> $file" >&2
          fail=1
          continue
        fi

        shots=$((shots + 1))
        echo "ok  $culture $theme $vp ${page:-<home>} -> $file"
      done
    done
  done
done

echo
echo "$shots screenshot(s) written to $OUT"

if [ "$fail" -ne 0 ]; then
  echo "one or more pages failed to navigate or capture" >&2
  exit 1
fi
