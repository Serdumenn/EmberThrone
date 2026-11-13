import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/EmberThrone/Resources/Board/bg_game.jpg"
TARGET = ROOT / "Assets/EmberThrone/Resources/Board/bg_game_calm.jpg"

NIGHT = np.array([16.0, 20.0, 60.0])
BLUR = 0.007
DIM = 0.42
TINT = 0.45
VIGNETTE = 0.55


def main():
    target = Path(sys.argv[1]) if len(sys.argv) > 1 else TARGET
    im = Image.open(SOURCE).convert("RGB")
    w, h = im.size
    im = im.filter(ImageFilter.GaussianBlur(w * BLUR))
    a = np.asarray(im).astype(np.float64)
    lum = a.mean(axis=2, keepdims=True)
    a = a * 0.75 + lum * 0.25
    a = a * DIM
    a = a * (1 - TINT) + NIGHT * TINT
    ys, xs = np.mgrid[0:h, 0:w]
    dx = (xs - w / 2) / (w / 2)
    dy = (ys - h * 0.45) / (h / 2)
    r = np.clip(np.sqrt(dx * dx * 0.8 + dy * dy), 0, 1.6)
    v = 1 - VIGNETTE * np.clip(r - 0.35, 0, 1) ** 1.6
    a = a * v[..., None] + NIGHT * (1 - v[..., None]) * 0.5
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(target, quality=90)
    print(target)


if __name__ == "__main__":
    main()
