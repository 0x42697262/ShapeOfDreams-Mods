#!/usr/bin/env bash
set -euo pipefail
incus exec chen -- su chicken -s /bin/bash -c "
  export DOTNET_CLI_TELEMETRY_OPTOUT=1
  cd '/home/chicken/.local/share/Steam/steamapps/common/Shape of Dreams/Mods/LastStand'
  dotnet build -c '${1:-Debug}'
"
