#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")" && pwd)"
name="LastStand"
dll="$root/bin/Release/netstandard2.1/$name.dll"
out="$root/dist/$name"
id_file="about/publishedfileid.txt"
note_file="about/changenote.txt"

if [ ! -f "$dll" ]; then
  echo "Missing $dll. Build Release first: dotnet build -c Release" >&2
  exit 1
fi

if [ -f "$out/$id_file" ] && ! cmp -s "$out/$id_file" "$root/$id_file"; then
  cp "$out/$id_file" "$root/$id_file"
  echo "Saved the Workshop item id to $id_file. Commit it."
fi

rm -rf "$out"
mkdir -p "$out/about"
cp "$dll" "$out/"
cp "$root/about/icon.png" "$root/about/preview.png" "$root/about/description.txt" "$out/about/"
if [ -f "$root/$id_file" ]; then
  cp "$root/$id_file" "$out/$id_file"
fi
if [ -s "$root/$note_file" ]; then
  cp "$root/$note_file" "$out/$note_file"
else
  echo "No $note_file: the next upload goes up without a change note." >&2
fi

sed -E 's|"assemblies": *\[[^]]*\]|"assemblies": [ "'"$name"'.dll" ]|' \
  "$root/about/metadata.json" > "$out/about/metadata.json"
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
