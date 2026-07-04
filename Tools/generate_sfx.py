"""
generate_sfx.py — Genererar slutbossens ljudeffekter för FrostyPlatformer.

Syntetiserar riktiga .wav-filer (16-bit PCM, 44100 Hz, mono) i Resources/Assets/Sound/
genom att skriva ut själva ljudvågen sample för sample — sinus/fyrkant/brus + envelopes.
Samma teknik som chiptune/8-bit-ljud, vilket passar spelets pixel-estetik.

Körs med: python generate_sfx.py
Kräver: inget utöver Python-standardbiblioteket (wave, math, struct, random).

Genererar (första ljud-skivan — se RELEASE_PLAN.md "Måste för release" punkt 1):
  slam_impact.wav  — jättens näve slår i marken (akt 3)
  slam_hammer.wav  — tyngre variant: hammarslaget (camp-brytaren)
  poff_glitch.wav  — digital poff/glitch (akt 4-finalen; återanvändbar för svärm/spegel)
  act_sting1.wav   — akt-övergång 1→2 (spegel → svärm): kort stigande blip
  act_sting2.wav   — akt-övergång 2→3 (svärm → jätte): mörkare, olycksbådande
  act_sting3.wav   — akt-övergång 3→4 (jätte → acceptans): mjuk, upplösande (leder in i tystnaden)

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

    print("Klart.")


if __name__ == "__main__":
    main()
