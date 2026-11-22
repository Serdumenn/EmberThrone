import os, pathlib, shutil, sys
from fontTools.ttLib import TTFont

OUT = pathlib.Path(__file__).resolve().parents[2] / 'Assets' / 'EmberThrone' / 'Resources' / 'UI' / 'Fonts'
TURKISH = 'şŞğĞıİçÇöÖüÜ'


def main(src, license_file):
    path = OUT / 'PaytoneOne-Regular.ttf'
    shutil.copyfile(src, path)
    shutil.copyfile(license_file, OUT / 'OFL-PaytoneOne.txt')
    cmap = TTFont(path).getBestCmap()
    missing = [c for c in TURKISH if ord(c) not in cmap]
    print(f'{path.name}: {os.path.getsize(path)} bytes', 'MISSING ' + ''.join(missing) if missing else 'Turkish ok')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
