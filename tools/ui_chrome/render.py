import argparse, json, os, shutil, subprocess, sys, tempfile, pathlib

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent.parent
DEFAULT_OUT = REPO / 'Assets' / 'EmberThrone' / 'Resources' / 'UI' / 'Chrome'
BROWSERS = [r'C:\Program Files\Google\Chrome\Application\chrome.exe', r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
            '/usr/bin/google-chrome', '/usr/bin/chromium', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome']

SIZES = {
    'panel': (360, 360), 'panel_band': (400, 540), 'btn_green': (400, 200), 'btn_orange': (400, 200), 'btn_primary': (400, 200),
    'btn_close': (150, 160), 'pill': (300, 100), 'chip': (300, 70), 'badge': (84, 84), 'plus': (80, 80), 'check': (80, 80), 'tag': (260, 76),
    'ring': (220, 228), 'slot': (180, 188), 'slot_warm': (180, 188), 'nav': (200, 240), 'nav_tab_on': (300, 300),
    'trough': (300, 72), 'fill': (200, 61), 'header': (1080, 330), 'banner': (600, 120), 'row': (500, 165), 'row_me': (500, 165),
    'ribbon': (1040, 190), 'fx_glow': (512, 512), 'fx_rays': (1024, 1024), 'scrim_vignette': (512, 512),
}


def browser():
    for b in BROWSERS:
        if os.path.exists(b): return b
    sys.exit('Chrome or Edge not found')


def shot(exe, url, w, h, out, profile):
    subprocess.run([exe, '--headless=new', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000',
                    f'--user-data-dir={profile}', '--force-device-scale-factor=1', f'--window-size={w},{h}',
                    f'--screenshot={out}', url], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=True, timeout=60)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--theme', default='kor')
    ap.add_argument('--out', default=str(DEFAULT_OUT))
    ap.add_argument('names', nargs='*')
    a = ap.parse_args()
    exe = browser()
    os.makedirs(a.out, exist_ok=True)
    page = (HERE / 'chrome.html').as_uri()
    profile = tempfile.mkdtemp(prefix='ui_chrome_')
    try:
        for name in a.names or SIZES:
            w, h = SIZES[name]
            out = os.path.abspath(os.path.join(a.out, name + '.png'))
            shot(exe, f'{page}?c={name}&t={a.theme}', w, h, out, profile)
            print(f'{name:16s} {w}x{h}')
    finally:
        shutil.rmtree(profile, ignore_errors=True)


if __name__ == '__main__':
    main()
