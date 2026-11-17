#!/usr/bin/env python3
"""
Gera os efeitos sonoros e as músicas do Xeque de Tinta por síntese (chiptune), só com a biblioteca padrão.
Obra original dos autores do projeto, dedicada ao domínio público (CC0). Uso:

    python3 Tools/gen_audio.py            # escreve em Assets/_Project/Audio/{Sfx,Music}
"""
import math, os, random, struct, sys, wave

SR = 22050
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Project", "Audio")
random.seed(7)


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def osc(kind, phase):
    p = phase % 1.0
    if kind == "square": return 1.0 if p < 0.5 else -1.0
    if kind == "pulse": return 1.0 if p < 0.25 else -1.0
    if kind == "tri": return 4 * abs(p - 0.5) - 1
    if kind == "saw": return 2 * p - 1
    if kind == "sine": return math.sin(2 * math.pi * p)
    return random.uniform(-1, 1)  # noise


def tone(f0, dur, kind="square", vol=0.3, f1=None, attack=0.004, release=0.5):
    """Nota com envelope. f1 faz um glissando de f0 a f1. release = fração final que decai."""
    n = int(SR * dur)
    out = []
    ph = 0.0
    for i in range(n):
        t = i / n
        f = f0 if f1 is None else f0 + (f1 - f0) * t
        ph += f / SR
        a = min(1.0, i / max(1, SR * attack))
        r = 1.0 if t < 1 - release else (1 - t) / release
        out.append(osc(kind, ph) * vol * a * r)
    return out


def noise(dur, vol=0.3, decay=4.0, lowpass=0.0):
    n = int(SR * dur)
    out, prev = [], 0.0
    for i in range(n):
        s = random.uniform(-1, 1)
        if lowpass: s = prev + (s - prev) * (1 - lowpass); prev = s
        out.append(s * vol * math.exp(-decay * i / n))
    return out


def mix(*parts):
    """parts: (offset_em_segundos, amostras)."""
    length = max(int(o * SR) + len(a) for o, a in parts)
    out = [0.0] * length
    for o, a in parts:
        s = int(o * SR)
        for i, v in enumerate(a): out[s + i] += v
    return out


def save(path, samples, gain=0.9):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    peak = max(1e-9, max(abs(v) for v in samples))
    k = gain / peak if peak > gain else 1.0
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, v * k)) * 32767)) for v in samples))
    print("ok", os.path.relpath(path))


# ------------------------------------------------------------------ efeitos
def sfx():
    d = os.path.join(OUT, "Sfx")
    # dado: estalos de madeira que espaçam
    parts = []
    t = 0.0
    for i in range(9):
        parts.append((t, mix((0, noise(0.04, 0.5, 9, 0.2)), (0, tone(300 - i * 12, 0.05, "tri", 0.35, release=0.9)))))
        t += 0.05 + i * 0.012
    save(os.path.join(d, "sfx_dice.wav"), mix(*parts))
    save(os.path.join(d, "sfx_step.wav"), tone(180, 0.09, "square", 0.25, f1=90))
    save(os.path.join(d, "sfx_card.wav"), mix((0, tone(380, 0.05, "square", 0.22, f1=620)), (0.04, tone(620, 0.07, "square", 0.2, f1=820))))
    save(os.path.join(d, "sfx_hit.wav"), mix((0, noise(0.16, 0.55, 7, 0.1)), (0, tone(140, 0.14, "tri", 0.5, f1=60))))
    save(os.path.join(d, "sfx_hit_effective.wav"),
         mix((0, noise(0.2, 0.6, 6, 0.05)), (0, tone(170, 0.18, "tri", 0.55, f1=55)),
             (0.05, tone(988, 0.08, "square", 0.22)), (0.11, tone(1319, 0.12, "square", 0.22)), (0.18, tone(1760, 0.2, "square", 0.2))))
    save(os.path.join(d, "sfx_shield.wav"), mix((0, tone(740, 0.18, "sine", 0.3)), (0, tone(1110, 0.16, "sine", 0.22)), (0.02, noise(0.05, 0.2, 8, 0.3))))
    heal = [(i * 0.07, tone(midi(n), 0.14, "sine", 0.28)) for i, n in enumerate((72, 76, 79, 84))]
    save(os.path.join(d, "sfx_heal.wav"), mix(*heal))
    save(os.path.join(d, "sfx_burn.wav"), mix((0, noise(0.4, 0.4, 2.2, 0.55)), (0, tone(220, 0.35, "saw", 0.12, f1=330))))
    save(os.path.join(d, "sfx_die.wav"), mix((0, tone(300, 0.4, "square", 0.25, f1=60)), (0, noise(0.4, 0.35, 3, 0.4))))
    save(os.path.join(d, "sfx_hurt.wav"), mix((0, tone(260, 0.24, "saw", 0.3, f1=90)), (0, noise(0.1, 0.4, 8, 0.2))))
    save(os.path.join(d, "sfx_coin.wav"), mix((0, tone(1568, 0.07, "square", 0.2)), (0.07, tone(2093, 0.2, "square", 0.2))))
    save(os.path.join(d, "sfx_click.wav"), tone(880, 0.045, "square", 0.18, release=0.9))
    win = [(i * 0.13, tone(midi(n), 0.2 if i < 5 else 0.7, "square", 0.22)) for i, n in enumerate((60, 64, 67, 72, 76, 79))]
    win += [(0.65, tone(midi(72), 0.7, "tri", 0.25)), (0.65, tone(midi(64), 0.7, "tri", 0.2))]
    save(os.path.join(d, "sfx_win.wav"), mix(*win))
    lose = [(i * 0.22, tone(midi(n), 0.3 if i < 3 else 0.8, "tri", 0.3)) for i, n in enumerate((69, 65, 62, 57))]
    save(os.path.join(d, "sfx_lose.wav"), mix(*lose))
    save(os.path.join(d, "sfx_summon.wav"), mix((0, tone(110, 0.5, "saw", 0.22, f1=330)), (0.1, tone(220, 0.45, "square", 0.15, f1=660))))


# ------------------------------------------------------------------ músicas
def track(bpm, bars, chord_fn, lead_fn, bass_fn, drums=False, arp=False):
    beat = 60.0 / bpm
    parts = []
    for bar in range(bars):
        t0 = bar * 4 * beat
        chord = chord_fn(bar)
        # baixo
        for (n, b, l) in bass_fn(bar, chord):
            parts.append((t0 + b * beat, tone(midi(n), l * beat * 0.95, "tri", 0.30, release=0.35)))
        # melodia
        t = 0.0
        for (n, l) in lead_fn(bar, chord):
            if n is not None:
                parts.append((t0 + t * beat, tone(midi(n), l * beat * 0.92, "pulse", 0.13, release=0.4)))
            t += l
        # arpejo suave
        if arp:
            for i in range(8):
                n = chord[i % 3] + 12
                parts.append((t0 + i * 0.5 * beat, tone(midi(n), 0.45 * beat, "square", 0.05, release=0.6)))
        if drums:
            for b in range(4):
                parts.append((t0 + b * beat, noise(0.1, 0.30, 12, 0.5) if b % 2 == 0 else noise(0.12, 0.38, 9, 0.15)))
                parts.append((t0 + (b + 0.5) * beat, noise(0.03, 0.12, 14, 0.0)))
    return mix(*parts)


def music():
    d = os.path.join(OUT, "Music")
    # ---- Tabuleiro / título: calmo, dó maior, 96 bpm, 16 compassos
    prog = [(48, (60, 64, 67)), (45, (57, 60, 64)), (41, (53, 57, 60)), (43, (55, 59, 62))]  # C Am F G
    def chord_b(bar): return prog[(bar // 2) % 4][1]
    def bass_b(bar, chord): r = prog[(bar // 2) % 4][0]; return [(r, 0, 2), (r + 7, 2, 1), (r, 3, 1)]
    patA = lambda c: [(c[2] + 12, 1), (c[1] + 12, .5), (c[2] + 12, .5), (c[0] + 12, 1), (c[1] + 12, 1)]
    patB = lambda c: [(c[0] + 12, 1.5), (c[2] + 12, .5), (c[1] + 12, 1), (None, 1)]
    def lead_b(bar, chord): return patA(chord) if bar % 2 == 0 else patB(chord)
    save(os.path.join(d, "music_board.wav"), track(96, 16, chord_b, lead_b, bass_b, drums=False, arp=True), gain=0.8)
    # ---- Batalha: tenso, lá menor, 140 bpm, 16 compassos
    prog2 = [(45, (57, 60, 64)), (41, (53, 57, 60)), (43, (55, 59, 62)), (40, (52, 55, 59))]  # Am F G Em
    def chord_c(bar): return prog2[bar % 4][1]
    def bass_c(bar, chord): r = prog2[bar % 4][0]; return [(r, i * 0.5, 0.5) for i in range(8)]
    def lead_c(bar, c):
        riff = [(c[0] + 12, .5), (c[1] + 12, .5), (c[2] + 12, .5), (c[1] + 12, .5), (c[0] + 12, .5), (c[1] + 12, .5), (c[2] + 12, .5), (c[2] + 19, .5)]
        if bar % 4 == 3: riff = [(c[2] + 12, 1), (c[1] + 12, 1), (c[0] + 12, 1), (None, 1)]
        return riff
    save(os.path.join(d, "music_battle.wav"), track(140, 16, chord_c, lead_c, bass_c, drums=True, arp=False), gain=0.8)


if __name__ == "__main__":
    sfx()
    music()
