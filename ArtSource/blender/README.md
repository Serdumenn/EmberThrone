# Blender render pipeline

EmberThrone's 3D art is built and lit by Python scripts. The scripts produce transparent PNG sprites and JPEG backgrounds; nothing is modelled by hand, so every asset can be re-rendered or recolored from text.

| File | What |
|---|---|
| `et_core.py` | Shared look: render settings (Cycles, Standard view, transparent film), studio HDRI, the sprite light rig (key, glint, warm/cool rims), materials (gem with inner glow, gold, wood, ice), geometry helpers (faceted `gem`, `prism`, `round_corners`). |
| `et_assets.py` | One builder per sprite (pieces, specials, obstacles, boosters, currency, menu icons, the ember spirit mascot), the `ASSETS` list and `render_assets()`. |
| `et_scenes.py` | The kingdom island (home map, with the grey unrestored "ash" district) and the castle view behind the board: `render_island()`, `render_game_bg()`. |

Output: `docs/design/style-frames/img/` by default (set `ET_OUT` before loading `et_core.py` to change it). Sprites are 512×512; backgrounds 1080×1920.

## Running

Blender 5.1, Scripting workspace → Python Console (or `blender --background --python-expr "…"`). Load the three files, then render:

```python
P = r"C:\Users\fatih\Documents\GitHub\EmberThrone\ArtSource\blender"
g = globals()
for f in ("et_core.py", "et_assets.py", "et_scenes.py"):
    exec(open(P + "\\" + f, encoding="utf-8").read(), g)
render_assets(['piece_R', 'piece_O'])   # or render_island(), render_game_bg()
```

Renders land in `docs/design/style-frames/img`. The game uses copies: board art (`piece_*`, `sp_*`, `ob_*`, `bg_game.jpg`) in `Assets/EmberThrone/Resources/Board/`, interface icons in `Assets/EmberThrone/Resources/UI/Art/`. Copy a file there again after re-rendering it.

Notes learned while building it:
- A render blocks Blender until it finishes; render a few sprites at a time.
- Load the three files again in every new Blender session.
- Metals go dark under the dim sprite HDRI. Use `mat_glossy(..., metallic≈0.75)` with a mid-grey base rather than pure metal.
- Inner glow on gems (`mat_gem` `inner`) washes colors out above about 0.4.
- Use the Standard view transform; AgX and Filmic flatten the saturated look.
- Board art sits on light cells: keep obstacles saturated and mid-dark (the first crates were too pale), and check every render on a light tile before copying it in.
- Don't open a render while Blender is still writing it.
