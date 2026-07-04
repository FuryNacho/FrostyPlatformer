"""
generate_seal.py — Genererar "Remastered Edition"-sigillet för startskärmen.

Skapar en PNG (RGBA, 96×96 spelpixlar) i Resources/Assets/Sprites/:
  splash_remastered_seal.png

Designen är en pastisch på "Official Nintendo Seal of Quality"-emblemet som
satt på NES-kassetter: en spikig silver-stjärna (sunburst) med en medaljong i
mitten där texten REMASTERED / EDITION står i borstat silver.

Körs med: python generate_seal.py
Kräver: Pillow  (pip install Pillow)

Rendering i spelet:
  Ritas ovanpå splashstart.png via DrawSprite (RGBA-alpha komponeras av
  SpriteBatch i AlphaBlend-läge). Placeras nere till höger i splash-rutan.
"""

import os
import math
from PIL import Image, ImageDraw

# ── Mål-katalog ──────────────────────────────────────────────────────────────
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_DIR    = os.path.join(SCRIPT_DIR, "..", "FrostyPlatformer",
                          "Resources", "Assets", "Sprites")
os.makedirs(OUT_DIR, exist_ok=True)

S       = 88                 # Canvas-sida (spelpixlar). 4× i MonoGame → 352 px.
CX = CY = S / 2.0            # Mittpunkt
SPIKES  = 16                 # Antal stjärnpiggar
R_OUTER = 42                 # Yttre radie (piggarnas spets)
R_INNER = 34                 # Inre radie (piggarnas bas)
R_MEDAL = 33                 # Medaljongens ytterkant (ring)
R_FACE  = 32                 # Medaljongens inre yta (textbakgrund)


# ── Silverpalett ─────────────────────────────────────────────────────────────
# Vertikal metallramp för stjärnan (ljus topp → mörk botten).
SILVER_RAMP = [
    (0.00, (242, 246, 250)),   # högdager
    (0.30, (206, 214, 224)),
    (0.58, (166, 178, 191)),
    (0.80, (120, 132, 146)),
    (1.00, ( 86,  96, 108)),   # skugga
]
STAR_OUTLINE = (34, 39, 47)    # mörk kant runt stjärnan

# Medaljongens ansikte (mörk slate så silvertexten lyser).
FACE_TOP = (52, 60, 74)
FACE_BOT = (28, 34, 44)
RING_LIGHT = (214, 222, 232)   # blank ring-högdager (topp)
RING_DARK  = ( 62,  70,  82)   # ring-skugga (botten)

# Text — borstat silver med liten vertikal lyster + mörk relief-skugga.
TEXT_TOP = (238, 243, 249)
TEXT_BOT = (176, 188, 202)
TEXT_SHADOW = (16, 20, 28)


def ramp_color(ramp, t):
    """Linjär interpolation i en färgramp (lista av (pos, rgb))."""
    t = max(0.0, min(1.0, t))
    for i in range(len(ramp) - 1):
        p0, c0 = ramp[i]
        p1, c1 = ramp[i + 1]
        if p0 <= t <= p1:
            f = (t - p0) / (p1 - p0) if p1 > p0 else 0.0
            return tuple(int(c0[k] + (c1[k] - c0[k]) * f) for k in range(3))
    return ramp[-1][1]


def star_points(spikes, r_out, r_in):
    """Vertexlista för en spikig stjärna, första spetsen rakt uppåt."""
    pts = []
    for i in range(spikes * 2):
        ang = -math.pi / 2 + i * math.pi / spikes
        r   = r_out if i % 2 == 0 else r_in
        pts.append((CX + r * math.cos(ang), CY + r * math.sin(ang)))
    return pts


def build_star_mask():
    """1-bitars mask (L) för stjärnans yta."""
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).polygon(star_points(SPIKES, R_OUTER, R_INNER), fill=255)
    return mask


def shade_star(img, mask):
    """Fyller stjärnan med faktetterad silvergradient (vertikal + spik-lyster)."""
    px   = img.load()
    mpx  = mask.load()
    for y in range(S):
        vt = y / (S - 1)                      # vertikal ramp-parameter
        base = ramp_color(SILVER_RAMP, vt)
        for x in range(S):
            if mpx[x, y] == 0:
                continue
            dx, dy = x - CX, y - CY
            ang = math.atan2(dy, dx)
            # Fasett-lyster: ljusa/mörka ådror längs varje pigg
            facet = 0.5 + 0.5 * math.sin(SPIKES * ang - math.pi / 2)
            # Diagonalt ljus uppe-till-vänster ger metallkänsla
            diag = 0.5 - 0.5 * (dx + dy) / (1.6 * R_OUTER)
            k = 0.72 + 0.30 * facet + 0.16 * diag
            k = max(0.55, min(1.25, k))
            px[x, y] = (
                min(255, int(base[0] * k)),
                min(255, int(base[1] * k)),
                min(255, int(base[2] * k)),
                255,
            )


def draw_star_outline(img, mask):
    """Ritar en 1 px mörk kant runt stjärnan genom att jämföra med masken."""
    px  = img.load()
    mpx = mask.load()
    for y in range(S):
        for x in range(S):
            if mpx[x, y] != 0:
                continue
            # Grannpixel inne i masken ⇒ vi står på kanten
            for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1),
                           (1, 1), (-1, -1), (1, -1), (-1, 1)):
                nx, ny = x + ox, y + oy
                if 0 <= nx < S and 0 <= ny < S and mpx[nx, ny] != 0:
                    px[x, y] = (*STAR_OUTLINE, 255)
                    break


def draw_medallion(img):
    """Ritar medaljongen: myntad silverrim + mörk, svagt buktande textyta."""
    draw = ImageDraw.Draw(img)
    # Ytterring (mörk bas — ger rimmen djup)
    draw.ellipse([CX - R_MEDAL, CY - R_MEDAL, CX + R_MEDAL, CY + R_MEDAL],
                 fill=(*RING_DARK, 255))

    # Inre yta med vertikal gradient (topp ljusare → botten mörkare)
    px = img.load()
    for y in range(S):
        vt = (y - (CY - R_FACE)) / (2 * R_FACE)
        vt = max(0.0, min(1.0, vt))
        col = tuple(int(FACE_TOP[k] + (FACE_BOT[k] - FACE_TOP[k]) * vt)
                    for k in range(3))
        for x in range(S):
            if (x - CX) ** 2 + (y - CY) ** 2 <= R_FACE ** 2:
                px[x, y] = (*col, 255)

    # Myntad silverrim: blank båge upptill-vänster, skugga nedtill-höger
    draw.arc([CX - R_MEDAL, CY - R_MEDAL, CX + R_MEDAL, CY + R_MEDAL],
             start=150, end=340, fill=(*RING_LIGHT, 255), width=2)
    draw.arc([CX - R_MEDAL, CY - R_MEDAL, CX + R_MEDAL, CY + R_MEDAL],
             start=-30, end=150, fill=(*RING_DARK, 255), width=2)

    # Tunn inre bevel intill rimmen: ljus båge upptill, skugga nedtill
    draw.arc([CX - R_FACE, CY - R_FACE, CX + R_FACE, CY + R_FACE],
             start=195, end=345, fill=(206, 216, 228, 210), width=1)
    draw.arc([CX - R_FACE, CY - R_FACE, CX + R_FACE, CY + R_FACE],
             start=15, end=165, fill=(14, 18, 26, 210), width=1)


# ── Pixelfont 5×7 (endast tecken som behövs) ─────────────────────────────────
FONT = {
    "R": ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
    "E": ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    "M": ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
    "A": ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
    "S": ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
    "T": ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
    "D": ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
    "I": ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
    "O": ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    "N": ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
    "*": ["00100", "10101", "01110", "11111", "01110", "10101", "00100"],
}
GLYPH_W, GLYPH_H, GAP = 5, 7, 1


def text_width(text):
    return len(text) * (GLYPH_W + GAP) - GAP


def draw_pixel_text(img, text, top_y):
    """Ritar centrerad text med silvergradient + 1 px mörk relief-skugga."""
    px = img.load()
    start_x = int(round(CX - text_width(text) / 2))
    for ci, ch in enumerate(text):
        glyph = FONT.get(ch)
        if glyph is None:
            continue
        gx = start_x + ci * (GLYPH_W + GAP)
        for ry, row in enumerate(glyph):
            for rx, bit in enumerate(row):
                if bit != "1":
                    continue
                x, y = gx + rx, top_y + ry
                # Relief-skugga snett ner-höger
                if 0 <= x + 1 < S and 0 <= y + 1 < S:
                    px[x + 1, y + 1] = (*TEXT_SHADOW, 255)
    # Andra passet: själva silvret ovanpå skuggan
    for ci, ch in enumerate(text):
        glyph = FONT.get(ch)
        if glyph is None:
            continue
        gx = start_x + ci * (GLYPH_W + GAP)
        for ry, row in enumerate(glyph):
            vt  = ry / (GLYPH_H - 1)
            col = tuple(int(TEXT_TOP[k] + (TEXT_BOT[k] - TEXT_TOP[k]) * vt)
                        for k in range(3))
            for rx, bit in enumerate(row):
                if bit != "1":
                    continue
                x, y = gx + rx, top_y + ry
                if 0 <= x < S and 0 <= y < S:
                    px[x, y] = (*col, 255)


def make_seal():
    img  = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    mask = build_star_mask()
    shade_star(img, mask)
    draw_star_outline(img, mask)
    draw_medallion(img)

    # Två textrader, vertikalt centrerade i medaljongen
    #   REMASTERED   (rad 1)
    #   * EDITION *   (rad 2, flankerad av små stjärnor)
    block_h = GLYPH_H * 2 + 4
    top1 = int(round(CY - block_h / 2))
    top2 = top1 + GLYPH_H + 4
    draw_pixel_text(img, "REMASTERED", top1)
    draw_pixel_text(img, "*EDITION*", top2)

    path = os.path.join(OUT_DIR, "splash_remastered_seal.png")
    img.save(path)
    print(f"  ✓ splash_remastered_seal.png  ({img.width}×{img.height}, {img.mode})")


if __name__ == "__main__":
    print(f"Genererar Remastered-sigill → {OUT_DIR}\n")
    make_seal()
    print("\nKlart!")
