"""
generate_boss_music.py — Genererar slutbossens akt 4-tema (acceptance.wav).

Skapar EN fil (stereo, 44100 Hz, 16-bit PCM) i Resources/Assets/Sound/:
  acceptance.wav — "Soluppgang": lugnt ambient-tema for akt 4 (Acceptans) i mapten

Körs med: python generate_boss_music.py
Kräver: inget utover Python-standardbiblioteket (wave, struct, math).

KONCEPT
  C-dur: parallelldur till bossmusikens (bossong) c-moll. Samma grundton, moll -> dur
  = "tvivlet/kolden slapper och ljuset kommer". Lugnt ~72 BPM, ambient: varm strak-pad,
  speldosa-arpeggio och en sparsmakad, hoppfull melodi. Bryggar bossens spänning ->
  den ljusa slutscenen (theend/finalend/Caveman ligger i G/A/C-dur).

TEKNIK (samma stdlib-chiptune som Tools/generate_sfx.py, men varmt och melodiskt)
  * Pad = rik vagtabell (strak/fiol) med 3 lätt otonade roster -> ensemble, inte spelsag.
  * Somlos loop via wrap-add: allt som spiller forbi slutet adderas cirkulart till borjan.
  * Latt "reverb" = glesa cirkulara tap -> ambient rymd som ocksa loopar somlost.

KOPPLING I SPELET
  Registreras som SoundRef.BGSoundAcceptance (Program.cs), spelas fordrojt när akt 4
  borjar och stoppas när boss-arenan lämnas (se GameplayState.ManageBossAudio/Exit).
"""
import os
import wave
import struct
import math

SR   = 44100
BPM  = 72.0
BEAT = 60.0 / BPM
BAR  = 4 * BEAT
BARS = 16
N    = int(round(BARS * BAR * SR))          # loop-längd i samples

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(SCRIPT_DIR, "..", "FrostyPlatformer",
                   "Resources", "Assets", "Sound", "acceptance.wav")

L = [0.0] * N
R = [0.0] * N

def midi(name):
    step = {"C":0,"C#":1,"D":2,"D#":3,"E":4,"F":5,"F#":6,"G":7,"G#":8,"A":9,"A#":10,"B":11}
    n, o = name[:-1], int(name[-1])
    return 12 * (o + 1) + step[n]
def freq(name):
    return 440.0 * 2 ** ((midi(name) - 69) / 12.0)

def add(buf, start, samples, gain):
    for j, s in enumerate(samples):
        buf[(start + j) % N] += s * gain      # wrap-add -> somlos loop

def env(i, n, atk, rel, sus=1.0, decay=0.0):
    """Attack -> (svag decay till sus) -> hall -> release."""
    t = i / SR; total = n / SR
    if t < atk:            return (t / atk) * 1.0
    if t > total - rel:    return max(0.0, (total - t) / rel) * sus
    if decay > 0 and t < atk + decay:
        return 1.0 + (sus - 1.0) * ((t - atk) / decay)
    return sus

# Vagtabell: varm strak-/fiol-klang (rik overtonsstapel, avrundad upptill).
# En ren sinus-pad later som en spelsag/theremin; overtonerna ger istället kropp.
WTLEN = 2048
def _make_wt(harm):
    wt = [sum(a*math.sin(2*math.pi*k*n/WTLEN) for k, a in harm) for n in range(WTLEN)]
    m = max(abs(v) for v in wt) or 1.0
    return [v/m for v in wt]
VIOLIN = _make_wt([(1,1.0),(2,0.5),(3,0.33),(4,0.22),(5,0.15),
                   (6,0.10),(7,0.07),(8,0.05),(9,0.03),(10,0.02)])

def pad(f, dur):
    """Varma strakar (fiol-ensemble): 3 lätt otonade roster panorerade L..R."""
    n = int(dur * SR); l = [0.0]*n; r = [0.0]*n
    voices = [(0.994, 0.66, 0.34), (1.0, 0.5, 0.5), (1.006, 0.34, 0.66)]  # (otoning, panL, panR)
    inc = [f * det * WTLEN / SR for det, _, _ in voices]
    ph  = [0.0, 0.0, 0.0]
    for i in range(n):
        e = env(i, n, atk=0.20, rel=0.6, sus=0.9)
        vib = 1.0 + 0.0026*math.sin(2*math.pi*4.8*i/SR) * min(1.0, max(0.0, (i/SR-0.3)/0.3))
        sl = sr = 0.0
        for vi in range(3):
            idx = ph[vi]; i0 = int(idx) % WTLEN; fr = idx - i0
            s = VIOLIN[i0]*(1-fr) + VIOLIN[(i0+1) % WTLEN]*fr
            sl += s*voices[vi][1]; sr += s*voices[vi][2]
            ph[vi] += inc[vi]*vib
        l[i] = sl/3*e; r[i] = sr/3*e
    return l, r

def bell(f, dur):
    n = int(dur * SR); o = [0.0]*n
    for i in range(n):
        t = i / SR
        e = math.exp(-t/0.55) * min(1.0, t/0.004)
        o[i] = (math.sin(2*math.pi*f*t) + 0.4*math.sin(2*math.pi*2*f*t)
                + 0.16*math.sin(2*math.pi*3*f*t)) * e
    return o

def lead(f, dur):
    n = int(dur * SR); o = [0.0]*n
    for i in range(n):
        t = i / SR
        vib = 1.0 + 0.006*math.sin(2*math.pi*5.2*t) * min(1.0, max(0.0, (t-0.18)/0.15))
        e = env(i, n, atk=0.05, rel=min(0.25, dur*0.4), sus=0.78, decay=0.15)
        o[i] = (math.sin(2*math.pi*f*vib*t) + 0.22*math.sin(2*math.pi*2*f*t)) * e
    return o

def bass(f, dur):
    n = int(dur * SR); o = [0.0]*n
    for i in range(n):
        t = i / SR
        e = env(i, n, atk=0.03, rel=0.15, sus=0.85)
        o[i] = (math.sin(2*math.pi*f*t) + 0.12*math.sin(2*math.pi*2*f*t)) * e
    return o

def at(bar, beat):     # tid (samples) for bar (1-indexerad) + beat (0-indexerad)
    return int(((bar-1)*BAR + beat*BEAT) * SR)

# Harmonik: vi -> IV -> I -> V, blommar hem till C.
CHORDS = [  # (bar, bas-rot, [ackordtoner for pad], [arp-toner])
    (1,  "A2", ["A3","C4","E4","G4"], ["A4","C5","E5","G5"]),   # Am7
    (2,  "F2", ["F3","A3","C4","G4"], ["F4","A4","C5","G5"]),   # Fadd9
    (3,  "C3", ["C4","E4","G4","D5"], ["C5","E5","G5","D5"]),   # Cadd9
    (4,  "G2", ["G3","B3","D4","A4"], ["G4","B4","D5","A5"]),   # Gadd9
    (5,  "A2", ["A3","C4","E4","G4"], ["A4","C5","E5","G5"]),
    (6,  "F2", ["F3","A3","C4","G4"], ["F4","A4","C5","G5"]),
    (7,  "C3", ["C4","E4","G4","D5"], ["C5","E5","G5","D5"]),
    (8,  "G2", ["G3","B3","D4","A4"], ["G4","B4","D5","A5"]),
    (9,  "F2", ["F3","A3","C4","G4"], ["F4","A4","C5","G5"]),
    (10, "G2", ["G3","B3","D4","A4"], ["G4","B4","D5","A5"]),
    (11, "C3", ["C4","E4","G4","D5"], ["C5","E5","G5","D5"]),
    (12, "A2", ["A3","C4","E4","G4"], ["A4","C5","E5","G5"]),
    (13, "F2", ["F3","A3","C4","G4"], ["F4","A4","C5","G5"]),
    (14, "G2", ["G3","B3","D4","A4"], ["G4","B4","D5","A5"]),
    (15, "C3", ["C4","E4","G4","E5"], ["C5","E5","G5","C6"]),
    (16, "C3", ["C4","E4","G4","E5"], ["C5","G5","E5","C6"]),
]

# Pad + bas per takt
for bar, root, tones, arp in CHORDS:
    b = bass(freq(root), BAR*0.98)
    add(L, at(bar, 0), b, 0.5); add(R, at(bar, 0), b, 0.5)
    for k, tn in enumerate(tones):
        pl, pr = pad(freq(tn), BAR*0.99)
        g = 0.13 if k == 0 else 0.09
        add(L, at(bar, 0), pl, g); add(R, at(bar, 0), pr, g)

# Speldosa-arpeggio: 8:e-delar, vaxlar L/R. Borjar mjukt (bar 3), svag svällning i mitten.
for bar, root, tones, arp in CHORDS:
    if bar < 3:
        continue
    dens = 0.9 if 5 <= bar <= 14 else 0.55
    pat = [0, 1, 2, 3, 2, 1, 2, 3]
    for e in range(8):
        tn = arp[pat[e] % len(arp)]
        s = bell(freq(tn), BEAT*0.9)
        pan_l = 0.62 if (e % 2 == 0) else 0.30
        g = 0.10 * dens
        add(L, at(bar, e*0.5), s, g*pan_l)
        add(R, at(bar, e*0.5), s, g*(0.92-pan_l))

# Melodi (bar, beat, längd i beats, ton) — sparsmakad, hoppfull.
MEL = [
    (1,2.0,2.0,"E5"),
    (2,0.0,1.0,"G5"), (2,1.0,2.0,"A5"),
    (3,0.0,1.5,"G5"), (3,2.0,1.0,"E5"),
    (4,0.0,3.0,"D5"),
    (5,0.0,1.0,"E5"), (5,1.0,1.0,"G5"), (5,2.0,2.0,"A5"),
    (6,0.0,1.5,"C6"), (6,2.0,1.5,"A5"),
    (7,0.0,2.0,"G5"), (7,2.0,1.5,"E5"),
    (8,0.0,3.2,"D5"),
    (9,0.0,1.0,"A5"), (9,1.0,1.0,"C6"), (9,2.0,2.0,"D6"),
    (10,0.0,1.0,"D6"), (10,1.0,2.0,"B5"),
    (11,0.0,2.0,"C6"), (11,2.0,1.5,"G5"),
    (12,0.0,2.0,"A5"), (12,2.0,1.5,"E5"),
    (13,0.0,1.0,"G5"), (13,1.0,1.0,"A5"), (13,2.0,2.0,"C6"),
    (14,0.0,2.0,"B5"), (14,2.0,1.5,"D6"),
    (15,0.0,3.6,"C6"),
    (16,2.0,1.5,"E5"),
]
for bar, beat, ln, tn in MEL:
    s = lead(freq(tn), ln*BEAT)
    add(L, at(bar, beat), s, 0.30); add(R, at(bar, beat), s, 0.30)

# Latt "reverb"-moln: glesa cirkulara tap (somlost), lite olika L/R.
def reverb(buf, taps):
    dry = buf[:]
    for d, g in taps:
        dd = int(d * SR)
        for i in range(N):
            buf[i] += g * dry[(i - dd) % N]

taps_l = [(0.037,0.28),(0.061,0.22),(0.089,0.17),(0.127,0.13),(0.170,0.10),
          (0.223,0.075),(0.281,0.055),(0.350,0.04)]
taps_r = [(0.041,0.28),(0.067,0.22),(0.097,0.17),(0.134,0.13),(0.181,0.10),
          (0.236,0.075),(0.297,0.055),(0.366,0.04)]
reverb(L, taps_l); reverb(R, taps_r)

# Normalisera + mjuk-klampa -> stereo 16-bit.
peak = max(1e-9, max(max(abs(x) for x in L), max(abs(x) for x in R)))
scale = 0.82 / peak
frames = bytearray()
for i in range(N):
    a = max(-1.0, min(1.0, L[i]*scale))
    b = max(-1.0, min(1.0, R[i]*scale))
    frames += struct.pack("<hh", int(a*32767), int(b*32767))

if __name__ == "__main__":
    with wave.open(OUT, "w") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(bytes(frames))
    print("  [ok] acceptance.wav  %.1fs stereo -> %s" % (N/SR, os.path.normpath(OUT)))
