#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
magick raw/camp.png -alpha off -crop 1000x947+300+110 +repage -resize 680x644! -modulate 112,108 t1.png
magick raw/arena.png -alpha off -crop 802x760+395+150 +repage -resize 680x644! t2.png
magick raw/wave.png -alpha off -crop 700x663+620+290 +repage -resize 680x644! t3.png
magick raw/upgrade.png -alpha off -crop 802x760+330+110 +repage -resize 680x644! t4.png
magick raw/horde.png -alpha off -crop 1143x600+240+160 +repage -resize 940x494! -modulate 108,112 gameplay.png
magick raw/horde.png -alpha off -crop 1600x900+160+90 +repage -resize 1280x720! -blur 0x6 -modulate 60,90 bgshot.png
