#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/tools/env.sh"

if [ $# -ne 1 ]; then
  echo "Usage: ./pack.sh <mod>   (build it first: ./build.sh <mod> Release)" >&2
  echo "Mods: $(sod_mods | tr '\n' ' ')" >&2
  exit 1
fi

mod_dir="$SOD_ROOT/$1"
name="$(sod_project "$1")"
dll="$mod_dir/bin/Release/netstandard2.1/$name.dll"
out="$mod_dir/dist/$name"
id_file="about/publishedfileid.txt"
note_file="about/changenote.txt"

if [ ! -f "$dll" ]; then
  echo "Missing $dll. Build Release first: ./build.sh $1 Release" >&2
  exit 1
fi

if [ -f "$out/$id_file" ] && ! cmp -s "$out/$id_file" "$mod_dir/$id_file"; then
  cp "$out/$id_file" "$mod_dir/$id_file"
  echo "Saved the Workshop item id to $1/$id_file. Commit it."
fi

rm -rf "$out"
mkdir -p "$out/about"
cp "$dll" "$out/"
cp "$mod_dir/about/icon.png" "$mod_dir/about/preview.png" "$mod_dir/about/description.txt" "$out/about/"
if [ -f "$mod_dir/$id_file" ]; then
  cp "$mod_dir/$id_file" "$out/$id_file"
fi
if [ -s "$mod_dir/$note_file" ]; then
  cp "$mod_dir/$note_file" "$out/$note_file"
else
  echo "No $note_file: the next upload goes up without a change note." >&2
fi

sed -E 's|"assemblies": *\[[^]]*\]|"assemblies": [ "'"$name"'.dll" ]|' \
  "$mod_dir/about/metadata.json" > "$out/about/metadata.json"
if ! grep -q "\"$name.dll\"" "$out/about/metadata.json"; then
  echo "Could not point metadata.json at $name.dll" >&2
  exit 1
fi

echo "Packed $out:"
(cd "$out" && find . -type f | sort | sed 's|^\./|  |')
if [ -f "$out/$note_file" ]; then
  echo "Change note for the next upload ($note_file):"
  sed 's|^|  |' "$out/$note_file"
fi
