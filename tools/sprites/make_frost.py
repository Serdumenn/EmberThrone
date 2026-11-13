import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/EmberThrone/Resources/Board/ob_ice.png"
TARGET = ROOT / "Assets/EmberThrone/Resources/Board/ob_frost.png"
ICON = ROOT / "Assets/EmberThrone/Resources/Board/ob_frost_icon.png"

RIME = np.array([226.0, 238.0, 255.0])
SNOW = np.array([252.0, 254.0, 255.0])
SHADE = np.array([150.0, 178.0, 230.0])


def branch(draw, x, y, angle, length, width, depth, rng):
    if depth == 0 or length < 5:
        return
    steps = 4
    bend = rng.uniform(-0.18, 0.18)
    px, py, a = x, y, angle
    for i in range(steps):
        a += bend + rng.uniform(-0.1, 0.1)
        nx = px + math.cos(a) * length / steps
        ny = py + math.sin(a) * length / steps
        draw.line([(px, py), (nx, ny)], fill=255, width=max(1, int(width)))
        r = width * 0.5
        draw.ellipse([nx - r, ny - r, nx + r, ny + r], fill=255)
        side = length * rng.uniform(0.25, 0.42) * (1 - i / (steps + 1))
        for s in (-1, 1):
            if rng.random() < 0.8:
                branch(draw, nx, ny, a + s * rng.uniform(0.85, 1.2), side, width * 0.6, depth - 1, rng)
        px, py = nx, ny


def ferns(size, rng):
    scale = 2
    s = size * scale
    layer = Image.new("L", (s, s), 0)
    draw = ImageDraw.Draw(layer)
    edge = 0.13
    for side in range(4):
        for k in range(4):
            t = (k + rng.uniform(0.2, 0.8)) / 4
            if side == 0:
                x, y, deg = t, edge, 90
            elif side == 1:
                x, y, deg = 1 - edge, t, 180
            elif side == 2:
                x, y, deg = 1 - t, 1 - edge, 270
            else:
                x, y, deg = edge, 1 - t, 0
            a = math.radians(deg) + rng.uniform(-0.6, 0.6)
            branch(draw, x * s, y * s, a, s * rng.uniform(0.11, 0.2), s * 0.016, 4, rng)
    return layer.resize((size, size), Image.LANCZOS)


def sparkle(draw, x, y, r):
    draw.polygon([(x, y - r), (x + r * 0.18, y - r * 0.18), (x + r, y), (x + r * 0.18, y + r * 0.18),
                  (x, y + r), (x - r * 0.18, y + r * 0.18), (x - r, y), (x - r * 0.18, y - r * 0.18)], fill=255)


def main():
    rng = random.Random(29)
    ice = Image.open(SOURCE).convert("RGBA")
    size = ice.width
    arr = np.asarray(ice).astype(np.float64)
    rgb = arr[..., :3]
    alpha = arr[..., 3] / 255.0
    inside = (alpha > 0.02).astype(np.float64)

    lum = rgb.mean(axis=2, keepdims=True) / 255.0
    base = RIME * (0.78 + 0.3 * lum) * 0.62 + rgb * 0.38

    blurred = np.asarray(Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(size * 0.045))) / 255.0
    rim = np.clip((1.0 - blurred) * 2.6, 0, 1) * inside

    noise = Image.effect_noise((size, size), 64).filter(ImageFilter.GaussianBlur(2.2))
    noise = np.asarray(noise).astype(np.float64) / 255.0
    crust = np.clip(rim * (0.55 + 0.9 * (noise - 0.5)) * 1.4, 0, 1)

    inner = np.asarray(Image.fromarray((inside * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(size * 0.09))) / 255.0
    shade = np.clip(1.0 - inner, 0, 1) * inside * 0.25

    fern = np.asarray(ferns(size, rng)).astype(np.float64) / 255.0
    glow = np.asarray(Image.fromarray((fern * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(size * 0.012))) / 255.0
    fern = np.clip(fern * inner * 1.3, 0, 1)
    glow = np.clip(glow * inner * 1.6, 0, 1)

    stars = Image.new("L", (size, size), 0)
    sd = ImageDraw.Draw(stars)
    for _ in range(5):
        sparkle(sd, rng.uniform(0.25, 0.75) * size, rng.uniform(0.25, 0.75) * size, rng.uniform(0.025, 0.045) * size)
    stars = np.asarray(stars.filter(ImageFilter.GaussianBlur(0.8))).astype(np.float64) / 255.0 * inside

    out = base * (1 - shade[..., None]) + SHADE * shade[..., None]
    out = out * (1 - glow[..., None] * 0.5) + SNOW * glow[..., None] * 0.5
    out = out * (1 - crust[..., None]) + SNOW * crust[..., None]
    out = out * (1 - fern[..., None]) + SNOW * fern[..., None]
    out = out * (1 - stars[..., None]) + SNOW * stars[..., None]

    edge = np.clip(1.0 - inner, 0, 1)
    a = np.clip(alpha * (0.42 + 0.5 * edge) + crust * 0.3 + fern * 0.45 + glow * 0.12, 0, 1) * (alpha > 0.0)
    a = np.minimum(a, np.clip(alpha * 1.35, 0, 1))
    result = np.dstack([np.clip(out, 0, 255), a * 255]).astype(np.uint8)
    folder = Path(sys.argv[1]) if len(sys.argv) > 1 else TARGET.parent
    frost = Image.fromarray(result, "RGBA")
    frost.save(folder / TARGET.name, optimize=True)
    deep = np.dstack([np.clip(rgb * np.array([0.42, 0.6, 0.95]), 0, 255), arr[..., 3]]).astype(np.uint8)
    icon = Image.fromarray(deep, "RGBA")
    veil = result.copy()
    veil[..., 3] = (veil[..., 3].astype(np.float64) * np.clip(0.35 + fern * 0.65 + crust * 0.5, 0, 1)).astype(np.uint8)
    icon.alpha_composite(Image.fromarray(veil, "RGBA"))
    icon.save(folder / ICON.name, optimize=True)
    print(folder / TARGET.name)
    print(folder / ICON.name)


if __name__ == "__main__":
    main()
