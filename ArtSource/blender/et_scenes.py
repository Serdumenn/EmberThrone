"""EmberThrone environment scenes: the kingdom island (home map) and the castle view behind the board.
Load et_core.py and et_assets.py first. The island shows the theme: restored, warm districts and one
ash-grey district that the player has not rekindled yet.
"""
import bpy, bmesh, math, random
from mathutils import Vector

ASH_FROM, ASH_TO = 0.25, 1.85  # angle range (radians) of the unrestored district, upper right of the screen


def island_radius(t):
    return 5.0 + 0.55 * math.sin(3 * t + 1.0) + 0.35 * math.sin(5 * t + 2.0) + 0.22 * math.sin(7 * t + 0.4)


def island_xy(t, k=1.0):
    r = island_radius(t) * k
    return r * math.cos(t), r * math.sin(t) * 1.3


def inside(x, y, margin=0.85):
    t = math.atan2(y / 1.3, x)
    return math.hypot(x, y / 1.3) < island_radius(t) * margin


def is_ash(x, y):
    t = math.atan2(y, x)
    return ASH_FROM < t < ASH_TO


def mat_ground(name, a, b, ash_a, ash_b, scale=1.4):
    """Grass that turns to ash inside the unrestored district, with a noisy border."""
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; p = _p(m)
    tc = nt.nodes.new('ShaderNodeTexCoord')
    noise = nt.nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = scale; noise.inputs['Detail'].default_value = 4
    nt.links.new(tc.outputs['Object'], noise.inputs['Vector'])
    def ramp(c0, c1):
        r = nt.nodes.new('ShaderNodeValToRGB')
        r.color_ramp.elements[0].color = srgb(c0); r.color_ramp.elements[1].color = srgb(c1)
        r.color_ramp.elements[0].position = 0.3; r.color_ramp.elements[1].position = 0.7
        nt.links.new(noise.outputs['Fac'], r.inputs['Fac']); return r
    green, ash = ramp(a, b), ramp(ash_a, ash_b)
    sep = nt.nodes.new('ShaderNodeSeparateXYZ'); nt.links.new(tc.outputs['Object'], sep.inputs[0])
    at = nt.nodes.new('ShaderNodeMath'); at.operation = 'ARCTAN2'
    nt.links.new(sep.outputs['Y'], at.inputs[0]); nt.links.new(sep.outputs['X'], at.inputs[1])
    jit = nt.nodes.new('ShaderNodeMath'); jit.operation = 'MULTIPLY_ADD'; jit.inputs[1].default_value = 0.35; jit.inputs[2].default_value = -0.17
    nt.links.new(noise.outputs['Fac'], jit.inputs[0])
    ang = nt.nodes.new('ShaderNodeMath'); ang.operation = 'ADD'
    nt.links.new(at.outputs[0], ang.inputs[0]); nt.links.new(jit.outputs[0], ang.inputs[1])
    up = nt.nodes.new('ShaderNodeMapRange'); up.inputs['From Min'].default_value = ASH_FROM - 0.08; up.inputs['From Max'].default_value = ASH_FROM + 0.08
    dn = nt.nodes.new('ShaderNodeMapRange'); dn.inputs['From Min'].default_value = ASH_TO - 0.08; dn.inputs['From Max'].default_value = ASH_TO + 0.08
    dn.inputs['To Min'].default_value = 1.0; dn.inputs['To Max'].default_value = 0.0
    nt.links.new(ang.outputs[0], up.inputs['Value']); nt.links.new(ang.outputs[0], dn.inputs['Value'])
    fac = nt.nodes.new('ShaderNodeMath'); fac.operation = 'MULTIPLY'
    nt.links.new(up.outputs['Result'], fac.inputs[0]); nt.links.new(dn.outputs['Result'], fac.inputs[1])
    mix = nt.nodes.new('ShaderNodeMix'); mix.data_type = 'RGBA'
    nt.links.new(fac.outputs[0], mix.inputs['Factor']); nt.links.new(green.outputs['Color'], mix.inputs['A']); nt.links.new(ash.outputs['Color'], mix.inputs['B'])
    nt.links.new(mix.outputs['Result'], p.inputs['Base Color'])
    p.inputs['Roughness'].default_value = 0.75
    bump = nt.nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = 0.15
    nt.links.new(noise.outputs['Fac'], bump.inputs['Height']); nt.links.new(bump.outputs['Normal'], p.inputs['Normal'])
    return m


def mat_sea():
    m = bpy.data.materials.new('sea'); m.use_nodes = True; nt = m.node_tree; p = _p(m)
    tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ'); nt.links.new(tc.outputs['Object'], sep.inputs[0])
    sy = nt.nodes.new('ShaderNodeMath'); sy.operation = 'DIVIDE'; sy.inputs[1].default_value = 1.3
    nt.links.new(sep.outputs['Y'], sy.inputs[0])
    ln = nt.nodes.new('ShaderNodeCombineXYZ'); nt.links.new(sep.outputs['X'], ln.inputs['X']); nt.links.new(sy.outputs[0], ln.inputs['Y'])
    vl = nt.nodes.new('ShaderNodeVectorMath'); vl.operation = 'LENGTH'; nt.links.new(ln.outputs[0], vl.inputs[0])
    mr = nt.nodes.new('ShaderNodeMapRange'); mr.inputs['From Min'].default_value = 5.0; mr.inputs['From Max'].default_value = 9.0
    nt.links.new(vl.outputs['Value'], mr.inputs['Value'])
    rp = nt.nodes.new('ShaderNodeValToRGB'); cr = rp.color_ramp
    cr.elements[0].color = srgb('#46e2e6'); cr.elements[0].position = 0.0
    cr.elements[1].color = srgb('#155cc8'); cr.elements[1].position = 1.0
    e = cr.elements.new(0.25); e.color = srgb('#22a8e8')
    nt.links.new(mr.outputs['Result'], rp.inputs['Fac']); nt.links.new(rp.outputs['Color'], p.inputs['Base Color'])
    p.inputs['Roughness'].default_value = 0.12; p.inputs['Coat Weight'].default_value = 0.6
    wav = nt.nodes.new('ShaderNodeTexWave'); wav.inputs['Scale'].default_value = 1.6; wav.inputs['Distortion'].default_value = 8; wav.inputs['Detail'].default_value = 4
    bump = nt.nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = 0.08
    nt.links.new(tc.outputs['Object'], wav.inputs['Vector']); nt.links.new(wav.outputs['Fac'], bump.inputs['Height']); nt.links.new(bump.outputs['Normal'], p.inputs['Normal'])
    return m


def island_slab(name, k, z0, z1, mat_top, mat_side, bev=0.25, n=160):
    bm = bmesh.new()
    pts = [island_xy(2 * math.pi * i / n, k) for i in range(n)]
    bot = [bm.verts.new((x, y, z0)) for x, y in pts]
    f = bm.faces.new(bot)
    ext = bmesh.ops.extrude_face_region(bm, geom=[f])
    for v in [e for e in ext['geom'] if isinstance(e, bmesh.types.BMVert)]: v.co.z = z1
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    ob = mesh_obj(name, bm, None, smooth=False)
    ob.data.materials.append(mat_top); ob.data.materials.append(mat_side)
    for poly in ob.data.polygons:
        poly.material_index = 0 if poly.normal.z > 0.5 else 1
    bevel(ob, bev, 4)
    return ob


def house(x, y, rng, ash, mats, s=1.0):
    wall, roof, door, win, ash_wall, ash_roof = mats
    h = (0.42 + rng.random() * 0.22) * s
    w = (0.42 + rng.random() * 0.2) * s
    rot = rng.uniform(-30, 30)
    objs = [box((w, w * 0.85, h), loc=(0, 0, h / 2), mat=ash_wall if ash else wall, bev=0.03)]
    if ash:
        objs.append(box((w * 0.5, w * 0.9, 0.08), loc=(-w * 0.2, 0, h + 0.04), rot=(0, 18, 0), mat=ash_roof, bev=0.02))
    else:
        r = prism('roof', [(-w * 0.62, 0), (w * 0.62, 0), (0, w * 0.55)], w * 0.95, roof, y0=w * 0.475)
        r.location = (0, 0, h); bevel(r, 0.03, 2); objs.append(r)
        objs.append(box((w * 0.22, 0.04, h * 0.42), loc=(0, -w * 0.43, h * 0.21), mat=door, bev=0.02))
        objs.append(box((w * 0.18, 0.04, w * 0.18), loc=(w * 0.26, -w * 0.43, h * 0.62), mat=win, bev=0.02))
    return group(objs, loc=(x, y, 0.62), rot=(0, 0, rot))


def tree(x, y, rng, ash, mats, s=1.0):
    leaf, leaf2, trunk, dead = mats
    s *= 0.8 + rng.random() * 0.5
    objs = [cyl(0.06 * s, 0.4 * s, loc=(0, 0, 0.2 * s), mat=dead if ash else trunk, verts=12)]
    if ash:
        for a in (0, 120, 240):
            br = cyl(0.03 * s, 0.32 * s, loc=(0.08 * s * math.cos(math.radians(a)), 0.08 * s * math.sin(math.radians(a)), 0.48 * s), rot=(math.radians(35), 0, math.radians(a)), mat=dead, verts=8)
            objs.append(br)
    else:
        kind = rng.random()
        if kind < 0.35:
            for i, (rr, zz) in enumerate(((0.32, 0.45), (0.25, 0.7), (0.17, 0.92))):
                objs.append(cyl(rr * s, 0.36 * s, loc=(0, 0, zz * s), mat=leaf2, r2=0.0, verts=16))
        else:
            objs.append(sphere(0.34 * s, loc=(0, 0, 0.62 * s), mat=leaf if rng.random() < 0.6 else leaf2, seg=20))
            objs.append(sphere(0.22 * s, loc=(0.18 * s, -0.12 * s, 0.75 * s), mat=leaf, seg=16))
    return group(objs, loc=(x, y, 0.62))


def big_castle(mats):
    stone, roof, door, win, flag = mats
    R = 1.25
    objs = [cyl(2.0, 0.5, loc=(0, 0, 0.85), mat=stone, verts=64, bev=0.08)]
    for i in range(4):
        a = math.radians(45 + i * 90)
        x, y = R * math.cos(a), R * math.sin(a)
        objs.append(cyl(0.34, 1.6, loc=(x, y, 1.9), mat=stone, verts=24, bev=0.03))
        objs.append(cyl(0.46, 0.9, loc=(x, y, 3.15), mat=roof, r2=0.0, verts=24, bev=0.02))
        objs.append(box((0.14, 0.06, 0.24), loc=(x * 1.27, y * 1.27, 2.1), rot=(0, 0, math.degrees(a) + 90), mat=win, bev=0.05))
    for i in range(4):
        a = math.radians(i * 90)
        objs.append(box((0.22, 1.7, 0.9), loc=(R * 0.95 * math.cos(a), R * 0.95 * math.sin(a), 1.55), rot=(0, 0, i * 90), mat=stone, bev=0.03))
    objs.append(box((1.0, 1.0, 2.3), loc=(0, 0.1, 2.2), mat=stone, bev=0.04))
    objs.append(cyl(0.82, 1.3, loc=(0, 0.1, 4.0), rot=(0, 0, 45), mat=roof, r2=0.0, verts=4, bev=0.03))
    objs.append(box((0.34, 0.08, 0.5), loc=(0, -0.42, 1.35), mat=door, bev=0.12))
    for z in (2.4, 2.9):
        objs.append(box((0.16, 0.06, 0.26), loc=(0, -0.42, z), mat=win, bev=0.06))
    objs.append(cyl(0.025, 0.8, loc=(0, 0.1, 4.95), mat=mat_metal('pole', '#c9a26a')))
    f = prism('flag', [(0, 0), (0.6, -0.12), (0, -0.3)], 0.04, flag, y0=0.02)
    f.location = (0.02, 0.1, 5.32); objs.append(f)
    return group(objs, loc=(0, 0.2, 0))


def brazier(x, y, mats):
    stone, fire = mats
    return group([cyl(0.12, 0.3, loc=(0, 0, 0.15), mat=stone, verts=16, bev=0.03),
                  sphere(0.12, loc=(0, 0, 0.38), mat=fire, scale=(1, 1, 1.4), seg=16)], loc=(x, y, 0.62))


def cloud_bank(x, y, z, k, rng, mat):
    objs = []
    for i in range(9):
        objs.append(sphere(k * (0.5 + rng.random() * 0.6), loc=(k * rng.uniform(-1.6, 1.6), k * rng.uniform(-0.6, 0.6), k * rng.uniform(-0.15, 0.35)), mat=mat, scale=(1, 1, 0.65), seg=24))
    return group(objs, loc=(x, y, z))


def build_island(seed=7, keep_clear=None):
    rng = random.Random(seed)
    blocked = keep_clear or (lambda x, y: False)
    grass_top = mat_ground('grass', '#5fc63a', '#9ae85e', '#8d8b86', '#6c6a66')
    cliff = mat_glossy('cliff', '#c8945a', rough=0.7, coat=0.0)
    sand = mat_glossy('sand', '#f6dca4', rough=0.8, coat=0.0)
    sea_m = mat_sea()
    bpy.ops.mesh.primitive_plane_add(size=400, location=(0, 0, 0)); sea = bpy.context.active_object; sea.data.materials.append(sea_m)
    foam = mat_glossy('foam', '#ffffff', rough=0.6, coat=0, emis=0.4)
    island_slab('foam', 1.15, -0.3, 0.03, foam, foam, bev=0.1)
    island_slab('beach', 1.08, -0.3, 0.18, sand, sand, bev=0.2)
    island_slab('island', 1.0, -0.3, 0.62, grass_top, cliff, bev=0.18)

    stone = mat_glossy('stone', '#ead6b4', rough=0.5, coat=0.1)
    roof = mat_glossy('roof', '#f2541a', rough=0.18, coat=0.8)
    door = mat_glossy('door', '#6a3416', rough=0.5)
    win = mat_emit('window', '#ffd25a', 4)
    flag = mat_glossy('flag', '#ff8a1a', rough=0.3, emis=0.2)
    ash_wall = mat_glossy('ashwall', '#7a7672', rough=0.8, coat=0)
    ash_roof = mat_glossy('ashroof', '#4a4644', rough=0.8, coat=0)
    big_castle((stone, roof, door, win, flag))
    hm = (stone, roof, door, win, ash_wall, ash_roof)
    tm = (mat_glossy('leaf', '#3fb83a', rough=0.5, coat=0.3), mat_glossy('leaf2', '#2a9a3a', rough=0.5, coat=0.3),
          mat_glossy('trunk', '#8a5228', rough=0.6, coat=0), mat_glossy('dead', '#55504c', rough=0.8, coat=0))
    placed = []
    def free(x, y, d):
        return all(math.hypot(x - a, y - b) > d for a, b in placed)
    tries = 0
    while len(placed) < 16 and tries < 4000:
        tries += 1
        x, y = rng.uniform(-5, 5), rng.uniform(-6.5, 6.5)
        if not inside(x, y, 0.82) or math.hypot(x, y - 0.2) < 2.6 or not free(x, y, 1.1) or blocked(x, y): continue
        placed.append((x, y)); house(x, y, rng, is_ash(x, y), hm)
    trees = 0
    while trees < 60 and tries < 20000:
        tries += 1
        x, y = rng.uniform(-5.5, 5.5), rng.uniform(-7, 7)
        if not inside(x, y, 0.92) or math.hypot(x, y - 0.2) < 2.3 or not free(x, y, 0.5) or blocked(x, y): continue
        placed.append((x, y)); tree(x, y, rng, is_ash(x, y), tm); trees += 1
    fire = mat_emit('fire', '#ff9a2a', 12)
    for a in range(0, 360, 45):
        x, y = 2.35 * math.cos(math.radians(a)), 2.35 * math.sin(math.radians(a)) + 0.2
        if not is_ash(x, y) and not keep_clear: brazier(x, y, (stone, fire))
    cloud = mat_glossy('cloud', '#ffffff', rough=0.9, coat=0.0, emis=0.35, emis_color='#f4f8ff', sss=0.0)
    cloud_bank(4.8, 7.6, 1.6, 1.1, rng, cloud)
    cloud_bank(-5.6, 6.8, 1.3, 1.0, rng, cloud)
    cloud_bank(6.4, -2.0, 1.0, 0.9, rng, cloud)
    cloud_bank(-6.6, -6.2, 1.2, 1.0, rng, cloud)
    smoke = mat_glossy('smoke', '#8a8792', rough=0.9, coat=0.0, emis=0.15, emis_color='#9a98a2')
    cloud_bank(2.6, 5.2, 2.6, 0.75, rng, smoke)
    # sand paths from the castle gate
    path_m = mat_glossy('path', '#f0d49a', rough=0.85, coat=0)
    for ang in (-100, -40, -150, 200):
        a = math.radians(ang)
        pts = [(2.0 * math.cos(a), 2.0 * math.sin(a) + 0.2, 0.64), (3.2 * math.cos(a + 0.2), 3.2 * math.sin(a + 0.2) * 1.2, 0.64), (4.2 * math.cos(a + 0.05), 4.2 * math.sin(a + 0.05) * 1.25, 0.64)]
        t = tube(pts, 0.16, path_m); t.scale = (1, 1, 0.12)
    # shore rocks
    rock = mat_glossy('rock', '#9a8f86', rough=0.7, coat=0.1)
    for i in range(14):
        tt = rng.uniform(0, 2 * math.pi); x, y = island_xy(tt, 1.1)
        sphere(rng.uniform(0.12, 0.3), loc=(x, y, 0.1), mat=rock, scale=(1.2, 1, 0.7), seg=12)
    # pier and a little boat
    plank = mat_wood('pier', '#d8924e', '#9a5a2a', scale=0.8)
    tt = math.radians(-70); x, y = island_xy(tt, 1.02)
    box((0.5, 1.8, 0.08), loc=(x + 0.2, y - 0.9, 0.28), mat=plank, bev=0.02)
    for dy in (-0.2, -1.0, -1.7):
        cyl(0.05, 0.5, loc=(x + 0.0, y + dy, 0.05), mat=plank, verts=10); cyl(0.05, 0.5, loc=(x + 0.4, y + dy, 0.05), mat=plank, verts=10)
    hull = mat_glossy('hull', '#c8582a', rough=0.3)
    b = sphere(0.45, loc=(x + 1.1, y - 1.6, 0.05), mat=hull, scale=(0.55, 1.2, 0.35), seg=24)
    sail = mat_glossy('sail', '#fff6e4', rough=0.6, coat=0)
    s2 = prism('sail', [(0, 0), (0.0, 0.9), (0.55, 0.05)], 0.03, sail, y0=0.015); s2.location = (x + 1.1, y - 1.6, 0.2); s2.rotation_euler = (0, 0, math.radians(90))


def sky_world(top='#5ab6ff', strength=1.0, horizon=None):
    sc = bpy.context.scene
    world = bpy.data.worlds.get('ET_Sky') or bpy.data.worlds.new('ET_Sky')
    sc.world = world; world.use_nodes = True; nt = world.node_tree; nt.nodes.clear()
    bg = nt.nodes.new('ShaderNodeBackground'); bg.inputs['Color'].default_value = srgb(top); bg.inputs['Strength'].default_value = strength
    out = nt.nodes.new('ShaderNodeOutputWorld'); nt.links.new(bg.outputs['Background'], out.inputs['Surface'])
    if horizon:
        tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ')
        nt.links.new(tc.outputs['Generated'], sep.inputs[0])
        rp = nt.nodes.new('ShaderNodeValToRGB'); cr = rp.color_ramp
        cr.elements[0].position = 0.0; cr.elements[0].color = srgb(horizon)
        cr.elements[1].position = 0.55; cr.elements[1].color = srgb(top)
        nt.links.new(sep.outputs['Z'], rp.inputs['Fac']); nt.links.new(rp.outputs['Color'], bg.inputs['Color'])


def sun(rot=(42, 0, -38), strength=4.2, color='#fff1d8', angle=3.0):
    ld = bpy.data.lights.new('Sun', 'SUN'); ld.energy = strength; ld.color = srgb(color)[:3]; ld.angle = math.radians(angle)
    ob = link(bpy.data.objects.new('Sun', ld)); ob.rotation_euler = [math.radians(a) for a in rot]
    return ob


def persp_camera(loc, target, lens=35, w=1080, h=1920):
    cd = bpy.data.cameras.new('Cam'); cd.lens = lens
    cam = link(bpy.data.objects.new('Cam', cd)); cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.camera = cam
    sc = bpy.context.scene; sc.render.resolution_x = w; sc.render.resolution_y = h
    return cam


def render_island(name='bg_map', samples=96):
    reset_scene()
    sc = setup_render(1080, 1920, samples, transparent=False)
    sc.render.image_settings.file_format = 'JPEG'; sc.render.image_settings.quality = 90
    sky_world('#7cc6ff', 0.9)
    sun()
    build_island()
    persp_camera((0, -12.6, 14.2), (0, -0.2, 0.4), lens=31)
    return render(name)


def render_game_bg(name='bg_game', samples=64):
    """Close view of the castle from the meadow: sky and roofs above the board, grass and houses below."""
    reset_scene()
    sc = setup_render(1080, 1920, samples, transparent=False)
    sc.render.image_settings.file_format = 'JPEG'; sc.render.image_settings.quality = 90
    sky_world('#2f8cff', 1.0, horizon='#e6f6ff')
    sun(rot=(50, 0, -32), strength=4.2)
    build_island(keep_clear=lambda x, y: abs(x) < 1.2 and y < -2.6)
    rng = random.Random(3)
    cloud = mat_glossy('cloud2', '#ffffff', rough=0.9, coat=0.0, emis=0.4, emis_color='#f4f8ff')
    for (x, y, z, k) in ((-5.5, 9, 6.5, 1.6), (5.8, 10, 7.5, 1.8), (0.5, 14, 9.5, 2.0), (-2.5, 6, 2.2, 0.8), (4.2, 6, 2.0, 0.9)):
        cloud_bank(x, y, z, k, rng, cloud)
    cam = persp_camera((0.0, -10.5, 5.6), (0.0, 0.8, 2.0), lens=27)
    cam.data.dof.use_dof = True; cam.data.dof.focus_distance = 11.0; cam.data.dof.aperture_fstop = 2.0
    return render(name)
