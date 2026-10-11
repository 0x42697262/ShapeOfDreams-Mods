#!/usr/bin/env bash
set -uo pipefail
source "$(dirname "$0")/tools/env.sh"
cd "$SOD_ROOT"

baseline="tools/duplicates.txt"
allowed_literals='/src/(Config|Text|Identity)/|/src/Game/(GameNames|SharedGameNames)\.cs$'

if [ "${1:-}" = "--update-baseline" ]; then
  python3 tools/dupscan.py "$SOD_ROOT" "$baseline" --update
  exit
fi

sources="library/src $(for mod in $(sod_mods); do printf '%s/src ' "$mod"; done)"
failed=0

echo "== comments"
if grep -rnE '//|/\*' --include='*.cs' $sources; then
  echo "comments: remove them; the reasons go in docs/."
  failed=1
else
  echo "comments: none."
fi

echo "== string literals"
stray="$(grep -rl '"' --include='*.cs' $sources | grep -vE "$allowed_literals" || true)"
if [ -n "$stray" ]; then
  echo "$stray"
  echo "string literals: move them to Text/, Config/ (Tuning), Identity/ or Game/GameNames.cs."
  failed=1
else
  echo "string literals: only in the allowed places."
fi

echo "== duplicates"
python3 tools/dupscan.py "$SOD_ROOT" "$baseline" || failed=1

echo "== build"
output="$(./build.sh all Debug 2>&1)"
echo "$output" | grep -E '^== |error|warning CS' | sed "s|$SOD_ROOT/||g" | sort -u
if echo "$output" | grep -qE 'error|warning CS|Build FAILED'; then
  echo "build: fix the errors and warnings above."
  failed=1
else
  echo "build: every mod built with 0 warnings and 0 errors."
fi

if [ "$failed" -ne 0 ]; then
  echo "check: FAILED"
  exit 1
fi
echo "check: passed"
