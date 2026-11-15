import argparse

import cv2
from PIL import Image, ImageDraw, ImageFont

LABEL_HEIGHT = 20
STAMP_HEIGHT = 18
GAP = 3


def parse_row(text):
    label, start, end, every, crop = text.split(";")
    box = tuple(int(v) for v in crop.split(","))
    return label, float(start), float(end), max(1, int(every)), box


def frames(video, start, end, every, box, width):
    cap = cv2.VideoCapture(video)
    cap.set(cv2.CAP_PROP_POS_MSEC, max(0.0, start - 0.4) * 1000)
    taken = []
    index = 0
    x0, y0, x1, y1 = box
    while True:
        ok, frame = cap.read()
        if not ok:
            break
        t = cap.get(cv2.CAP_PROP_POS_MSEC) / 1000
        if t < start:
            continue
        if t > end:
            break
        if index % every == 0:
            image = Image.fromarray(frame[y0:y1, x0:x1][:, :, ::-1])
            height = int(image.height * width / image.width)
            taken.append((t, image.resize((width, height))))
        index += 1
    cap.release()
    return taken


def font():
    try:
        return ImageFont.truetype("DejaVuSans-Bold.ttf", 14)
    except OSError:
        return ImageFont.load_default()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("video")
    parser.add_argument("out")
    parser.add_argument("--row", action="append", required=True)
    parser.add_argument("--width", type=int, default=150)
    args = parser.parse_args()

    rows = []
    for text in args.row:
        label, start, end, every, box = parse_row(text)
        taken = frames(args.video, start, end, every, box, args.width)
        if taken:
            rows.append((label, taken))
    if not rows:
        raise SystemExit("no frames in the given ranges")

    columns = max(len(taken) for _, taken in rows)
    height = sum(LABEL_HEIGHT + STAMP_HEIGHT + taken[0][1].height + GAP for _, taken in rows)
    sheet = Image.new("RGB", (columns * (args.width + GAP), height), "white")
    draw = ImageDraw.Draw(sheet)
    face = font()
    y = 0
    for label, taken in rows:
        draw.text((2, y + 2), label, fill="blue", font=face)
        y += LABEL_HEIGHT
        for k, (t, image) in enumerate(taken):
            x = k * (args.width + GAP)
            draw.text((x + 2, y + 1), f"{t:.3f}", fill="black", font=face)
            sheet.paste(image, (x, y + STAMP_HEIGHT))
        y += STAMP_HEIGHT + taken[0][1].height + GAP
    sheet.save(args.out, quality=90)
    print(args.out, sheet.size, [len(taken) for _, taken in rows])


if __name__ == "__main__":
    main()
