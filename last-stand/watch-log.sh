#!/usr/bin/env bash
set -uo pipefail
LOG="/home/chicken/.local/share/Steam/steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/Player.log"
if [ "${1:-}" = "all" ]; then
  incus exec chen -- su chicken -s /bin/bash -c "tail -F '$LOG'"
else
  incus exec chen -- su chicken -s /bin/bash -c \
    "tail -F '$LOG' | grep --line-buffered -iE 'LastStand|Loading mod|Harmony|Exception|Error'"
fi
