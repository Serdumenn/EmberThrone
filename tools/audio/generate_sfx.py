import math
import os
import random
import struct
import sys
import wave

RATE = 44100
PEAK = 10 ** (-3 / 20)
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "EmberThrone", "Resources", "Audio")


def silence(seconds):
    return [0.0] * int(RATE * seconds)


def mix(base, layer, at=0.0, gain=1.0):
    start = int(RATE * at)
    need = start + len(layer)
    if need > len(base):
        base.extend([0.0] * (need - len(base)))
    for i, v in enumerate(layer):
        base[start + i] += v * gain
    return base


def note(name):
    names = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
    semis = names[name[0]] + (1 if "#" in name else 0) - (1 if "b" in name[1:-1] else 0)
    octave = int(name[-1])
    return 440.0 * 2 ** ((semis - 9) / 12 + (octave - 4))


def tone(f0, f1, seconds, decay, partials=((1.0, 1.0),), glide=0.02, attack=0.002, vibrato=0.0, rate=6.0):
    n = int(RATE * seconds)
    out = []
    phases = [0.0] * len(partials)
    for i in range(n):
        t = i / RATE
        k = min(1.0, t / glide) if glide > 0 else 1.0
        f = f0 + (f1 - f0) * (1 - (1 - k) ** 3)
        f *= 1 + vibrato * math.sin(2 * math.pi * rate * t)
        env = min(1.0, t / attack) * math.exp(-t / decay)
        v = 0.0
        for p, (mult, amp) in enumerate(partials):
            phases[p] += 2 * math.pi * f * mult / RATE
            part_env = math.exp(-t / (decay / (1 + (mult - 1) * 0.6)))
            v += math.sin(phases[p]) * amp * part_env
        out.append(v * env)
    return out


def noise(seconds, decay, low=0.0, high=1.0, attack=0.002, seed=1, sweep=None):
    rng = random.Random(seed)
    n = int(RATE * seconds)
    out = []
    lp = 0.0
    hp_in = 0.0
    hp = 0.0
    for i in range(n):
        t = i / RATE
        a = high if sweep is None else sweep[0] + (sweep[1] - sweep[0]) * (i / n)
        lp += (rng.random() * 2 - 1 - lp) * a
        hp = (1 - low) * (hp + lp - hp_in)
        hp_in = lp
        env = min(1.0, t / attack) * math.exp(-t / decay)
        out.append(hp * env)
    return out


def click(seconds=0.003, gain=0.35, seed=7):
    rng = random.Random(seed)
    n = int(RATE * seconds)
    return [(rng.random() * 2 - 1) * gain * (1 - i / n) for i in range(n)]


def marimba(freq, seconds=0.35, decay=0.09, bright=0.28):
    body = tone(freq * 1.03, freq, seconds, decay, ((1.0, 1.0), (4.0, bright), (10.0, bright * 0.25)), glide=0.012)
    return mix(body, click(0.002, 0.18), 0.0)


def bell(freq, seconds=0.6, decay=0.18, gain=1.0):
    return [v * gain for v in tone(freq, freq, seconds, decay, ((1.0, 1.0), (2.76, 0.35), (5.4, 0.18), (8.9, 0.06)), glide=0.0, attack=0.001)]


def fade_tail(samples, seconds=0.02):
    n = min(len(samples), int(RATE * seconds))
    for i in range(n):
        samples[len(samples) - n + i] *= 1 - i / n
    return samples


def normalize(samples, peak=PEAK):
    top = max(abs(v) for v in samples) or 1.0
    return [math.tanh(v / top * 1.1) / math.tanh(1.1) * peak for v in samples]


def swap():
    s = tone(520, 860, 0.11, 0.04, ((1.0, 1.0), (2.0, 0.12)), glide=0.06)
    return mix(s, noise(0.08, 0.025, 0.6, 0.5, seed=3), 0.0, 0.25)


def swap_back():
    s = marimba(note("E4"), 0.14, 0.045, 0.18)
    return mix(s, marimba(note("C4"), 0.22, 0.06, 0.15), 0.09)


def match():
    s = marimba(note("C5"), 0.32, 0.085, 0.3)
    return mix(s, bell(note("C6"), 0.25, 0.05, 0.18), 0.0)


def special_made():
    s = silence(0.0)
    for i, n in enumerate(("G5", "C6", "E6")):
        mix(s, marimba(note(n), 0.3, 0.08, 0.26), i * 0.05)
    for i, n in enumerate(("G6", "C7")):
        mix(s, bell(note(n), 0.4, 0.1, 0.22), 0.14 + i * 0.05)
    return s


def rocket():
    s = noise(0.42, 0.16, 0.2, 0.08, attack=0.01, seed=11, sweep=(0.04, 0.5))
    mix(s, tone(note("C5"), note("C7"), 0.36, 0.14, ((1.0, 1.0), (2.0, 0.2)), glide=0.28, attack=0.01), 0.0, 0.45)
    return mix(s, click(0.003, 0.4, 5), 0.0)


def bomb():
    s = [v * 0.5 for v in tone(140, 38, 0.7, 0.22, ((1.0, 1.0), (1.5, 0.3)), glide=0.25, attack=0.002)]
    mix(s, tone(460, 170, 0.45, 0.1, ((1.0, 1.0), (2.0, 0.5), (3.1, 0.25)), glide=0.18, attack=0.002), 0.0, 0.7)
    mix(s, noise(0.5, 0.12, 0.25, 0.35, seed=23), 0.0, 1.1)
    mix(s, noise(0.6, 0.16, 0.0, 0.18, seed=21), 0.0, 0.9)
    mix(s, noise(0.12, 0.03, 0.4, 0.9, seed=22), 0.0, 0.5)
    return mix(s, bell(note("G6"), 0.3, 0.06, 0.08), 0.02)


def color_bomb():
    s = silence(0.0)
    for i, n in enumerate(("C6", "E6", "G6", "B6", "D7", "G7")):
        mix(s, bell(note(n), 0.6, 0.16, 0.5), i * 0.045)
    mix(s, tone(note("C4"), note("C6"), 0.7, 0.3, ((1.0, 1.0), (1.5, 0.4), (2.0, 0.3)), glide=0.5, attack=0.04, vibrato=0.01), 0.0, 0.45)
    return mix(s, noise(0.7, 0.25, 0.7, 0.6, attack=0.06, seed=31), 0.0, 0.18)


def combo():
    s = bomb()
    mix(s, color_bomb(), 0.04, 0.8)
    mix(s, tone(note("C3"), note("C3"), 0.9, 0.35, ((1.0, 1.0), (2.0, 0.4))), 0.0, 0.5)
    return s


def firework():
    s = tone(note("E5"), note("B6"), 0.2, 0.08, ((1.0, 1.0), (2.0, 0.15)), glide=0.16, attack=0.005)
    mix(s, noise(0.18, 0.05, 0.5, 0.7, seed=41), 0.12, 0.4)
    return mix(s, bell(note("E7"), 0.25, 0.06, 0.25), 0.14)


def collect():
    s = bell(note("E6"), 0.35, 0.09)
    return mix(s, bell(note("B6"), 0.4, 0.1, 0.7), 0.05)


def hammer():
    s = tone(220, 120, 0.25, 0.06, ((1.0, 1.0), (2.3, 0.4)), glide=0.04)
    mix(s, noise(0.1, 0.02, 0.3, 0.8, seed=51), 0.0, 0.9)
    return mix(s, bell(note("A5"), 0.3, 0.07, 0.25), 0.01)


def line_blast():
    s = rocket()
    return mix(s, rocket(), 0.03, 0.6)


def shuffle():
    s = silence(0.0)
    for i in range(8):
        mix(s, marimba(note(("C5", "E5", "G5", "D5", "F5", "A5", "E5", "C6")[i]), 0.12, 0.03, 0.2), i * 0.045, 0.7)
    return mix(s, noise(0.4, 0.2, 0.6, 0.5, attack=0.03, seed=61), 0.0, 0.15)


def ui_tap():
    return tone(620, 1100, 0.085, 0.03, ((1.0, 1.0), (2.0, 0.1)), glide=0.045)


def win():
    s = silence(0.0)
    for i, n in enumerate(("C5", "E5", "G5", "C6")):
        mix(s, marimba(note(n), 0.32, 0.09, 0.24), i * 0.09)
    mix(s, marimba(note("E6"), 0.9, 0.26, 0.2), 0.36)
    mix(s, marimba(note("G5"), 0.9, 0.26, 0.2), 0.36, 0.7)
    for i, n in enumerate(("G6", "C7", "E7")):
        mix(s, bell(note(n), 0.5, 0.12, 0.25), 0.4 + i * 0.06)
    return s


def lose():
    s = marimba(note("G4"), 0.3, 0.1, 0.2)
    mix(s, marimba(note("E4"), 0.3, 0.1, 0.2), 0.16)
    mix(s, marimba(note("C4"), 0.7, 0.22, 0.18), 0.32)
    return mix(s, tone(note("C3"), note("C3"), 0.7, 0.2), 0.32, 0.35)


def star():
    s = bell(note("G6"), 0.45, 0.12)
    mix(s, bell(note("D7"), 0.4, 0.1, 0.4), 0.03)
    return mix(s, marimba(note("G5"), 0.3, 0.08, 0.2), 0.0, 0.6)


def moves_added():
    s = tone(note("G5"), note("D6"), 0.22, 0.07, ((1.0, 1.0), (2.0, 0.18)), glide=0.08)
    mix(s, tone(note("D6"), note("G6"), 0.3, 0.09, ((1.0, 1.0), (3.0, 0.1)), glide=0.06), 0.09, 0.8)
    return mix(s, bell(note("B6"), 0.4, 0.1, 0.3), 0.18)


def freeze():
    s = silence(0.0)
    for i, n in enumerate(("E7", "B6", "G6", "E6")):
        mix(s, bell(note(n), 0.4, 0.09, 0.45), i * 0.035)
    mix(s, noise(0.45, 0.18, 0.8, 0.6, attack=0.03, seed=71), 0.0, 0.3)
    return mix(s, tone(note("B5"), note("E5"), 0.35, 0.12, ((1.0, 1.0), (2.0, 0.2)), glide=0.3, attack=0.02), 0.0, 0.25)


def break_wood():
    s = tone(170, 85, 0.18, 0.05, ((1.0, 1.0), (2.7, 0.35)), glide=0.05)
    mix(s, noise(0.16, 0.04, 0.25, 0.7, seed=81), 0.0, 0.8)
    mix(s, click(gain=0.4, seed=82), 0.0)
    return mix(s, noise(0.1, 0.03, 0.5, 0.9, seed=83), 0.05, 0.5)


def break_ice():
    s = silence(0.0)
    for i, n in enumerate(("B7", "F7", "D8", "G7", "C8")):
        mix(s, bell(note(n), 0.22, 0.05, 0.35), i * 0.018)
    mix(s, noise(0.25, 0.07, 0.75, 0.95, seed=91), 0.0, 0.55)
    return mix(s, click(gain=0.5, seed=92), 0.0)


def break_ash():
    s = noise(0.3, 0.1, 0.1, 0.45, attack=0.01, seed=101)
    mix(s, tone(120, 70, 0.2, 0.06), 0.0, 0.5)
    return mix(s, noise(0.12, 0.04, 0.4, 0.7, seed=102), 0.03, 0.4)


SOUNDS = {
    "sfx_swap": swap,
    "sfx_swap_back": swap_back,
    "sfx_match": match,
    "sfx_special_made": special_made,
    "sfx_rocket": rocket,
    "sfx_bomb": bomb,
    "sfx_color_bomb": color_bomb,
    "sfx_combo": combo,
    "sfx_firework": firework,
    "sfx_collect": collect,
    "sfx_hammer": hammer,
    "sfx_line_blast": line_blast,
    "sfx_shuffle": shuffle,
    "sfx_ui_tap": ui_tap,
    "sfx_win": win,
    "sfx_lose": lose,
    "sfx_star": star,
    "sfx_moves_added": moves_added,
    "sfx_freeze": freeze,
    "sfx_break_wood": break_wood,
    "sfx_break_ice": break_ice,
    "sfx_break_ash": break_ash,
}


def write(name, samples):
    samples = normalize(fade_tail(samples))
    path = os.path.normpath(os.path.join(OUT, name + ".wav"))
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, v)) * 32767)) for v in samples))
    return path, len(samples) / RATE


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    wanted = sys.argv[1:] or list(SOUNDS)
    for name in wanted:
        path, seconds = write(name, SOUNDS[name]())
        print(f"{name}: {seconds:.2f}s -> {path}")
