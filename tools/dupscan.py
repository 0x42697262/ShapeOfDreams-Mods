import collections
import os
import re
import sys

WINDOW = 6
TRIVIAL = re.compile(r"^(using |namespace |\[|$)|^[{}();,]*$")
MOD_NAMES = re.compile(r"\b(LastStand|DreamArena|Arena|SodLib)\w*")


def normalize(line):
    return MOD_NAMES.sub("X", line.strip())


def windows(path):
    lines = []
    for number, raw in enumerate(open(path, encoding="utf-8"), 1):
        line = normalize(raw)
        if not TRIVIAL.match(line):
            lines.append((number, line))
    for i in range(len(lines) - WINDOW + 1):
        yield lines[i][0], "\n".join(line for _, line in lines[i:i + WINDOW])


def source_files(root):
    for top in sorted(os.listdir(root)):
        src = os.path.join(root, top, "src")
        if not os.path.isdir(src):
            continue
        for folder, _, files in os.walk(src):
            for name in sorted(files):
                if name.endswith(".cs"):
                    yield os.path.relpath(os.path.join(folder, name), root)


def scan(root):
    seen = collections.defaultdict(list)
    for path in source_files(root):
        for line, text in windows(os.path.join(root, path)):
            seen[text].append((path, line))
    pairs = collections.Counter()
    for places in seen.values():
        files = sorted({path for path, _ in places})
        for i in range(len(files)):
            for j in range(i + 1, len(files)):
                pairs[(files[i], files[j])] += 1
    return pairs


def main():
    root = sys.argv[1]
    baseline_path = sys.argv[2]
    update = len(sys.argv) > 3 and sys.argv[3] == "--update"
    pairs = scan(root)
    current = {f"{a} {b}": n for (a, b), n in pairs.items()}
    if update:
        with open(baseline_path, "w", encoding="utf-8") as out:
            for key in sorted(current):
                out.write(f"{current[key]} {key}\n")
        print(f"duplicates: baseline written, {len(current)} file pair(s), {sum(current.values())} window(s).")
        return 0
    baseline = {}
    if os.path.exists(baseline_path):
        for row in open(baseline_path, encoding="utf-8"):
            count, key = row.rstrip("\n").split(" ", 1)
            baseline[key] = int(count)
    grown = [(key, baseline.get(key, 0), n) for key, n in sorted(current.items()) if n > baseline.get(key, 0)]
    shrunk = [key for key, n in baseline.items() if current.get(key, 0) < n]
    for key, before, after in grown:
        a, b = key.split(" ")
        print(f"duplicates: {a} and {b} share {after} window(s) of {WINDOW} lines (baseline {before}).")
    if grown:
        print("duplicates: move the shared code into library/src, or reuse the existing helper.")
        return 1
    if shrunk:
        print(f"duplicates: {len(shrunk)} pair(s) dropped below the baseline. Run ./check.sh --update-baseline.")
    print(f"duplicates: ok, {sum(current.values())} known window(s) in {len(current)} file pair(s).")
    return 0


sys.exit(main())
