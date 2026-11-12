#!/usr/bin/env python3

import os
import subprocess
import sys
import uuid

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets")
FOOTER = "  userData: \n  assetBundleName: \n  assetBundleVariant: \n"

FOLDER = "fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n" + FOOTER
SCRIPT = ("fileFormatVersion: 2\nguid: {guid}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n"
          "  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n" + FOOTER)
ASMDEF = "fileFormatVersion: 2\nguid: {guid}\nAssemblyDefinitionImporter:\n  externalObjects: {{}}\n" + FOOTER
TEXT = "fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n" + FOOTER
DEFAULT = "fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  externalObjects: {{}}\n" + FOOTER

BY_EXT = {
    ".cs": SCRIPT,
    ".asmdef": ASMDEF,
    ".asmref": ASMDEF.replace("AssemblyDefinitionImporter", "AssemblyDefinitionReferenceImporter"),
    ".json": TEXT, ".txt": TEXT, ".md": TEXT, ".csv": TEXT, ".xml": TEXT, ".bytes": TEXT, ".yaml": TEXT,
}

IMPORTED = {".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".wav", ".ogg", ".mp3", ".ttf", ".otf", ".fbx", ".mp4", ".webm", ".ogv"}

def skip(name):
    return name.startswith(".") or name.endswith("~") or name.endswith(".meta")

def ignored():
    repo = os.path.join(ROOT, "..")
    try:
        out = subprocess.run(["git", "ls-files", "--others", "--ignored", "--exclude-standard", "-z", "Assets"],
                             cwd=repo, capture_output=True, check=True).stdout.decode("utf-8")
    except (OSError, subprocess.CalledProcessError):
        return set()
    return {os.path.normpath(os.path.join(repo, p)) for p in out.split("\0") if p}

def main():
    check = "--check" in sys.argv
    missing, orphans = [], []
    hidden = ignored()
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if not skip(d)]
        if dirpath.endswith(".androidlib"):
            dirnames[:] = []
            continue
        for name in dirnames + filenames:
            if skip(name):
                continue
            path = os.path.join(dirpath, name)
            if not os.path.exists(path + ".meta"):
                missing.append(path)
            elif os.path.normpath(path) in hidden and os.path.normpath(path + ".meta") not in hidden:
                orphans.append(path + ".meta")
        for name in filenames:
            if name.endswith(".meta") and not os.path.exists(os.path.join(dirpath, name[:-5])):
                orphans.append(os.path.join(dirpath, name))
    for path in orphans:
        print("orphan meta:", os.path.relpath(path, os.path.join(ROOT, "..")))
    if check:
        for path in missing:
            print("missing meta:", os.path.relpath(path, os.path.join(ROOT, "..")))
        sys.exit(1 if missing or orphans else 0)
    for path in missing:
        if os.path.isdir(path):
            template = FOLDER
        elif os.path.splitext(path)[1].lower() in IMPORTED:
            print("left for Unity:", os.path.relpath(path, os.path.join(ROOT, "..")))
            continue
        else:
            template = BY_EXT.get(os.path.splitext(path)[1].lower(), DEFAULT)
        with open(path + ".meta", "w", newline="\n") as f:
            f.write(template.format(guid=uuid.uuid4().hex))
        print("created", os.path.relpath(path + ".meta", os.path.join(ROOT, "..")))

if __name__ == "__main__":
    main()
