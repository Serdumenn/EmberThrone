import argparse
import csv
import json
import os
import sys

import numpy as np
from PIL import Image

COLORS = {
    'green': lambda r, g, b: (g > 110) & (g > r + 35) & (g > b + 35),
    'yellow': lambda r, g, b: (r > 190) & (g > 130) & (b < 110) & (r > b + 100),
    'red': lambda r, g, b: (r > 180) & (g < 90) & (b < 110),
    'blue': lambda r, g, b: (b > 150) & (b > r + 70) & (b > g + 20),
    'purple': lambda r, g, b: (b > 150) & (r > 100) & (g < 110) & (b > g + 70),
}


def video_frames(path, start, end):
    import cv2
    cap = cv2.VideoCapture(path)
    cap.set(cv2.CAP_PROP_POS_MSEC, max(0.0, start - 0.5) * 1000)
    while True:
        ok, frame = cap.read()
        if not ok:
            return
        t = cap.get(cv2.CAP_PROP_POS_MSEC) / 1000
        if t < start:
            continue
        if t > end:
            return
        yield t, frame[:, :, ::-1]


def folder_frames(path):
    times_csv = os.path.join(path, 'times.csv')
    times_json = os.path.join(path, 'times.json')
    if os.path.exists(times_csv):
        with open(times_csv) as f:
            times = [float(row['time']) for row in csv.DictReader(f)]
    elif os.path.exists(times_json):
        with open(times_json) as f:
            times = json.load(f)
    else:
        sys.exit('times.csv or times.json is missing in ' + path)
    names = sorted(n for n in os.listdir(path) if n.endswith('.jpg') or n.endswith('.png'))
    for t, name in zip(times, names):
        yield t, np.asarray(Image.open(os.path.join(path, name)).convert('RGB'))


def main():
    parser = argparse.ArgumentParser(description='Track a colored piece frame by frame.')
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument('--video')
    source.add_argument('--frames')
    parser.add_argument('--start', type=float, default=0.0)
    parser.add_argument('--end', type=float, default=1e9)
    parser.add_argument('--box', required=True, help='x0,y0,x1,y1')
    parser.add_argument('--color', choices=sorted(COLORS), required=True)
    parser.add_argument('--origin', type=float, help='time that counts as 0 ms')
    args = parser.parse_args()

    x0, y0, x1, y1 = (int(v) for v in args.box.split(','))
    frames = video_frames(args.video, args.start, args.end) if args.video else folder_frames(args.frames)
    origin = args.origin
    print('ms,pixels,x,y,height')
    for t, rgb in frames:
        if origin is None:
            origin = t
        area = rgb[y0:y1, x0:x1].astype(int)
        mask = COLORS[args.color](area[..., 0], area[..., 1], area[..., 2])
        ys, xs = np.nonzero(mask)
        if len(xs):
            print(f'{1000 * (t - origin):.0f},{len(xs)},{xs.mean() + x0:.1f},{ys.mean() + y0:.1f},{ys.max() - ys.min() + 1}')
        else:
            print(f'{1000 * (t - origin):.0f},0,,,0')


if __name__ == '__main__':
    main()
