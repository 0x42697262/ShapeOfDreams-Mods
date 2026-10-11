#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/tools/env.sh"

if [ $# -lt 1 ]; then
  echo "Usage: ./build.sh <mod|all> [Debug|Release]" >&2
  echo "Mods: $(sod_mods | tr '\n' ' ')" >&2
  exit 1
fi

configuration="${2:-Debug}"
if [ "$1" = "all" ]; then
  mods="$(sod_mods)"
else
  mods="$1"
fi

for mod in $mods; do
  name="$(sod_project "$mod")"
  echo "== $name ($configuration)"
  sod_in_container "export DOTNET_CLI_TELEMETRY_OPTOUT=1 PATH=\$PATH:\$HOME/.dotnet; cd '$SOD_ROOT/$mod' && dotnet build -c '$configuration' -nologo -v q"
done
