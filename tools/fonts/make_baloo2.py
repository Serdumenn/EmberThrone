import os, pathlib, sys
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset

OUT = pathlib.Path(__file__).resolve().parents[2] / 'Assets' / 'EmberThrone' / 'Resources' / 'UI' / 'Fonts'
RANGES = [(0x20, 0x7E), (0xA0, 0x24F), (0x2000, 0x206F), (0x20A0, 0x20CF), (0x2100, 0x214F), (0x2190, 0x21FF), (0x2212, 0x2212)]
TURKISH = 'şŞğĞıİçÇöÖüÜ'


def main(src):
    unicodes = [u for a, b in RANGES for u in range(a, b + 1)]
    for weight, name in ((700, 'Bold'), (800, 'ExtraBold')):
        font = TTFont(src)
        instantiateVariableFont(font, {'wght': weight}, inplace=True, updateFontNames=True)
        opts = subset.Options()
        opts.layout_features = ['kern', 'liga', 'calt', 'ccmp', 'locl', 'mark', 'mkmk', 'case', 'tnum', 'lnum']
        opts.name_IDs = ['*']
        opts.notdef_outline = True
        sub = subset.Subsetter(opts)
        sub.populate(unicodes=unicodes)
        sub.subset(font)
        path = OUT / f'Baloo2-{name}.ttf'
        font.save(path)
        cmap = TTFont(path).getBestCmap()
        missing = [c for c in TURKISH if ord(c) not in cmap]
        print(f'{path.name}: {os.path.getsize(path)} bytes', 'MISSING ' + ''.join(missing) if missing else 'Turkish ok')


if __name__ == '__main__':
    main(sys.argv[1])
