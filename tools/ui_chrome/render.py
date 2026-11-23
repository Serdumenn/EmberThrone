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
    'b2_green': (300, 150), 'b2_orange': (300, 150), 'b2_blue': (300, 150), 'b2_purple': (300, 150), 'b2_red': (300, 150), 'b2_grey': (300, 150),
    'b2_green_framed': (320, 176), 'b2_orange_framed': (320, 176), 'p2_ember': (320, 320), 'p2_frost': (320, 320), 'p2_plum': (320, 320), 'p2_cream': (320, 320),
    'band_ember': (600, 110), 'band_frost': (600, 110), 'band_plum': (600, 110), 'row2': (400, 150), 'row2_me': (400, 150), 'row2_rose': (400, 150),
    'corner_tab': (360, 110), 'price2': (260, 96), 'hdr2': (1080, 250), 'bg2': (512, 512), 'nav2': (200, 230), 'nav2_on': (260, 300), 'top2': (300, 100), 'pat_star': (90, 90),
    'p2_crimson': (320, 320), 'p2_royal': (320, 320), 'p2_gold': (320, 320), 'band_crimson': (600, 110), 'band_royal': (600, 110),
    'hdr3': (1080, 308), 'bg3': (512, 512), 'nav3': (200, 230), 'nav3_on': (260, 300), 'top3': (300, 100), 'row3_rose': (400, 150),
    'hud_plate': (520, 244), 'hud_tab': (240, 68), 'hud_card': (240, 182), 'hex_frame': (288, 327),
    'booster_green': (166, 166), 'count_red': (68, 68), 'count_green': (68, 68), 'btn_purple_round': (130, 130),
    'lvl_band': (600, 262), 'btn_close_gold': (158, 158), 'flag_center': (326, 278), 'flag_side': (185, 150),
    'wide_plum': (1080, 180), 'wide_cream': (1080, 420), 'gold_line': (64, 15), 'hex_red': (260, 270), 'heart_broken': (120, 108),
    'ic_gear_cream': (96, 96), 'bar_crimson': (400, 132), 'b2_purple_framed': (320, 176), 'ribbon_crimson': (200, 82), 'btn_orange_round': (88, 88),
    'side_crimson': (170, 170), 'side_royal': (170, 170), 'side_purple': (170, 170), 'pill_navy': (158, 55), 'pill_crimson': (158, 55),
    'toggle_on': (200, 200), 'toggle_slash': (200, 200), 'ic_sound': (128, 128), 'ic_vibrate': (128, 128), 'ic_shake': (128, 128),
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
