import json
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
ART = os.path.join(ROOT, "ArtSource")
RECIPE = os.path.join(ART, "restore", "restore.json")
OUT = os.path.join(ROOT, "Assets", "EmberThrone", "Resources", "UI", "Restore")
ICE = np.array([0.58, 0.78, 1.04], dtype=np.float32)
SNOW = np.array([0.93, 0.97, 1.0], dtype=np.float32)


def frozen(image):
    a = np.asarray(image).astype(np.float32) / 255.0
    lum = (a * np.array([0.299, 0.587, 0.114], dtype=np.float32)).sum(axis=2, keepdims=True)
    cold = (np.repeat(lum, 3, axis=2) * 0.74 + a * 0.26) * ICE
    cold = 0.15 + cold * 0.95
    edges = np.asarray(image.convert("L").filter(ImageFilter.FIND_EDGES).filter(ImageFilter.GaussianBlur(1.2))).astype(np.float32)[..., None] / 255.0
    noise = np.random.default_rng(3).random(a.shape[:2])[..., None]
    snow = np.clip((lum - 0.5) * 2.4, 0, 1) * 0.6 + (noise > 0.996) * 0.6 + edges * 0.25
    cold = cold + np.clip(snow, 0, 1) * (SNOW - cold)
    picture = Image.fromarray((np.clip(cold, 0, 1) * 255).astype(np.uint8))
    return Image.blend(picture, picture.filter(ImageFilter.GaussianBlur(0.8)), 0.4)


def patch(image, area, feather):
    w, h = image.size
    cx, cy, rx, ry = area
    box = (max(0, round((cx - rx) * w)), max(0, round((cy - ry) * h)), min(w, round((cx + rx) * w)), min(h, round((cy + ry) * h)))
    crop = image.crop(box)
    yy, xx = np.mgrid[box[1]:box[3], box[0]:box[2]].astype(np.float32)
    d = np.sqrt(((xx + 0.5) / w - cx) ** 2 / rx ** 2 + ((yy + 0.5) / h - cy) ** 2 / ry ** 2)
    t = np.clip((1.0 - d) / feather, 0.0, 1.0)
    alpha = (t * t * (3.0 - 2.0 * t) * 255).astype(np.uint8)
    rgba = crop.convert("RGBA")
    rgba.putalpha(Image.fromarray(alpha))
    return rgba, [box[0] / w, box[1] / h, box[2] / w, box[3] / h]


def make(district, width, feather):
    source = Image.open(os.path.join(ART, district["source"])).convert("RGB")
    scale = width / source.width
    warm = source.resize((width, round(source.height * scale)), Image.LANCZOS)
    name = district["name"]
    os.makedirs(OUT, exist_ok=True)
    warm.save(os.path.join(OUT, name + ".jpg"), quality=88, optimize=True)
    frozen(warm).save(os.path.join(OUT, name + "_frozen.jpg"), quality=86, optimize=True)
    areas = []
    for i, area in enumerate(district["areas"]):
        rgba, box = patch(warm, area, feather)
        image = name + "_" + str(i + 1)
        rgba.save(os.path.join(OUT, image + ".png"), optimize=True)
        areas.append({"image": image, "box": [round(v, 4) for v in box]})
    if "card" in district:
        x0, y0, x1, y1 = district["card"]
        crop = source.crop((round(x0 * source.width), round(y0 * source.height), round(x1 * source.width), round(y1 * source.height)))
        card = crop.resize((width, round(crop.height * width / crop.width)), Image.LANCZOS)
        card.save(os.path.normpath(os.path.join(ROOT, district["cardPath"])), quality=88, optimize=True)
    return {"warm": name, "frozen": name + "_frozen", "areas": areas}


if __name__ == "__main__":
    with open(RECIPE, encoding="utf-8") as f:
        recipe = json.load(f)
    wanted = set(sys.argv[1:])
    runtime_path = os.path.join(OUT, "restore.json")
    runtime = {"districts": []}
    if os.path.exists(runtime_path):
        with open(runtime_path, encoding="utf-8") as f:
            runtime = json.load(f)
    districts = runtime["districts"]
    while len(districts) < len(recipe["districts"]):
        districts.append(None)
    for i, district in enumerate(recipe["districts"]):
        if wanted and district["name"] not in wanted:
            continue
        districts[i] = make(district, recipe.get("width", 1152), recipe.get("feather", 0.35))
        print(district["name"], len(district["areas"]))
    runtime["districts"] = districts[:len(recipe["districts"])]
    with open(runtime_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(runtime, indent=2, ensure_ascii=False) + "\n")
