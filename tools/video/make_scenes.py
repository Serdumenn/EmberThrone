import json
import os
import subprocess
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
SOURCE = os.path.join(ROOT, "ArtSource", "video")
MANIFEST = os.path.join(SOURCE, "scenes.json")


def ffmpeg():
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except ImportError:
        return "ffmpeg"


def run(args):
    subprocess.run([ffmpeg(), "-hide_banner", "-loglevel", "error", "-y"] + args, check=True)


def make(scene):
    src = os.path.join(SOURCE, scene["file"])
    clip = os.path.normpath(os.path.join(ROOT, scene["clip"]))
    poster = os.path.normpath(os.path.join(ROOT, scene["poster"]))
    os.makedirs(os.path.dirname(clip), exist_ok=True)
    os.makedirs(os.path.dirname(poster), exist_ok=True)
    start = ["-ss", str(scene.get("start", 0))]
    span = start + ["-i", src] + (["-t", str(scene["length"])] if "length" in scene else [])
    video = ["-c:v", "libvpx", "-b:v", scene.get("bitrate", "1600k"), "-crf", "10", "-deadline", "good",
             "-cpu-used", "1", "-auto-alt-ref", "0", "-pix_fmt", "yuv420p"]
    audio = ["-c:a", "libvorbis", "-b:a", "96k"] if scene.get("audio", True) else ["-an"]
    run(span + video + audio + [clip])
    run(start + ["-i", src, "-frames:v", "1", "-q:v", "3", poster])
    return clip, poster


if __name__ == "__main__":
    wanted = set(sys.argv[1:])
    with open(MANIFEST, encoding="utf-8") as f:
        scenes = json.load(f)["scenes"]
    for scene in scenes:
        if wanted and scene["file"] not in wanted and os.path.basename(scene["clip"]) not in wanted:
            continue
        for path in make(scene):
            print(os.path.relpath(path, ROOT), os.path.getsize(path))
