"""
generate_parallax.py — Genererar parallax-bakgrundsbilder för FrostyPlatformer.

Skapar 8 PNG-filer (512×224 px) i Resources/Assets/Sprites/:
  parallax_sky_{spring,summer,fall,winter}.png  — solid himmel (RGB)
  parallax_mid_{spring,summer,fall,winter}.png  — siluetter med alpha (RGBA)

Körs med: python generate_parallax.py
Kräver: Pillow  (pip install Pillow)

Bildernas storlek:
  Bredd 512 px = 2× skärmbredden (256 px) — garanterar sömlös tiling.
  Höjd  224 px = full skärmhöjd (NES-upplösning).

Ritordning i spelet:
  1. FillRect (svart)
  2. Sky-lager  (denna fil, solid, scroll 0.10×)
  3. Mid-lager  (denna fil, transparent topp, scroll 0.30×)
  4. Tiles (1.0×)
"""

import os
import math
import random
from PIL import Image, ImageDraw

# ── Mål-katalog ──────────────────────────────────────────────────────────────
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_DIR    = os.path.join(SCRIPT_DIR, "..", "FrostyPlatformer",
                          "Resources", "Assets", "Sprites")
os.makedirs(OUT_DIR, exist_ok=True)

W, H = 512, 224   # Spelpixlar (skalas 4× i MonoGame → 2048×896 på skärm)


# ── Hjälpfunktioner ──────────────────────────────────────────────────────────

def vertical_gradient(img: Image.Image, top_color, bot_color):
    """Fyller bilden med ett vertikalt gradientfält från top till bot."""
    draw = ImageDraw.Draw(img)
    for y in range(H):
        t = y / (H - 1)
        r = int(top_color[0] + (bot_color[0] - top_color[0]) * t)
        g = int(top_color[1] + (bot_color[1] - top_color[1]) * t)
        b = int(top_color[2] + (bot_color[2] - top_color[2]) * t)
        draw.line([(0, y), (W - 1, y)], fill=(r, g, b))


def draw_stars(img: Image.Image, count=120, seed=42):
    """Lägger ut slumpmässiga pixelstjärnor i den övre halvan av bilden."""
    rng  = random.Random(seed)
    draw = ImageDraw.Draw(img)
    for _ in range(count):
        x = rng.randint(0, W - 1)
        y = rng.randint(0, H // 2)
        brightness = rng.randint(160, 255)
        size = rng.choice([1, 1, 1, 2])   # 75% enkelpixel, 25% tvåpixel
        draw.rectangle([x, y, x + size - 1, y + size - 1],
                        fill=(brightness, brightness, brightness))


def draw_distant_hills(draw: ImageDraw.ImageDraw, base_y, color, seed, amplitude=18, freq=0.012):
    """Ritar en mjuk siluettkulle längs horisontlinjen."""
    rng   = random.Random(seed)
    phase = rng.uniform(0, math.tau)
    pts   = [(0, H)]
    for x in range(W):
        # Kombination av två sinusvågor för organisk form
        y = base_y - int(amplitude * math.sin(freq * x + phase)
                         - amplitude * 0.4 * math.sin(freq * 1.7 * x + phase * 1.3))
        pts.append((x, max(0, y)))
    pts.append((W - 1, H))
    draw.polygon(pts, fill=color)


def draw_pine_silhouette(draw: ImageDraw.ImageDraw, cx, base_y, height, color):
    """Ritar en stiliserad gran-siluett som en triangelstam."""
    # Stam
    stam_w = max(2, height // 10)
    draw.rectangle([cx - stam_w // 2, base_y - height // 4,
                    cx + stam_w // 2, base_y], fill=color)
    # Tre lagor krona (trianglar)
    for level in range(3):
        t      = (level + 1) / 3
        tier_h = int(height * (0.5 + 0.2 * level))
        tier_w = int(height * (0.22 + 0.18 * level))
        y_bot  = base_y - height // 4 - level * height // 5
        y_top  = y_bot - tier_h
        draw.polygon([(cx, y_top), (cx - tier_w, y_bot), (cx + tier_w, y_bot)],
                      fill=color)


def draw_bare_tree(draw: ImageDraw.ImageDraw, cx, base_y, height, color):
    """Ritar ett lövfällt träd med grenar (höst)."""
    draw.line([(cx, base_y), (cx, base_y - height)], fill=color, width=2)
    # Grenar
    rng = random.Random(cx)
    for i in range(5):
        bh  = base_y - height // 3 - i * height // 8
        bw  = int(height * (0.2 - i * 0.02))
        dir = 1 if rng.random() > 0.5 else -1
        draw.line([(cx, bh), (cx + dir * bw, bh - bw // 2)],
                   fill=color, width=1)
        draw.line([(cx, bh), (cx - dir * bw // 2, bh - bw // 3)],
                   fill=color, width=1)


def save(img: Image.Image, name: str):
    path = os.path.join(OUT_DIR, name)
    img.save(path)
    print(f"  ✓ {name}  ({img.width}×{img.height}, {img.mode})")


# ── VÅR ─────────────────────────────────────────────────────────────────────

def make_spring_sky():
    img  = Image.new("RGB", (W, H))
    # Ljus himmelblå mot avblekt mintgrön vid horisonten
    vertical_gradient(img, (135, 200, 235), (195, 230, 210))
    # Avlägsna rundkullar i horisontlinjen
    draw = ImageDraw.Draw(img)
    draw_distant_hills(draw, base_y=H - 30, color=(155, 200, 175), seed=1, amplitude=22)
    draw_distant_hills(draw, base_y=H - 15, color=(170, 215, 190), seed=2, amplitude=12)
    save(img, "parallax_sky_spring.png")


def make_spring_mid():
    img  = Image.new("RGBA", (W, H), (0, 0, 0, 0))   # Helt transparent start
    draw = ImageDraw.Draw(img)
    # Markband längst ner
    draw.rectangle([0, H - 18, W - 1, H - 1], fill=(60, 120, 60, 255))
    # Lövskogssiluetter
    rng = random.Random(7)
    for x in range(-20, W + 20, 28):
        h_tree = rng.randint(55, 90)
        # Rund lövkrona
        cw = rng.randint(22, 38)
        cy = H - 18 - h_tree
        draw.ellipse([x - cw, cy - cw // 2, x + cw, cy + cw // 2],
                      fill=(40, 95, 40, 255))
        # Stam
        draw.rectangle([x - 2, cy + cw // 2, x + 2, H - 18],
                        fill=(55, 35, 20, 255))
    # Blommor i förgrunden
    rng2 = random.Random(11)
    colors = [(220, 80, 120), (240, 200, 60), (200, 120, 220)]
    for _ in range(60):
        fx   = rng2.randint(0, W - 1)
        fy   = H - rng2.randint(2, 16)
        col  = colors[rng2.randint(0, 2)]
        draw.ellipse([fx - 2, fy - 2, fx + 2, fy + 2], fill=(*col, 220))
    save(img, "parallax_mid_spring.png")


# ── SOMMAR ───────────────────────────────────────────────────────────────────

def make_summer_sky():
    img  = Image.new("RGB", (W, H))
    # Djupblå sommardag med varm gradient mot horisonten
    vertical_gradient(img, (60, 140, 210), (175, 215, 230))
    draw = ImageDraw.Draw(img)
    # Dimmiga avlägsna berg
    draw_distant_hills(draw, base_y=H - 40, color=(120, 160, 195), seed=5, amplitude=30)
    draw_distant_hills(draw, base_y=H - 20, color=(150, 185, 215), seed=6, amplitude=15)
    # Distansmoln (enkla rektanglar + rundning)
    for cx, cy, cw, ch in [(80, 50, 60, 20), (250, 35, 80, 22), (420, 60, 55, 18)]:
        draw.ellipse([cx - cw, cy - ch, cx + cw, cy + ch], fill=(245, 248, 255))
    save(img, "parallax_sky_summer.png")


def make_summer_mid():
    img  = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Gräsmark
    draw.rectangle([0, H - 22, W - 1, H - 1], fill=(50, 110, 45, 255))
    # Lummiga trädkronor (överlappande ellipser för täthet)
    rng = random.Random(13)
    for x in range(-15, W + 15, 22):
        h_tree = rng.randint(70, 110)
        cy     = H - 22 - h_tree
        # Tre lager ellipser per träd (mörk → ljusare)
        for dk, (dc, da) in enumerate([(0, 255), (20, 240), (35, 220)]):
            cw = rng.randint(18, 32) - dk * 3
            draw.ellipse([x - cw, cy - cw // 2 + dk * 5,
                          x + cw, cy + cw // 2 + dk * 5],
                          fill=(25 + dc, 85 + dc, 20 + dc, da))
        # Stam
        draw.rectangle([x - 3, cy + 20, x + 3, H - 22], fill=(60, 40, 20, 255))
    save(img, "parallax_mid_summer.png")


# ── HÖST ─────────────────────────────────────────────────────────────────────

def make_fall_sky():
    img  = Image.new("RGB", (W, H))
    # Varmt solnedgångsljus — djuporange vid horisont, mörkare ovan
    vertical_gradient(img, (55, 30, 60), (215, 115, 50))
    draw = ImageDraw.Draw(img)
    # Avlägsna siluettkullar i höstton
    draw_distant_hills(draw, base_y=H - 35, color=(85, 45, 30), seed=9, amplitude=25)
    draw_distant_hills(draw, base_y=H - 18, color=(100, 55, 35), seed=10, amplitude=12)
    save(img, "parallax_sky_fall.png")


def make_fall_mid():
    img  = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Torr mark
    draw.rectangle([0, H - 16, W - 1, H - 1], fill=(80, 55, 30, 255))
    # Lövfällda träd
    rng = random.Random(17)
    for x in range(-10, W + 10, 35):
        h_tree = rng.randint(55, 85)
        draw_bare_tree(draw, x, H - 16, h_tree, (60, 35, 20, 255))
    # Löv som flyger (enkla ellipser i höstfärger)
    rng2 = random.Random(23)
    leaf_cols = [(200, 80, 20, 200), (220, 140, 30, 190), (180, 60, 40, 180),
                 (240, 180, 50, 170)]
    for _ in range(80):
        lx  = rng2.randint(0, W - 1)
        ly  = rng2.randint(H // 3, H - 20)
        col = leaf_cols[rng2.randint(0, 3)]
        draw.ellipse([lx - 3, ly - 2, lx + 3, ly + 2], fill=col)
    save(img, "parallax_mid_fall.png")


# ── VINTER ───────────────────────────────────────────────────────────────────

def make_winter_sky():
    img  = Image.new("RGB", (W, H))
    # Mörk vinterhimmel — djupblå natt mot lite ljusare på horisonten
    vertical_gradient(img, (8, 12, 35), (28, 45, 85))
    draw_stars(img, count=140, seed=3)
    # Svag månglöd (stor ljus cirkel, inte solid)
    draw = ImageDraw.Draw(img)
    for r in range(22, 0, -1):
        alpha = int(8 * (1 - r / 22))
        draw.ellipse([W // 4 - r, 30 - r, W // 4 + r, 30 + r],
                      fill=(200, 215, 240))
    draw.ellipse([W // 4 - 10, 20, W // 4 + 10, 40], fill=(230, 235, 255))
    save(img, "parallax_sky_winter.png")


def make_winter_mid():
    img  = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Snötäckt mark
    draw.rectangle([0, H - 20, W - 1, H - 1], fill=(210, 225, 240, 255))
    # Snökullar (avrundad topp på markbandet)
    for cx in range(20, W, 60):
        draw.ellipse([cx - 40, H - 32, cx + 40, H - 12], fill=(210, 225, 240, 255))
    # Gran-siluetter
    rng = random.Random(19)
    for x in range(-10, W + 10, 38):
        h_tree = rng.randint(65, 100)
        draw_pine_silhouette(draw, x, H - 20, h_tree, (25, 40, 65, 255))
        # Snö på grentopp
        draw.ellipse([x - 8, H - 20 - h_tree - 4, x + 8, H - 20 - h_tree + 6],
                      fill=(210, 225, 240, 200))
    save(img, "parallax_mid_winter.png")


# ── Main ─────────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    print(f"Genererar parallax-bakgrunder → {OUT_DIR}\n")

    print("VÅR")
    make_spring_sky()
    make_spring_mid()

    print("SOMMAR")
    make_summer_sky()
    make_summer_mid()

    print("HÖST")
    make_fall_sky()
    make_fall_mid()

    print("VINTER")
    make_winter_sky()
    make_winter_mid()

    print(f"\nKlart! 8 filer skapade i:\n  {OUT_DIR}")
