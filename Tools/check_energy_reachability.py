#!/usr/bin/env python3
"""
check_energy_reachability.py — auditerar att alla energi-pickups i spelbanorna
är nåbara. Om någon energi inte går att nå är 100 % / perfekt slut omöjligt.

Hur det funkar:
  * Energi-koordinaterna läses direkt ur Models/Map.cs (regex på
    ItemFactory.Create(ItemType.Energi, X, Y, ..., ID)), grupperade per Map-klass —
    så verktyget håller sig aktuellt om placeringar ändras i koden.
  * Kollisionslagret läses ur varje Tiled-JSON (AttributeIndex = 0/1, ≠0 = solid,
    indexerat [y*width + x] — samma tolkning som TiledMapRepository).
  * En flood-fill körs från banans spawn med spelets rörelseregler: gång/luftkontroll
    horisontellt, fall, och hopp upp till 2 tiles. Modellen är medvetet generös
    (över-approximerar nåbarhet), så en energi som floden INTE når är definitivt onåbar.

Kör:  python Tools/check_energy_reachability.py
"""
import json
import os
import re
from collections import deque

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
MAP_CS = os.path.join(ROOT, "FrostyPlatformer", "Models", "Map.cs")
TILED = os.path.join(ROOT, "FrostyPlatformer", "Resources", "Assets", "MapData", "Tiled")

# Karta -> (Tiled-JSON, spawn-tile). Spawn hämtat från TiledWorldMapSystem.GetStageEntry.
MAPS = {
    "MapOne":   ("mapone.json",   (2, 23)),
    "MapTwo":   ("maptwo.json",   (2, 23)),
    "MapThree": ("mapthree.json", (2, 20)),
    "MapFour":  ("mapfour.json",  (2, 3)),
    "MapFive":  ("mapfive.json",  (2, 33)),
    "MapSix":   ("mapsix.json",   (2, 22)),
    "MapSeven": ("mapseven.json", (3, 18)),
    "MapEight": ("mapeight.json", (4, 41)),
}

ENERGI_RE = re.compile(
    r"Create\(\s*ItemType\.Energi\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,[^)]*?,\s*(\d+)\s*\)")
CLASS_RE = re.compile(r"class (Map\w+)\s*:\s*Map")


def parse_energies():
    """Returnerar {mapnamn: [(id, x, y), ...]} extraherat ur Map.cs."""
    with open(MAP_CS, encoding="utf-8-sig") as f:
        src = f.read()
    classes = sorted((m.start(), m.group(1)) for m in CLASS_RE.finditer(src))

    def map_at(pos):
        name = None
        for start, cname in classes:
            if start <= pos:
                name = cname
            else:
                break
        return name

    result = {}
    for m in ENERGI_RE.finditer(src):
        x, y, eid = int(m.group(1)), int(m.group(2)), int(m.group(3))
        result.setdefault(map_at(m.start()), []).append((eid, x, y))
    return result


def load_collision(jsonfile):
    with open(os.path.join(TILED, jsonfile), encoding="utf-8-sig") as f:
        m = json.load(f)
    coll = next(l["data"] for l in m["layers"] if l.get("name") == "Collision")
    return m["width"], m["height"], coll


def solid(coll, w, h, x, y):
    if x < 0 or y < 0 or x >= w or y >= h:
        return False
    return coll[y * w + x] != 0


def reachable(coll, w, h, spawn):
    sx, sy = spawn
    while sy < h and solid(coll, w, h, sx, sy):   # hjälten faller till mark
        sy += 1
    seen = set()
    if sy >= h:
        return seen
    dq = deque([(sx, sy)])
    seen.add((sx, sy))
    while dq:
        x, y = dq.popleft()
        cand = [(x - 1, y), (x + 1, y), (x, y + 1), (x, y - 1)]
        if not solid(coll, w, h, x, y - 1):       # hopp 2 kräver öppet mellansteg
            cand.append((x, y - 2))
        for nx, ny in cand:
            if (nx, ny) not in seen and not solid(coll, w, h, nx, ny) and 0 <= ny < h and 0 <= nx < w:
                seen.add((nx, ny))
                dq.append((nx, ny))
    return seen


def main():
    energies = parse_energies()
    total, bad = 0, []
    for name, (jsonfile, spawn) in MAPS.items():
        w, h, coll = load_collision(jsonfile)
        reach = reachable(coll, w, h, spawn)
        elist = sorted(energies.get(name, []))
        total += len(elist)
        unreach = [(eid, x, y) for (eid, x, y) in elist if (x, y) not in reach]
        status = "OK" if not unreach else f"{len(unreach)} ONABAR"
        print(f"{name:8} ({jsonfile:14} {w}x{h}, spawn {spawn}): {len(elist):2} energier -> {status}")
        for eid, x, y in unreach:
            off = " (off-map)" if (y >= h or x >= w) else ""
            print(f"    #{eid} ({x},{y}){off}")
            bad.append((name, eid, x, y))
    print(f"\nTotalt {total} energier. Onabara: {len(bad)}.")
    if not bad:
        print("Alla energier nabara -> 100% / perfekt slut ar mojligt.")


if __name__ == "__main__":
    main()
