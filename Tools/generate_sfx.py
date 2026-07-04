"""
generate_sfx.py — Genererar slutbossens ljudeffekter för FrostyPlatformer.

Syntetiserar riktiga .wav-filer (16-bit PCM, 44100 Hz, mono) i Resources/Assets/Sound/
genom att skriva ut själva ljudvågen sample för sample — sinus/fyrkant/brus + envelopes.
Samma teknik som chiptune/8-bit-ljud, vilket passar spelets pixel-estetik.

Körs med: python generate_sfx.py
Kräver: inget utöver Python-standardbiblioteket (wave, math, struct, random).

Genererar (se RELEASE_PLAN.md "Måste för release" punkt 1):

Skiva 1:
  slam_impact.wav  — jättens näve slår i marken (akt 3)
  slam_hammer.wav  — tyngre variant: hammarslaget (camp-brytaren)
  poff_glitch.wav  — digital poff/glitch (akt 4-finalen)
  act_sting1.wav   — akt-övergång 1→2 (spegel → svärm): kort stigande blip
  act_sting2.wav   — akt-övergång 2→3 (svärm → jätte): mörkare, olycksbådande
  act_sting3.wav   — akt-övergång 3→4 (jätte → acceptans): mjuk, upplösande (leder in i tystnaden)

Skiva 2:
  ice_whoosh.wav     — istapp faller (akt 3): kort luftig svischning
  ice_shatter.wav    — istapp krossas mot marken: ljus glasig splitter
  poff_small.wav     — svärm-kopia poffar (akt 2): lätt, ljus poff
  glitch_dissolve.wav— spegel-Scarlets glitch-exit (akt 1→2): digital upplösning ~0.7s
  giant_collapse.wav — jätten rasar (akt 3→4): tungt mullrande kollaps

OBS: Dessa är syntetiserade — avsiktligt enkla och genre-passande. Byt gärna ut mot
riktiga inspelningar senare; filnamnen (SoundRef) är kontraktet mot koden.
"""

import os
import math
import wave
import struct
import random

# ── Mål-katalog + format ─────────────────────────────────────────────────────
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_DIR    = os.path.join(SCRIPT_DIR, "..", "FrostyPlatformer",
                          "Resources", "Assets", "Sound")
os.makedirs(OUT_DIR, exist_ok=True)

RATE = 44100   # samples/sekund

random.seed(1234)   # deterministisk brus → reproducerbara filer mellan körningar


# ── Bas-vågformer (returnerar float-amplitud i [-1, 1] för fas t i sekunder) ──

def sine(freq, t):
    return math.sin(2.0 * math.pi * freq * t)

def square(freq, t, duty=0.5):
    return 1.0 if (freq * t) % 1.0 < duty else -1.0

def noise(_t):
    return random.uniform(-1.0, 1.0)


# ── Envelope + hjälpare ──────────────────────────────────────────────────────

def exp_decay(t, tau):
    """Exponentiellt avklingande envelope (1.0 vid t=0 → mot 0)."""
    return math.exp(-t / tau)

def lin_fade(i, n, fade_in=0.01, fade_out=0.02):
    """Linjär in/ut-fade (i sekunder) för att undvika klick i kanterna."""
    t = i / RATE
    total = n / RATE
    a = min(1.0, t / fade_in) if fade_in > 0 else 1.0
    b = min(1.0, (total - t) / fade_out) if fade_out > 0 else 1.0
    return max(0.0, min(a, b))

def lerp(a, b, x):
    return a + (b - a) * x


def write_wav(name, samples, peak=0.9):
    """Normaliserar till 'peak', mjuk-klampar och skriver 16-bit PCM mono."""
    hi = max((abs(s) for s in samples), default=1.0) or 1.0
    scale = peak / hi
    frames = bytearray()
    for s in samples:
        v = max(-1.0, min(1.0, s * scale))
        frames += struct.pack("<h", int(v * 32767))
    path = os.path.join(OUT_DIR, name)
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(bytes(frames))
    print(f"  {name:16s} {len(samples)/RATE:.2f}s  ({len(frames)} bytes)")


def dur(seconds):
    return int(seconds * RATE)


# ── Ljud-byggare ─────────────────────────────────────────────────────────────

def build_slam(pitch_hi, pitch_lo, length, noise_amt, tau):
    """Tungt nedslag: pitch-fallande boom + kort brus-transient ('crack') + mullrande svans."""
    n = dur(length)
    out = []
    crack_n = dur(0.05)
    for i in range(n):
        t = i / RATE
        x = t / length
        # Boom: sinus vars frekvens faller snabbt (impact → mark).
        f = lerp(pitch_hi, pitch_lo, min(1.0, x * 1.6))
        body = sine(f, t) * exp_decay(t, tau)
        # Impact-transient: ljus brus-smäll bara i början.
        crack = noise(t) * exp_decay(t, 0.02) if i < crack_n else 0.0
        # Lågt mullrande brus under svansen ('rubble').
        rumble = noise(t) * 0.35 * exp_decay(t, tau * 0.8)
        s = body + noise_amt * crack + noise_amt * 0.5 * rumble
        out.append(s * lin_fade(i, n, 0.0, 0.03))
    return out


def build_poff():
    """Digital poff: nedåt-stegad fyrkantszap + brusflisor — cyan/magenta-explosionens röst."""
    n = dur(0.30)
    steps = [1400, 1000, 720, 500, 340]   # stegad nedåt-pitch (bit-crush-känsla)
    out = []
    for i in range(n):
        t = i / RATE
        x = t / 0.30
        idx = min(len(steps) - 1, int(x * len(steps)))
        zap = square(steps[idx], t, duty=0.5) * 0.6
        spark = noise(t) * 0.5 * exp_decay(t, 0.06)
        env = exp_decay(t, 0.10)
        out.append((zap * env + spark) * lin_fade(i, n, 0.0, 0.03))
    return out


def build_sting(notes, note_len, wave_fn, amp=0.8, tau=0.12, gap=0.0):
    """Kort musikalisk stinger: en sekvens toner (arpeggio) med per-ton exp-decay."""
    step = note_len + gap
    n = dur(step * len(notes))
    out = [0.0] * n
    for k, freq in enumerate(notes):
        start = int(k * step * RATE)
        ln = dur(note_len)
        for j in range(ln):
            if start + j >= n:
                break
            t = j / RATE
            val = wave_fn(freq, t) * amp * exp_decay(t, tau)
            out[start + j] += val * lin_fade(j, ln, 0.004, 0.02)
    return out


def build_ice_whoosh():
    """Fallande istapp: luftig svischning — brus genom ett lågpass vars ton faller (mörknar)."""
    n = dur(0.32)
    out = []
    prev = 0.0
    for i in range(n):
        t = i / RATE
        x = t / 0.32
        a = lerp(0.5, 0.15, x)          # 1-pols lågpass; cutoff faller → luftigare mot slutet
        prev += a * (noise(t) - prev)
        env = math.sin(math.pi * x)     # mjuk svällning in→ut
        out.append(prev * env * 0.9)
    return out


def build_ice_shatter():
    """Istapp krossas: ljus brus-smäll + några glasiga 'tings' som klingar snabbt."""
    n = dur(0.28)
    tings = [2600, 3100, 3700, 2200]
    out = []
    for i in range(n):
        t = i / RATE
        s = noise(t) * 0.6 * exp_decay(t, 0.05)
        for k, f in enumerate(tings):
            st = k * 0.015
            if t >= st:
                s += square(f, t - st) * 0.18 * exp_decay(t - st, 0.04)
        out.append(s * lin_fade(i, n, 0.0, 0.03))
    return out


def build_poff_small():
    """Svärm-kopians poff: som den stora poffen men ljusare och kortare (mindre kropp)."""
    n = dur(0.16)
    steps = [1900, 1500, 1100, 760]
    out = []
    for i in range(n):
        t = i / RATE
        x = t / 0.16
        idx = min(len(steps) - 1, int(x * len(steps)))
        zap = square(steps[idx], t) * 0.55
        spark = noise(t) * 0.5 * exp_decay(t, 0.04)
        out.append((zap * exp_decay(t, 0.06) + spark) * lin_fade(i, n, 0.0, 0.02))
    return out


def build_glitch_dissolve():
    """Spegel-Scarlets exit: digital upplösning — nedåt-stegad 'tearing'-fyrkant + växande brus,
    hackad av en snabb stutter-gate (bit-crush-känsla). Klingar ut mot slutet."""
    n = dur(0.7)
    out = []
    for i in range(n):
        t = i / RATE
        x = t / 0.7
        fq = int(lerp(700, 120, x) / 40) * 40 + 40    # kvantiserad nedåt-pitch (digitalt)
        tear = square(fq, t) * 0.5
        nz = noise(t) * lerp(0.2, 0.6, x)             # bruset växer när hon löses upp
        gate = 1.0 if (int(t * 60) % 2 == 0) else 0.35   # stutter-gate → glitch-hack
        env = lerp(1.0, 0.2, x)
        out.append((tear + nz) * gate * env)
    return out


def build_giant_collapse():
    """Jätten rasar: lågt fallande boom + tungt mullrande brus + smulande skräp-stötar."""
    n = dur(0.9)
    out = []
    prev = 0.0
    for i in range(n):
        t = i / RATE
        x = t / 0.9
        f = lerp(70, 25, min(1.0, x * 1.3))
        boom = sine(f, t) * exp_decay(t, 0.5)
        prev += 0.08 * (noise(t) - prev)              # tungt lågpass → mullrande rumble
        rumble = prev * lerp(0.9, 0.3, x)
        crumb = noise(t) * 0.4 * exp_decay(t, 0.3) if (int(t * 30) % 3 == 0) else 0.0
        out.append((boom * 0.8 + rumble + crumb * 0.5) * lin_fade(i, n, 0.0, 0.05))
    return out


# ── Huvud ────────────────────────────────────────────────────────────────────

def main():
    print("Genererar boss-SFX ->", os.path.normpath(OUT_DIR))

    # Näve-nedslag: tungt, kort, med skarp transient.
    write_wav("slam_impact.wav", build_slam(pitch_hi=120, pitch_lo=38,
              length=0.34, noise_amt=0.9, tau=0.11), peak=0.95)

    # Hammarslaget: lägre, längre, tyngre — den hårda camp-brytaren.
    write_wav("slam_hammer.wav", build_slam(pitch_hi=95, pitch_lo=28,
              length=0.50, noise_amt=1.0, tau=0.17), peak=1.0)

    # Digital poff (glitch-explosion).
    write_wav("poff_glitch.wav", build_poff(), peak=0.9)

    # Akt-stingers — distinkt karaktär per övergång.
    # 1→2: kort stigande blip (fyrkant, ljust) — "akt klarad, mer kommer".
    write_wav("act_sting1.wav",
              build_sting([440, 587, 784], 0.075, square, amp=0.7, tau=0.06), peak=0.85)
    # 2→3: mörkare, olycksbådande fall (fyrkant, lågt) — jätten stiger.
    write_wav("act_sting2.wav",
              build_sting([330, 247, 165], 0.11, square, amp=0.7, tau=0.14), peak=0.85)
    # 3→4: mjuk, upplösande arpeggio (sinus) som klingar ut → in i tystnaden.
    write_wav("act_sting3.wav",
              build_sting([523, 659, 784, 1047], 0.13, sine, amp=0.6, tau=0.22, gap=0.02),
              peak=0.7)

    # ── Skiva 2 ──────────────────────────────────────────────────────────────
    write_wav("ice_whoosh.wav",      build_ice_whoosh(),      peak=0.7)
    write_wav("ice_shatter.wav",     build_ice_shatter(),     peak=0.9)
    write_wav("poff_small.wav",      build_poff_small(),      peak=0.85)
    write_wav("glitch_dissolve.wav", build_glitch_dissolve(), peak=0.85)
    write_wav("giant_collapse.wav",  build_giant_collapse(),  peak=1.0)

    print("Klart.")


if __name__ == "__main__":
    main()
