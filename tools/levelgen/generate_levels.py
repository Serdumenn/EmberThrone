import json
import os
import random
import subprocess
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
LEVELS = os.path.join(ROOT, "Assets", "EmberThrone", "Resources", "Levels")
PLAN = os.path.join(os.path.dirname(os.path.abspath(__file__)), "plan.json")
COLORS = "RGBYPO"
SIZES = [(8, 9), (8, 9), (8, 9), (7, 9), (9, 9), (8, 10), (9, 10)]
TARGETS = [0.92, 0.88, 0.85, 0.82, 0.62, 0.9, 0.84, 0.8, 0.77, 0.46]
RECIPES = [
    [["color"], ["color", "color"]],
    [["box"], ["box", "color"]],
    [["floor"], ["floor", "color"]],
    [["cover", "color"], ["cover"]],
    [["box", "cover"], ["collectible", "box"], ["floor", "cover"]],
    [["color"], ["collectible"]],
    [["box", "floor"], ["floor", "box"]],
    [["cover", "box"], ["cover", "floor"]],
    [["collectible", "color"], ["collectible", "cover"]],
    [["collectible", "box", "cover"], ["box", "floor", "cover"], ["floor", "cover", "color"]],
]
FROST_DISTRICTS = (7, 8)
NEW_KINDS = {3: "lamp", 4: "flower", 5: "statue"}
TUTORIALS = [["{0}"], ["{0}", "color"], ["{0}", "box"]]
BLOCKERS = {"lamp": "L", "flower": "F", "statue": "S"}
DENSE_FROM = 5
MAX_MOVES = 40
EXTRA_ROLLS = 6


def mirror(cells, w):
    return {(x, y) for x, y in cells} | {(w - 1 - x, y) for x, y in cells}


def holes_for(rng, w, h):
    kind = rng.choice(["full", "full", "corners", "notches", "center", "hourglass", "columns", "diamond"])
    cells = set()
    if kind == "corners":
        cells = {(0, 0), (0, h - 1)}
        if rng.random() < 0.5:
            cells |= {(1, h - 1), (0, h - 2)}
    elif kind == "notches":
        rows = rng.sample(range(2, h - 2), 2)
        cells = {(0, y) for y in rows}
    elif kind == "center":
        cx = w // 2 - 1
        cy = rng.randint(3, h - 4)
        cells = {(cx, cy)} if w % 2 == 0 else {(w // 2, cy), (w // 2, cy + 1)}
        if w % 2 == 0 and rng.random() < 0.5:
            cells |= {(cx, cy + 1)}
    elif kind == "hourglass":
        mid = h // 2
        cells = {(0, mid - 1), (0, mid), (0, mid + 1), (1, mid)}
    elif kind == "columns":
        c = rng.choice([1, 2])
        y0 = rng.randint(2, h - 5)
        cells = {(c, y) for y in range(y0, y0 + 2)}
    elif kind == "diamond":
        cells = {(0, 0), (1, 0), (0, 1), (0, h - 1), (1, h - 1), (0, h - 2)}
    return mirror(cells, w), kind


def stencil(rng, w, h, size, free, avoid_top=1, avoid_bottom=0):
    kinds = ["band", "frame", "blocks", "diamond", "columns", "checker", "rows"]
    for _ in range(40):
        kind = rng.choice(kinds)
        cells = set()
        if kind == "band":
            y0 = rng.randint(avoid_top + 1, h - 3 - avoid_bottom)
            rows = rng.choice([1, 2])
            x0 = rng.choice([0, 1])
            cells = {(x, y) for y in range(y0, y0 + rows) for x in range(x0, w - x0)}
        elif kind == "frame":
            x0, y0 = rng.randint(1, 2), rng.randint(avoid_top + 1, 4)
            x1, y1 = w - 1 - x0, min(h - 1 - avoid_bottom, y0 + rng.randint(3, 5))
            cells = {(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 1) if x in (x0, x1) or y in (y0, y1)}
        elif kind == "blocks":
            y0 = rng.randint(avoid_top + 1, h - 3 - avoid_bottom)
            x0 = rng.randint(0, max(0, w // 2 - 2))
            cells = mirror({(x0, y0), (x0 + 1, y0), (x0, y0 + 1), (x0 + 1, y0 + 1)}, w)
            if rng.random() < 0.5:
                y1 = min(h - 2 - avoid_bottom, y0 + 3)
                cells |= mirror({(x0, y1), (x0 + 1, y1), (x0, y1 + 1), (x0 + 1, y1 + 1)}, w)
        elif kind == "diamond":
            cx, cy = (w - 1) / 2.0, rng.randint(avoid_top + 3, h - 3 - avoid_bottom)
            r = rng.choice([2, 2.5, 3])
            cells = {(x, y) for x in range(w) for y in range(h) if abs(x - cx) + abs(y - cy) <= r}
        elif kind == "columns":
            c = rng.randint(0, w // 2 - 1)
            y0 = rng.randint(avoid_top + 1, 4)
            y1 = min(h - 1 - avoid_bottom, y0 + rng.randint(3, 5))
            cells = mirror({(c, y) for y in range(y0, y1 + 1)}, w)
        elif kind == "checker":
            y0 = rng.randint(avoid_top + 1, 4)
            y1 = min(h - 1 - avoid_bottom, y0 + 3)
            cells = {(x, y) for x in range(1, w - 1) for y in range(y0, y1 + 1) if (x + y) % 2 == 0}
            cells = mirror(cells, w)
        elif kind == "rows":
            ys = rng.sample(range(avoid_top + 1, h - 1 - avoid_bottom), 2)
            cells = {(x, y) for y in ys for x in range(1, w - 1)}
        cells = {c for c in cells if c in free}
        lo, hi = size
        if lo <= len(cells) <= hi:
            return cells
    return set()


def patches(rng, w, h, free, count, size, avoid_top=1):
    allowed = {c for c in free if avoid_top <= c[1] < h - 1 and 0 < c[0] < w - 1}
    pool = sorted(allowed)
    cells = set()
    for _ in range(count):
        if not pool:
            break
        blob = {rng.choice(pool)}
        for _ in range(size * 8):
            if len(blob) >= size:
                break
            x, y = rng.choice(sorted(blob))
            dx, dy = rng.choice([(1, 0), (-1, 0), (0, 1), (0, -1)])
            if (x + dx, y + dy) in allowed:
                blob.add((x + dx, y + dy))
        cells |= mirror(blob, w) & allowed
    return cells


def families(district):
    return [k for d, k in sorted(NEW_KINDS.items()) if d <= district]


def place_statues(rng, w, h, board, free, count):
    spots = [(x, y) for y in range(2, h - 1) for x in range(0, w - 1)
             if all(c in free for c in ((x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)))]
    rng.shuffle(spots)
    taken = set()
    placed = 0
    for x, y in spots:
        if placed >= count:
            break
        block = {(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)}
        twin = {(w - 1 - cx, cy) for cx, cy in block}
        both = block | twin if count - placed >= 2 and not (block & twin) else block
        if both & taken or not both <= free:
            continue
        for cx, cy in both:
            board[cy][cx] = "S"
        taken |= both
        placed += len(both) // 4
    free -= taken
    return placed


def place_kind(rng, w, h, board, free, kind, tier, hard, few):
    if kind == "statue":
        return place_statues(rng, w, h, board, free, 1 if few else rng.choice([1, 2, 2]) + (1 if hard else 0))
    if kind == "flower":
        cells = patches(rng, w, h, free, 1, 1 if few else rng.choice([1, 2]))
    else:
        cells = stencil(rng, w, h, (4, 8) if few else (6, 12 + tier), free, avoid_top=1, avoid_bottom=1)
    for x, y in cells:
        board[y][x] = BLOCKERS[kind]
        free.discard((x, y))
    return len(cells)


def densify(rng, w, h, board, free, kinds, ratio):
    existing = sum(1 for y in range(h) for x in range(w) if board[y][x] != "#")
    blocked = sum(1 for y in range(h) for x in range(w) if board[y][x] not in "#.$")
    want = int(existing * ratio) - blocked
    if want <= 0 or not kinds:
        return
    pool = sorted(c for c in free if c[1] >= 3)
    rng.shuffle(pool)
    pool.sort(key=lambda c: -c[1])
    for x, y in pool[:want]:
        board[y][x] = kinds[(x + y) % len(kinds)]
        free.discard((x, y))


def grid(w, h, fill="."):
    return [[fill] * w for _ in range(h)]


def rows(g):
    return ["".join(r) for r in g]


def make_level(rng, level_id, name, local, district, hard):
    w, h = rng.choice(SIZES)
    holes, shape = holes_for(rng, w, h)
    goals_kinds = list(rng.choice(RECIPES[local]))
    tutorial = NEW_KINDS.get(district) if local < len(TUTORIALS) else None
    if tutorial:
        goals_kinds = [k.format(tutorial) for k in TUTORIALS[local]]
    else:
        known = [k for k in families(district) if k not in goals_kinds]
        if known and local not in (0, 5) and rng.random() < 0.7:
            goals_kinds.append(rng.choice(known))
    goals_kinds = goals_kinds[:4]
    board = grid(w, h)
    for x, y in holes:
        board[y][x] = "#"
    free = {(x, y) for x in range(w) for y in range(h) if (x, y) not in holes}
    level = {"id": level_id, "name": name, "moves": 20, "colors": 5, "difficulty": "superhard" if local == 9 else ("hard" if hard else "normal"), "goals": []}
    tier = district - 3
    used_colors = set()
    lanes = set()

    if "collectible" in goals_kinds:
        bottom = h - 1
        for x in range(w):
            if (x, bottom) in holes or (x, 0) in holes:
                free.discard((x, 0))
        tops = sorted({x for x in range(w) if all((x, y) in free for y in range(h))})
        pick = rng.sample(tops[1:-1] if len(tops) > 4 else tops, 2)
        for x in pick:
            board[0][x] = "$"
            free.discard((x, 0))
        lanes = {(x, y) for x in pick for y in range(h)}
        total = rng.choice([1, 2, 2, 3]) + (1 if hard else 0)
        level["collectibles"] = {"total": total, "maxOnBoard": 2, "spawnChance": 0.15}
        level["goals"].append({"type": "collectible"})

    if "box" in goals_kinds:
        open_cells = free - lanes
        cells = stencil(rng, w, h, (6, 14 + tier), open_cells, avoid_top=1, avoid_bottom=1)
        if not cells:
            cells = stencil(rng, w, h, (4, 16), open_cells)
        weights = [max(1, 6 - tier), 3 + tier // 2, 1 + tier // 3]
        hp = rng.choices(["1", "2", "3"], weights=weights)[0]
        for x, y in cells:
            board[y][x] = hp
            free.discard((x, y))
        level["goals"].append({"type": "box"})

    for kind in ("lamp", "flower", "statue"):
        if kind not in goals_kinds:
            continue
        if place_kind(rng, w, h, board, free - lanes if lanes else free, kind, tier, hard, tutorial == kind and local == 0) > 0:
            level["goals"].append({"type": kind})
        for y in range(h):
            for x in range(w):
                if board[y][x] in "LFS":
                    free.discard((x, y))

    dense_kinds = [c for c, k in (("1", "box"), ("1", "box"), ("L", "lamp")) if k in goals_kinds]
    if district >= DENSE_FROM and local not in (0, 5) and dense_kinds:
        densify(rng, w, h, board, free - lanes, dense_kinds, min(0.5, 0.36 + 0.02 * (district - DENSE_FROM) + (0.05 if hard else 0)))
        for y in range(h):
            for x in range(w):
                if board[y][x] != "." and board[y][x] != "$":
                    free.discard((x, y))

    if "floor" in goals_kinds:
        cells = stencil(rng, w, h, (10, 22 + tier), free, avoid_top=1)
        floor = grid(w, h)
        hp = "2" if (hard or tier >= 4) and rng.random() < 0.5 else "1"
        for x, y in cells:
            floor[y][x] = hp
        level["floor"] = rows(floor)
        level["goals"].append({"type": "floor"})

    if "cover" in goals_kinds:
        taken = set()
        if "floor" in level:
            taken = {(x, y) for y, r in enumerate(level["floor"]) for x, c in enumerate(r) if c != "."}
        if district in FROST_DISTRICTS:
            cells = patches(rng, w, h, free - taken - lanes, rng.choice([1, 2, 2]), rng.choice([3, 4]) + (1 if hard else 0))
            frost = grid(w, h)
            for x, y in cells:
                frost[y][x] = "1"
            level["frost"] = rows(frost)
        else:
            cells = stencil(rng, w, h, (8, 16 + tier), free - taken, avoid_top=1)
            cover = grid(w, h)
            hp = "2" if (hard or tier >= 3) and rng.random() < 0.45 else "1"
            for x, y in cells:
                cover[y][x] = hp
            level["cover"] = rows(cover)
        level["goals"].append({"type": "cover"})

    for kind in goals_kinds:
        if kind != "color":
            continue
        color = rng.choice([c for c in COLORS[:5] if c not in used_colors])
        used_colors.add(color)
        count = rng.choice([25, 30, 35]) + 5 * (tier // 2)
        level["goals"].append({"type": "color", "color": color, "count": count})

    level["board"] = rows(board)
    ordered = {k: level[k] for k in ("id", "name", "moves", "colors", "difficulty", "goals", "board") if k in level}
    for k in ("floor", "cover", "frost", "collectibles"):
        if k in level:
            ordered[k] = level[k]
    return ordered, shape


def tune(path, target, runs):
    cmd = ["dotnet", "run", "--project", os.path.join(ROOT, "tools", "LevelSim"), "-c", "Release", "--no-build", "--",
           "--runs", str(runs), "--mistakes", "0.35", "--tune", str(target), "--write", path]
    out = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    lines = [l for l in out.stdout.splitlines() if "->" in l or "ERROR" in l or "INVALID" in l]
    return out.returncode, lines


if __name__ == "__main__":
    with open(PLAN, encoding="utf-8") as f:
        plan = json.load(f)
    only = set(int(a) for a in sys.argv[1:] if a.isdigit())
    skip_tune = "--no-tune" in sys.argv
    runs = 300
    for district in plan["districts"]:
        if not district.get("ready", False) and "--all" not in sys.argv:
            continue
        d = district["index"]
        for local, name in enumerate(district["names"]):
            level_id = d * 10 + local + 1
            if only and level_id not in only:
                continue
            reroll = plan.get("rerolls", {}).get(str(level_id), 0)
            hard = local in (4, 9)
            path = os.path.join(LEVELS, "level_%04d.json" % level_id)
            target = round(TARGETS[local] - 0.008 * d, 3)
            for extra in range(1 if skip_tune else EXTRA_ROLLS):
                rng = random.Random(plan["seed"] * 1000 + level_id + 7919 * (reroll + extra))
                level, shape = make_level(rng, level_id, name, local, d, hard)
                with open(path, "w", encoding="utf-8", newline="\n") as f:
                    f.write(json.dumps(level, indent=2, ensure_ascii=False) + "\n")
                if skip_tune:
                    break
                code, lines = tune(path, target, runs)
                with open(path, encoding="utf-8") as f:
                    if json.load(f)["moves"] <= MAX_MOVES:
                        break
            if skip_tune:
                print(level_id, name, shape, [g["type"] for g in level["goals"]])
                continue
            print(level_id, name, shape, [g["type"] for g in level["goals"]], "target", target, *lines)
