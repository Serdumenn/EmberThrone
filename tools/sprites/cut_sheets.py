import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
SHEETS = os.path.join(ROOT, "ArtSource", "sheets")
MANIFEST = os.path.join(SHEETS, "sheets.json")
LOOSE = 30
TIGHT = 9
MIN_HOLE = 120
SOFT_DARKER = 4


def background_of(rgb):
    h, w, _ = rgb.shape
    k = max(4, min(h, w) // 60)
    corners = np.concatenate([rgb[:k, :k].reshape(-1, 3), rgb[:k, -k:].reshape(-1, 3), rgb[-k:, :k].reshape(-1, 3), rgb[-k:, -k:].reshape(-1, 3)])
    return np.median(corners, axis=0)


def flood(mask, seeds):
    img = Image.fromarray((mask * 255).astype(np.uint8), "L").copy()
    for x, y in seeds:
        if img.getpixel((x, y)) == 255:
            ImageDraw.floodfill(img, (x, y), 128)
    return np.asarray(img) == 128


def border_seeds(w, h, step=16):
    seeds = []
    for x in range(0, w, step):
        seeds += [(x, 0), (x, h - 1)]
    for y in range(0, h, step):
        seeds += [(0, y), (w - 1, y)]
    return seeds


def enclosed_holes(dist, outside):
    tight = (dist < TIGHT) & ~outside
    core = np.asarray(Image.fromarray((tight * 255).astype(np.uint8), "L").filter(ImageFilter.MinFilter(7))) > 0
    holes = np.zeros_like(tight)
    img = Image.fromarray((tight * 255).astype(np.uint8), "L").copy()
    ys, xs = np.nonzero(core)
    for i in range(0, len(xs), 97):
        x, y = int(xs[i]), int(ys[i])
        if img.getpixel((x, y)) != 255:
            continue
        ImageDraw.floodfill(img, (x, y), 128)
        region = np.asarray(img) == 128
        if region.sum() >= MIN_HOLE:
            holes |= region
        img = Image.fromarray(np.where(region, 0, np.asarray(img)).astype(np.uint8), "L").copy()
    return holes


def matte(rgb, keep_holes, seeds=(), loose=LOOSE, soft=None):
    bg = background_of(rgb)
    dist = np.abs(rgb.astype(np.float32) - bg).max(axis=2)
    h, w = dist.shape
    outside = flood(dist < loose, border_seeds(w, h) + [(int(u * w), int(v * h)) for u, v in seeds])
    if not keep_holes:
        outside |= enclosed_holes(dist, outside)
    solid = Image.fromarray((~outside * 255).astype(np.uint8), "L")
    solid = solid.filter(ImageFilter.MinFilter(3))
    alpha = np.asarray(solid.filter(ImageFilter.GaussianBlur(1.2))).astype(np.float32) / 255.0
    alpha = np.clip((alpha - 0.35) / 0.5, 0.0, 1.0)

    color = rgb.astype(np.float32)
    if soft:
        region = np.zeros((h, w), dtype=bool)
        for x0, y0, x1, y1 in soft.get("boxes", [[0, 0, 1, 1]]):
            region[int(y0 * h):int(y1 * h), int(x0 * w):int(x1 * w)] = True
        region &= rgb.max(axis=2) >= bg.max() - SOFT_DARKER
        glow = np.where(region, np.clip((dist - loose) / (soft["to"] - loose), 0.0, 1.0), 1.0)
        unmixed = bg + (color - bg) / np.maximum(glow[..., None], 0.08)
        color = np.where(glow[..., None] < 1.0, np.clip(unmixed, 0, 255), color)
        alpha = alpha * glow
    known = np.asarray(solid).astype(np.float32) / 255.0
    for _ in range(6):
        weight = np.asarray(Image.fromarray((known * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(2))).astype(np.float32) / 255.0
        spread = np.stack([
            np.asarray(Image.fromarray(np.clip(color[..., c] * known, 0, 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(2))).astype(np.float32)
            for c in range(3)], axis=2)
        fill = spread / np.maximum(weight[..., None], 1e-3)
        grow = (known < 0.5) & (weight > 0.02)
        color[grow] = fill[grow]
        known = np.where(grow, 1.0, known)
    return np.dstack([np.clip(color, 0, 255), alpha * 255.0]).astype(np.uint8)


def adjust(rgba, hue=0.0, saturation=1.0, value=1.0):
    if hue == 0.0 and saturation == 1.0 and value == 1.0:
        return rgba
    rgb = rgba[..., :3].astype(np.float32) / 255.0
    hsv = np.asarray(Image.fromarray((rgb * 255).astype(np.uint8), "RGB").convert("HSV")).astype(np.float32)
    hsv[..., 0] = (hsv[..., 0] + hue * 255.0) % 255.0
    hsv[..., 1] = np.clip(hsv[..., 1] * saturation, 0, 255)
    hsv[..., 2] = np.clip(hsv[..., 2] * value, 0, 255)
    out = np.asarray(Image.fromarray(hsv.astype(np.uint8), "HSV").convert("RGB"))
    return np.dstack([out, rgba[..., 3]])


def glass(rgba, center):
    alpha = rgba[..., 3].astype(np.float32)
    ys, xs = np.nonzero(alpha > 128)
    cx, cy = (xs.min() + xs.max()) / 2.0, (ys.min() + ys.max()) / 2.0
    hx, hy = (xs.max() - xs.min()) / 2.0, (ys.max() - ys.min()) / 2.0
    yy, xx = np.mgrid[0:alpha.shape[0], 0:alpha.shape[1]]
    d = np.maximum(np.abs(xx - cx) / hx, np.abs(yy - cy) / hy)
    t = np.clip((d - 0.5) / 0.42, 0.0, 1.0)
    t = t * t * (3.0 - 2.0 * t)
    out = rgba.copy()
    out[..., 3] = (alpha * (center + (1.0 - center) * t)).astype(np.uint8)
    return out


def axis_angle(rgba):
    ys, xs = np.nonzero(rgba[..., 3] > 128)
    x = xs - xs.mean()
    y = ys - ys.mean()
    return float(np.degrees(0.5 * np.arctan2(2.0 * (x * y).mean(), (x * x).mean() - (y * y).mean())))


def place(rgba, size, fill, rotate=0, anchor="center", align=None):
    if align == "horizontal":
        rotate += axis_angle(rgba)
    elif align == "vertical":
        rotate += axis_angle(rgba) + 90.0
    img = Image.fromarray(rgba, "RGBA")
    box = img.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox()
    img = img.crop(box)
    if rotate:
        img = img.rotate(rotate, expand=True, resample=Image.BICUBIC)
    scale = size * fill / max(img.size)
    pre = img.convert("RGBa").resize((max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.LANCZOS).convert("RGBA")
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    x = (size - pre.width) // 2
    y = size - pre.height - round(size * (1 - fill) / 2) if anchor == "bottom" else (size - pre.height) // 2
    canvas.alpha_composite(pre, (x, y))
    return canvas


def cut(sheet):
    image = Image.open(os.path.join(SHEETS, sheet["file"])).convert("RGB")
    cols, rows = sheet["grid"]
    w, h = image.size
    written = []
    for index, cell in enumerate(sheet["cells"]):
        if cell is None:
            continue
        if "box" in cell:
            x0, y0, x1, y1 = [round(v * s) for v, s in zip(cell["box"], (w, h, w, h))]
        else:
            cx, cy = index % cols, index // cols
            x0, y0, x1, y1 = cx * w // cols, cy * h // rows, (cx + 1) * w // cols, (cy + 1) * h // rows
        rgba = matte(np.asarray(image.crop((x0, y0, x1, y1))), cell.get("keepHoles", False), cell.get("seeds", ()), cell.get("loose", LOOSE), cell.get("soft"))
        for ex0, ey0, ex1, ey1 in cell.get("erase", ()):
            ch, cw = rgba.shape[:2]
            rgba[int(ey0 * ch):int(ey1 * ch), int(ex0 * cw):int(ex1 * cw), 3] = 0
        rgba = adjust(rgba, cell.get("hue", 0.0), cell.get("saturation", 1.0), cell.get("value", 1.0))
        if "glass" in cell:
            rgba = glass(rgba, cell["glass"])
        for out in cell["out"]:
            tinted = adjust(rgba, out.get("hue", 0.0), out.get("saturation", 1.0), out.get("value", 1.0))
            sprite = place(tinted, out.get("size", sheet.get("size", 512)), out.get("fill", sheet.get("fill", 0.8)), out.get("rotate", 0), out.get("anchor", "center"), out.get("align"))
            path = os.path.normpath(os.path.join(ROOT, out["path"]))
            os.makedirs(os.path.dirname(path), exist_ok=True)
            sprite.save(path)
            written.append(path)
    return written


def export(image):
    source = Image.open(os.path.join(SHEETS, image["file"])).convert("RGB")
    written = []
    for out in image["out"]:
        part = source
        if "crop" in out:
            part = source.crop(tuple(round(v * s) for v, s in zip(out["crop"], (source.width, source.height, source.width, source.height))))
        width = out.get("width", part.width)
        picture = part.resize((width, round(part.height * width / part.width)), Image.LANCZOS)
        if out.get("blur"):
            picture = picture.filter(ImageFilter.GaussianBlur(out["blur"]))
        path = os.path.normpath(os.path.join(ROOT, out["path"]))
        os.makedirs(os.path.dirname(path), exist_ok=True)
        picture.save(path, quality=out.get("quality", 90), optimize=True, progressive=False)
        written.append(path)
    return written


def element(image, box, loose=LOOSE):
    w, h = image.size
    crop = image.crop(tuple(round(v * s) for v, s in zip(box, (w, h, w, h))))
    rgba = Image.fromarray(matte(np.asarray(crop), True, (), loose), "RGBA")
    return rgba.crop(rgba.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox())


def premultiplied(img):
    a = np.asarray(img).astype(np.float32)
    a[..., :3] *= a[..., 3:4] / 255.0
    return a


def straight(a):
    out = a.copy()
    alpha = np.maximum(out[..., 3:4], 1e-3)
    out[..., :3] = np.clip(out[..., :3] * 255.0 / alpha, 0, 255)
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA")


def ramp(n):
    return np.linspace(0.0, 1.0, n, dtype=np.float32)


def slice_h(a, left, right, width, fade=14):
    h, w = a.shape[:2]
    mid = a[:, int(w * 0.49):int(w * 0.51) + 1].mean(axis=1, keepdims=True)
    out = np.repeat(mid, width, axis=1)
    lcap, rcap = a[:, :left].copy(), a[:, w - right:].copy()
    t = ramp(fade)[None, :, None]
    lcap[:, left - fade:] = lcap[:, left - fade:] * (1 - t) + mid * t
    rcap[:, :fade] = rcap[:, :fade] * t + mid * (1 - t)
    out[:, :left] = lcap
    out[:, width - right:] = rcap
    return out


def slice_v(a, top, bottom, height, fade=14):
    return slice_h(a.transpose(1, 0, 2), top, bottom, height, fade).transpose(1, 0, 2)


def flatten(a, left, right):
    h, w = a.shape[:2]
    solid = a[..., 3] > 128
    tops = np.where(solid.any(axis=0), solid.argmax(axis=0), 0)
    level = int(round((tops[left] + tops[w - right - 1]) / 2.0))
    out = a.copy()
    for x in range(left, w - right):
        out[:, x] = np.roll(a[:, x], level - int(tops[x]), axis=0)
    return out


def chrome(item, image):
    art = element(image, item["box"], item.get("loose", LOOSE))
    kind = item["kind"]
    width, height = item["size"]
    if kind == "fit":
        scale = min(width / art.width, height / art.height) * item.get("fill", 1.0)
        art = art.convert("RGBa").resize((round(art.width * scale), round(art.height * scale)), Image.LANCZOS).convert("RGBA")
        art = Image.fromarray(adjust(np.asarray(art), 0.0, item.get("saturation", 1.0), item.get("value", 1.0)), "RGBA")
        out = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        out.alpha_composite(art, ((width - art.width) // 2, item.get("y", (height - art.height) // 2)))
        return out
    scale = item["scale"]
    art = art.convert("RGBa").resize((round(art.width * scale), round(art.height * scale)), Image.LANCZOS).convert("RGBA")
    a = premultiplied(art)
    left, top, right, bottom = item["slice"]
    fade = item.get("fade", 14)
    if item.get("flatten"):
        a = flatten(a, left, right)
    a = slice_h(a, left, right, width, fade)
    if kind == "nine":
        a = slice_v(a, top, bottom, height, fade)
    out = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    out.alpha_composite(straight(a), (0, item.get("y", 0)))
    return out


def stack(strip_map):
    rows = []
    for strip in strip_map["strips"]:
        picture = Image.open(os.path.join(SHEETS, strip["file"])).convert("RGB")
        rows.append(np.asarray(picture.crop((0, strip.get("top", 0), picture.width, picture.height - strip.get("bottom", 0)))).astype(np.float32))
    blend = strip_map.get("blend", 24)
    out = rows[0]
    for nxt in rows[1:]:
        t = np.linspace(0.0, 1.0, blend, dtype=np.float32)[:, None, None]
        seam = out[-blend:] * (1.0 - t) + nxt[:blend] * t
        out = np.concatenate([out[:-blend], seam, nxt[blend:]], axis=0)
    picture = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGB")
    width = strip_map.get("width", picture.width)
    picture = picture.resize((width, round(picture.height * width / picture.width)), Image.LANCZOS)
    path = os.path.normpath(os.path.join(ROOT, strip_map["path"]))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    picture.save(path, quality=strip_map.get("quality", 90), optimize=True, progressive=False)
    return path, picture.size


if __name__ == "__main__":
    wanted = set(sys.argv[1:])
    with open(MANIFEST, encoding="utf-8") as f:
        manifest = json.load(f)
    for sheet in manifest["sheets"]:
        if wanted and sheet["file"] not in wanted:
            continue
        for path in cut(sheet):
            print(os.path.relpath(path, ROOT))
    for image in manifest.get("images", []):
        if wanted and image["file"] not in wanted:
            continue
        for path in export(image):
            print(os.path.relpath(path, ROOT))
    for group in manifest.get("chrome", []):
        if wanted and group["file"] not in wanted:
            continue
        sheet_image = Image.open(os.path.join(SHEETS, group["file"])).convert("RGB")
        for item in group["items"]:
            path = os.path.normpath(os.path.join(ROOT, item["path"]))
            chrome(item, sheet_image).save(path)
            print(os.path.relpath(path, ROOT))
    for strip_map in manifest.get("maps", []):
        if wanted and os.path.basename(strip_map["path"]) not in wanted:
            continue
        path, size = stack(strip_map)
        print(os.path.relpath(path, ROOT), size)
