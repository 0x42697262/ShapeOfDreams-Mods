#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
python3 icon/icon.py
rsvg-convert -w 512 -h 512 icon/icon.svg -o ../about/icon.png
python3 preview/preview.py
rsvg-convert -w 1024 -h 576 preview/preview.svg | magick - -alpha off -define png:compression-level=9 ../about/preview.png
rm preview/preview.svg
