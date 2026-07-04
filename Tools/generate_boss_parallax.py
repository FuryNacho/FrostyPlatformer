"""
generate_boss_parallax.py — Genererar slutbossens mellanskikt (parallax_mid_boss.png).

Skapar EN fil (512x224 px, RGBA) i Resources/Assets/Sprites/:
  parallax_mid_boss.png — snoklädda fjäll under manljus (slutboss-arenan, mapten)

Körs med: python generate_boss_parallax.py
Kräver: Pillow  (pip install Pillow)

KONCEPT — "De vita vidderna"
  Slutboss-arenan (mapten) ligger direkt efter vinterbanorna (mapseven-nine). Mellan-
  skiktet ska ge vinterkänsla och gravitas: lager av snoklädda bergstoppar mot den
  manljusa natthimlen (parallax_sky_boss). Den tidigare placeholdern var platta trianglar.

TEKNIK (matchar generate_parallax.py:s dither-estetik)
  * Hojdkurvor som summor av heltalsfrekventa sinusar -> somlos horisontell tiling.
  * Vassa topp-tält (add_tents) inne i bilden ger dominanta spetsar utan att bryta skarven.
  * Global manljus uppe till vänster -> volym (ljusa vänstersidor, skuggade hogersidor).
  * Sno ovanfor en oregelbunden snogräns -> toppar far vita kalotter, passen bar sten.
  * Ingen genomskinlig dimma vid foten (den goms bakom arenagolvet, rad 9 / y~144).

Ritordning i spelet (ParallaxSystem, Season.Boss):
  1. FillRect (svart)  2. parallax_sky_boss (0.10x)  3. parallax_mid_boss (0.30x)  4. Tiles (1.0x)
"""
import os
import math
import random
from PIL import Image

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_DIR    = os.path.join(SCRIPT_DIR, "..", "FrostyPlatformer",
                          "Resources", "Assets", "Sprites")

W, H = 512, 224   # Spelpixlar (skalas 4x i MonoGame). Bredd = 2x skärm -> somlos tiling.

# Bayer 4x4 tröskelmatris (ordnad dithering) — samma stipplade look som ovriga lager.
_B = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
def dith(x, y, frac):
    return frac > (_B[y & 3][x & 3] + 0.5) / 16.0

def lerp(a, b, t):
    return (int(a[0] + (b[0]-a[0])*t), int(a[1] + (b[1]-a[1])*t), int(a[2] + (b[2]-a[2])*t))

def periodic(base, terms, seed):
    """Hojdkurva som summa av heltalsfrekventa sinusar -> somlos period-W-kurva."""
    rng = random.Random(seed)
    ph = [rng.uniform(0, math.tau) for _ in terms]
    out = [0.0] * W
    for x in range(W):
        v = 0.0
        for (amp, f), p in zip(terms, ph):
            v += amp * math.sin(2*math.pi*f*x/W + p)
        out[x] = base - v
    return out

def add_tents(height, peaks):
    """Lägg till vassa triangeltoppar (hallna borta fran kanterna sa tilingen halls)."""
    for cx, amp, half in peaks:
        for x in range(cx-half, cx+half+1):
            if 0 <= x < W:
                height[x] -= amp * (1 - abs(x-cx)/half)
    return height

def draw_range(px, height, snowline, rock_lit, rock_shadow, rock_deep, snow_lit, snow_shadow):
    """Ritar ett ogenomskinligt bergslager: skuggad sten + snokalotter ovanfor snogränsen."""
    for x in range(W):
        top = int(round(height[x]))
        slope = height[(x+1) % W] - height[(x-1) % W]   # >0 -> asen faller mot hoger
        lit = slope > 0                                 # vänster sida mot manen
        sl = snowline[x]
        span = max(1, H - top)
        for y in range(max(0, top), H):
            # Sno ovanfor den (oregelbundna) snogränsen, dithrad over en 8px-som.
            if   y < sl - 4: frac = 1.0
            elif y < sl + 4: frac = 1 - (y - (sl-4)) / 8.0
            else:            frac = 0.0
            if frac >= 1.0 or (frac > 0 and dith(x, y, frac)):
                col = snow_lit if lit else snow_shadow
            else:
                shade = (y - top) / span                # morkna mot foten -> djup
                col = lerp(rock_lit if lit else rock_shadow, rock_deep, 0.55*shade)
            px[x, y] = (col[0], col[1], col[2], 255)

def build():
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()

    # FJÄRRAN — disiga blaaktiga toppar (ritas forst = bakom)
    far = periodic(112, [(26, 3), (15, 5), (8, 9)], seed=11)
    far = add_tents(far, [(96, 40, 40), (360, 34, 34)])
    far_sl = periodic(80, [(7, 4), (4, 7)], seed=21)
    draw_range(px, far, far_sl,
               rock_lit=(62, 80, 114), rock_shadow=(48, 63, 96), rock_deep=(40, 54, 84),
               snow_lit=(160, 182, 210), snow_shadow=(130, 154, 188))

    # MELLAN — dominanta vassa toppar, stark volym, ljus sno
    mid = periodic(138, [(40, 2), (22, 4), (11, 6)], seed=4)
    mid = add_tents(mid, [(150, 92, 48), (300, 74, 40), (432, 60, 34)])
    mid_sl = periodic(108, [(9, 3), (5, 6), (3, 10)], seed=31)
    draw_range(px, mid, mid_sl,
               rock_lit=(44, 57, 92), rock_shadow=(26, 35, 62), rock_deep=(16, 22, 44),
               snow_lit=(210, 226, 248), snow_shadow=(150, 176, 212))

    # NÄRA — morkast, mjuka snoiga forberg som fyller foten solitt (goms av golvet)
    near = periodic(168, [(24, 3), (13, 6), (7, 10)], seed=7)
    near_sl = periodic(150, [(6, 5), (4, 8)], seed=41)
    draw_range(px, near, near_sl,
               rock_lit=(19, 26, 50), rock_shadow=(11, 16, 34), rock_deep=(8, 11, 26),
               snow_lit=(150, 170, 202), snow_shadow=(112, 134, 168))

    path = os.path.join(OUT_DIR, "parallax_mid_boss.png")
    img.save(path)
    print("  [ok] parallax_mid_boss.png  (%dx%d, %s)" % (img.width, img.height, img.mode))

if __name__ == "__main__":
    print("Genererar bossens mellanskikt ->", os.path.normpath(OUT_DIR))
    build()
    print("Klart.")
