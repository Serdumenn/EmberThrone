import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
MASCOT = os.path.join(ROOT, "Assets", "EmberThrone", "Resources", "UI", "Art", "mascot.png")
OUT = os.path.join(ROOT, "Assets", "EmberThrone", "Art", "AppIcon")
SIZE = 1024
CENTER_GLOW = (255, 158, 64)
OUTLINE = (120, 22, 18)
MIDDLE = (232, 92, 34)
EDGE = (110, 18, 30)
RAY = (255, 214, 140)
SPARK = (255, 236, 170)


def lerp(a, b, t):
    return tuple(int(round(x + (y - x) * t)) for x, y in zip(a, b))


def radial(size):
    img = Image.new("RGB", (size, size))
    px = img.load()
    c = size / 2
    for y in range(size):
        for x in range(size):
            d = min(1.0, math.hypot(x - c, y - c * 0.92) / (size * 0.72))
            px[x, y] = lerp(CENTER_GLOW, MIDDLE, d / 0.45) if d < 0.45 else lerp(MIDDLE, EDGE, (d - 0.45) / 0.55)
    return img


def rays(size, count=14, alpha=34):
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    c = size / 2
    reach = size
    for i in range(count):
        a0 = (i / count) * math.tau
        a1 = a0 + math.tau / count * 0.45
        pts = [(c, c * 0.92), (c + math.cos(a0) * reach, c * 0.92 + math.sin(a0) * reach), (c + math.cos(a1) * reach, c * 0.92 + math.sin(a1) * reach)]
        draw.polygon(pts, fill=RAY + (alpha,))
    return layer.filter(ImageFilter.GaussianBlur(size / 120))


def sparks(size, count=26, seed=5):
    rng = random.Random(seed)
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for _ in range(count):
        angle = rng.random() * math.tau
        dist = size * (0.3 + rng.random() * 0.2)
        x = size / 2 + math.cos(angle) * dist
        y = size * 0.46 + math.sin(angle) * dist
        r = size * (0.004 + rng.random() * 0.008)
        draw.ellipse((x - r, y - r, x + r, y + r), fill=SPARK + (int(150 + rng.random() * 100),))
    glow = layer.filter(ImageFilter.GaussianBlur(size / 160))
    return Image.alpha_composite(glow, layer)


def background(size):
    img = radial(size).convert("RGBA")
    img = Image.alpha_composite(img, rays(size))
    halo = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(halo).ellipse((size * 0.22, size * 0.18, size * 0.78, size * 0.74), fill=CENTER_GLOW + (120,))
    img = Image.alpha_composite(img, halo.filter(ImageFilter.GaussianBlur(size / 14)))
    return Image.alpha_composite(img, sparks(size))


def foreground(size, fill):
    mascot = Image.open(MASCOT).convert("RGBA")
    mascot = mascot.crop(mascot.getbbox())
    scale = size * fill / max(mascot.size)
    mascot = mascot.resize((int(mascot.width * scale), int(mascot.height * scale)), Image.LANCZOS)
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    x = (size - mascot.width) // 2
    y = (size - mascot.height) // 2 + int(size * 0.01)
    shadow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse((size * 0.32, y + mascot.height * 0.9, size * 0.68, y + mascot.height * 1.02), fill=(60, 8, 12, 150))
    layer = Image.alpha_composite(layer, shadow.filter(ImageFilter.GaussianBlur(size / 60)))
    alpha = Image.new("L", (size, size), 0)
    alpha.paste(mascot.getchannel("A"), (x, y))
    grow = int(size * 0.008) * 2 + 1
    edge = alpha.filter(ImageFilter.MaxFilter(grow)).filter(ImageFilter.GaussianBlur(size / 400))
    outline = Image.new("RGBA", (size, size), OUTLINE + (0,))
    outline.putalpha(edge)
    layer = Image.alpha_composite(layer, outline)
    layer.alpha_composite(mascot, (x, y))
    return layer


def rounded(img, radius):
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, img.width - 1, img.height - 1), radius, fill=255)
    out = img.copy()
    out.putalpha(mask)
    return out


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    bg = background(SIZE)
    fg = foreground(SIZE, 0.6)
    bg.save(os.path.join(OUT, "app_icon_bg.png"))
    fg.save(os.path.join(OUT, "app_icon_fg.png"))
    legacy = Image.alpha_composite(bg, foreground(SIZE, 0.74))
    rounded(legacy, SIZE * 0.18).save(os.path.join(OUT, "app_icon_legacy.png"))
    legacy.resize((512, 512), Image.LANCZOS).convert("RGB").save(os.path.join(ROOT, "docs", "release", "store_icon_512.png"))
    print("written", OUT)
