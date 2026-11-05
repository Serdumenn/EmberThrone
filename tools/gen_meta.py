#!/usr/bin/env python3
"""Creates missing Unity .meta files (with fresh GUIDs) for everything under Assets/.

Unity normally writes these when the editor imports a file. Because this project is
edited by AI agents without an editor, run this before every commit that adds files
under Assets/. Existing .meta files (and their GUIDs) are never touched.

Usage: python3 tools/gen_meta.py [--check]
  --check   only report missing/orphan .meta files, exit 1 if any
"""
import os
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

def skip(name):
    return name.startswith(".") or name.endswith("~") or name.endswith(".meta")

def main():
    check = "--check" in sys.argv
    missing, orphans = [], []
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if not skip(d)]
        for name in dirnames + filenames:
            if skip(name):
                continue
            path = os.path.join(dirpath, name)
            if not os.path.exists(path + ".meta"):
                missing.append(path)
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
        else:
            template = BY_EXT.get(os.path.splitext(path)[1].lower(), DEFAULT)
        with open(path + ".meta", "w", newline="\n") as f:
            f.write(template.format(guid=uuid.uuid4().hex))
        print("created", os.path.relpath(path + ".meta", os.path.join(ROOT, "..")))

if __name__ == "__main__":
    main()
