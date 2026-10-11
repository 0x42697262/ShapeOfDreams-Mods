#!/usr/bin/env bash
SOD_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOD_CONTAINER="${SOD_CONTAINER:-chen}"
SOD_USER="${SOD_USER:-$(id -un)}"
SOD_LOG="${SOD_LOG:-/home/$SOD_USER/.local/share/Steam/steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/Player.log}"

sod_mods() {
  for dir in "$SOD_ROOT"/*/; do
    dir="${dir%/}"
    if [ -f "$dir/about/metadata.json" ]; then
      basename "$dir"
    fi
  done
}

sod_project() {
  local dir="$SOD_ROOT/$1"
  if [ ! -f "$dir/about/metadata.json" ]; then
    echo "Unknown mod '$1'. Mods: $(sod_mods | tr '\n' ' ')" >&2
    return 1
  fi
  local project
  project="$(cd "$dir" && ls ./*.csproj 2>/dev/null | head -1)"
  if [ -z "$project" ]; then
    echo "No .csproj in $1" >&2
    return 1
  fi
  basename "$project" .csproj
}

sod_in_container() {
  incus exec "$SOD_CONTAINER" -- su "$SOD_USER" -s /bin/bash -c "$1"
}
