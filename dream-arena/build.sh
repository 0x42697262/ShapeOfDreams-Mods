#!/usr/bin/env bash
# Build Dream Arena. This directory is bind-mounted into the container's Mods/.
set -euo pipefail
incus exec chen -- su chicken -s /bin/bash -c "
  export DOTNET_CLI_TELEMETRY_OPTOUT=1
  cd '/home/chicken/.local/share/Steam/steamapps/common/Shape of Dreams/Mods/DreamArena'
  dotnet build -c '${1:-Debug}'
"
