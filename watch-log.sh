#!/usr/bin/env bash
set -uo pipefail
source "$(dirname "$0")/tools/env.sh"

if [ "${1:-all}" = "all" ]; then
  sod_in_container "tail -F '$SOD_LOG'"
  exit
fi

name="$(sod_project "$1")" || exit 1
lower="$(echo "$name" | tr '[:upper:]' '[:lower:]')"
sod_in_container "tail -F '$SOD_LOG' | grep --line-buffered -iE '$name|$lower|Loading mod|Harmony|Exception|Error'"
