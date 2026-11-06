"""EmberThrone asset builders. Load et_core.py first, then this file, then call sprite(...) or render_all().

Each builder makes one object group centered on the origin, about 2 units across, facing the camera (-Y).
"""
import bpy, bmesh, math
from mathutils import Vector

# body color, inner glow color
PIECES = {
    'R': ('#e00a24', '#ff5a4a'),
    'O': ('#f25a00', '#ffa040'),
    'Y': ('#f5a800', '#ffe060'),
    'G': ('#08a832', '#5cf070'),
    'B': ('#1458f0', '#5aa8ff'),
    'P': ('#8a14e6', '#cf70ff'),
}


def piece_outline(key):
    if key == 'R': return ring(18, 1.02)
    if key == 'O':
        return [(x, z + 0.1) for x, z in round_corners([(0, 1.1), (1.04, -0.74), (-1.04, -0.74)], 0.3, 6)]
    if key == 'Y': return round_corners(star_pts(5, 1.12, 0.54), 0.1, 3)
    if key == 'G': return round_corners(ring(6, 1.06), 0.16, 4)
    if key == 'B': return round_corners([(0, 1.14), (0.88, 0), (0, -1.14), (-0.88, 0)], 0.16, 4)
    return round_corners(ring(4, 1.3, 45), 0.34, 5)


def build_piece(key):
    body, glow = PIECES[key]
    m = mat_gem('gem_' + key, body, glow, inner=0.35)
    if key == 'Y':
        return gem('piece_Y', piece_outline(key), m, table=0.2, mid=0.6, crown=0.5, back=0.5)
    if key == 'R':
        return gem('piece_R', piece_outline(key), m, table=0.5, mid=0.8, crown=0.6)
    return gem('piece_' + key, piece_outline(key), m, table=0.52, mid=0.8, crown=0.55)


def sprite(name, build, scale=2.6, tilt=10.0, size=512, samples=160, glass=False, rig=1.0, world=0.32):
    reset_scene()
    setup_render(size, size, samples, glass=glass)
    setup_world(strength=world)
    studio_rig(rig)
    ortho_camera(scale, tilt)
    build()
    return render(name)


# ---------------------------------------------------------------- helpers for primitives
def _active(mat=None, smooth=True, bev=None, seg=3):
    ob = bpy.context.active_object
    if mat: ob.data.materials.append(mat)
    if smooth: bpy.ops.object.shade_smooth()
    if bev: bevel(ob, bev, seg)
    return ob


def cyl(r, depth, loc=(0, 0, 0), rot=(0, 0, 0), mat=None, verts=48, bev=None, seg=3, r2=None):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=rot)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=depth, location=loc, rotation=rot)
    return _active(mat, True, bev, seg)


def box(size, loc=(0, 0, 0), rot=(0, 0, 0), mat=None, bev=0.06, seg=3, smooth=True):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.scale = size
    bpy.ops.object.transform_apply(scale=True)
    return _active(mat, smooth, bev, seg)


def sphere(r, loc=(0, 0, 0), mat=None, scale=(1, 1, 1), seg=48):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=seg // 2, radius=r, location=loc)
    ob = bpy.context.active_object; ob.scale = scale
    return _active(mat, True)


def torus(R, r, loc=(0, 0, 0), rot=(0, 0, 0), mat=None):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, location=loc, rotation=rot, major_segments=64, minor_segments=16)
    return _active(mat, True)


def tube(points, radius, mat=None, name='Tube'):
    cu = bpy.data.curves.new(name, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = radius; cu.bevel_resolution = 6
    cu.use_fill_caps = True
    sp = cu.splines.new('BEZIER'); sp.bezier_points.add(len(points) - 1)
    for bp, p in zip(sp.bezier_points, points):
        bp.co = p; bp.handle_left_type = bp.handle_right_type = 'AUTO'
    ob = link(bpy.data.objects.new(name, cu))
    if mat: cu.materials.append(mat)
    return ob


def group(objs, loc=(0, 0, 0), rot=(0, 0, 0), scale=1.0):
    e = link(bpy.data.objects.new('Grp', None))
    for o in objs: o.parent = e
    e.location = loc; e.rotation_euler = [math.radians(a) for a in rot]; e.scale = (scale, scale, scale)
    return e


# ---------------------------------------------------------------- specials
def build_rocket(vertical=False):
    R90 = math.radians(90)
    body = mat_glossy('rk_body', '#ffe4a0', rough=0.12, emis=0.06)
    red = mat_glossy('rk_red', '#e60e28', rough=0.1)
    fin = mat_glossy('rk_fin', '#ff7400', rough=0.12)
    glass = mat_glossy('rk_glass', '#4cb8ff', rough=0.03, emis=0.8, emis_color='#bdf0ff')
    gold = mat_gold('rk_gold')
    objs = [cyl(0.36, 1.2, rot=(0, R90, 0), mat=body, bev=0.06)]
    for sgn in (-1, 1):
        objs.append(cyl(0.36, 0.52, loc=(sgn * 0.85, 0, 0), rot=(0, sgn * R90, 0), mat=red, r2=0.05, bev=0.04))
        objs.append(torus(0.365, 0.055, loc=(sgn * 0.38, 0, 0), rot=(0, R90, 0), mat=red))
    for sz in (1, -1):
        f = prism('fin', [(-0.32, 0.28 * sz), (0.22, 0.28 * sz), (0.02, 0.74 * sz), (-0.34, 0.74 * sz)], 0.12, fin, y0=0.06)
        bevel(f, 0.04, 3); objs.append(f)
    objs.append(sphere(0.17, loc=(0, -0.3, 0), mat=glass, scale=(1, 0.5, 1)))
    objs.append(torus(0.18, 0.04, loc=(0, -0.3, 0), rot=(R90, 0, 0), mat=gold))
    return group(objs, rot=(0, 90 if vertical else 0, 0))


def mat_bomb():
    m = mat_glossy('bomb', '#1e2338', rough=0.14, coat=1.0)
    nt = m.node_tree; p = _p(m)
    tc = nt.nodes.new('ShaderNodeTexCoord')
    vor = nt.nodes.new('ShaderNodeTexVoronoi'); vor.feature = 'DISTANCE_TO_EDGE'; vor.inputs['Scale'].default_value = 2.4
    nt.links.new(tc.outputs['Object'], vor.inputs['Vector'])

    def ramp(w, mult):
        r = nt.nodes.new('ShaderNodeMapRange'); r.inputs['From Min'].default_value = 0.0; r.inputs['From Max'].default_value = w
        r.inputs['To Min'].default_value = mult; r.inputs['To Max'].default_value = 0.0
        nt.links.new(vor.outputs['Distance'], r.inputs['Value']); return r
    a = ramp(0.035, 9.0); b = ramp(0.16, 1.3)
    add = nt.nodes.new('ShaderNodeMath'); add.operation = 'ADD'
    nt.links.new(a.outputs['Result'], add.inputs[0]); nt.links.new(b.outputs['Result'], add.inputs[1])
    nt.links.new(add.outputs[0], p.inputs['Emission Strength'])
    p.inputs['Emission Color'].default_value = srgb('#ff8a10')
    return m


def build_bomb():
    objs = [sphere(0.8, loc=(0, 0, -0.12), mat=mat_bomb(), seg=64)]
    steel = mat_metal('steel', '#c9d2e4', rough=0.22)
    objs.append(cyl(0.21, 0.22, loc=(0.12, 0, 0.7), rot=(0, math.radians(14), 0), mat=steel, bev=0.05))
    objs.append(tube([(0.15, 0, 0.78), (0.32, 0, 1.0), (0.55, 0, 1.0)], 0.05, mat_glossy('fuse', '#7a4a22', rough=0.5, coat=0.0)))
    objs.append(sphere(0.09, loc=(0.58, -0.02, 1.0), mat=mat_emit('spark', '#fff2a0', 18)))
    return group(objs)


def mat_orb():
    m = mat_glossy('orb', '#ffffff', rough=0.08, coat=1.0)
    nt = m.node_tree; p = _p(m)
    tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ'); comb = nt.nodes.new('ShaderNodeCombineXYZ')
    nt.links.new(tc.outputs['Object'], sep.inputs[0])
    nt.links.new(sep.outputs['X'], comb.inputs['X']); nt.links.new(sep.outputs['Z'], comb.inputs['Y']); nt.links.new(sep.outputs['Y'], comb.inputs['Z'])
    noise = nt.nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 1.6
    vmix = nt.nodes.new('ShaderNodeVectorMath'); vmix.operation = 'MULTIPLY_ADD'
    vmix.inputs[1].default_value = (0.5, 0.5, 0.5)
    nt.links.new(noise.outputs['Color'], vmix.inputs[0]); nt.links.new(comb.outputs[0], vmix.inputs[2])
    grad = nt.nodes.new('ShaderNodeTexGradient'); grad.gradient_type = 'RADIAL'
    nt.links.new(vmix.outputs[0], grad.inputs['Vector'])
    rp = nt.nodes.new('ShaderNodeValToRGB'); cr = rp.color_ramp
    cols = ['#ff1e3c', '#ff7a00', '#ffd000', '#18c840', '#1c78ff', '#a020f0', '#ff1e3c']
    cr.elements[0].position = 0.0; cr.elements[0].color = srgb(cols[0])
    cr.elements[1].position = 1.0; cr.elements[1].color = srgb(cols[-1])
    for i, c in enumerate(cols[1:-1], 1):
        e = cr.elements.new(i / 6); e.color = srgb(c)
    nt.links.new(grad.outputs['Fac'], rp.inputs['Fac'])
    lw = nt.nodes.new('ShaderNodeLayerWeight'); lw.inputs['Blend'].default_value = 0.5
    inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
    pw = nt.nodes.new('ShaderNodeMath'); pw.operation = 'POWER'; pw.inputs[1].default_value = 40.0
    nt.links.new(lw.outputs['Facing'], inv.inputs[1]); nt.links.new(inv.outputs[0], pw.inputs[0])
    mixc = nt.nodes.new('ShaderNodeMix'); mixc.data_type = 'RGBA'; mixc.inputs['B'].default_value = (1, 1, 1, 1)
    nt.links.new(pw.outputs[0], mixc.inputs['Factor']); nt.links.new(rp.outputs['Color'], mixc.inputs['A'])
    nt.links.new(rp.outputs['Color'], p.inputs['Base Color']); nt.links.new(mixc.outputs['Result'], p.inputs['Emission Color'])
    st = nt.nodes.new('ShaderNodeMath'); st.operation = 'MULTIPLY_ADD'; st.inputs[1].default_value = 2.5; st.inputs[2].default_value = 0.75
    nt.links.new(pw.outputs[0], st.inputs[0]); nt.links.new(st.outputs[0], p.inputs['Emission Strength'])
    return m


def build_orb():
    return sphere(0.92, mat=mat_orb(), seg=64)


# ---------------------------------------------------------------- obstacles
def build_crate(level=1):
    # level 3 is charred: darker planks, cross straps and a padlock
    # darker than the first kit: crates must stand out on the light board cells
    wood = mat_wood('plank', '#d6802e', '#9a4c18', scale=0.8) if level < 3 else mat_wood('plank3', '#a8602a', '#5e2c10', scale=0.8)
    dark = mat_wood('frame', '#a85a22', '#6a3010', scale=0.9)
    steel = mat_metal('steel', '#d4dbe8', rough=0.25)
    rivet = mat_metal('rivet', '#8a93a8', rough=0.3)
    objs = []
    for i, z in enumerate((0.6, 0.0, -0.6)):
        objs.append(box((1.86, 0.5, 0.56), loc=(0, 0.02 * i, z), mat=wood, bev=0.05))
    for z in (0.86, -0.86):
        objs.append(box((1.98, 0.6, 0.26), loc=(0, -0.08, z), mat=dark, bev=0.06))
    for x in (0.86, -0.86):
        objs.append(box((0.26, 0.6, 1.98), loc=(x, -0.08, 0), mat=dark, bev=0.06))
    objs.append(box((0.26, 0.2, 2.1), loc=(0, -0.3, 0), rot=(0, math.radians(45), 0), mat=dark, bev=0.05))
    for sx in (-1, 1):
        for sz in (-1, 1):
            objs.append(box((0.42, 0.12, 0.14), loc=(sx * 0.8, -0.42, sz * 0.93), mat=steel, bev=0.03))
            objs.append(box((0.14, 0.12, 0.42), loc=(sx * 0.93, -0.42, sz * 0.8), mat=steel, bev=0.03))
            objs.append(sphere(0.05, loc=(sx * 0.9, -0.5, sz * 0.9), mat=rivet, seg=16))
    if level >= 2:
        for x in (-0.42, 0.42):
            objs.append(box((0.22, 0.1, 2.02), loc=(x, -0.46, 0), mat=steel, bev=0.03))
            for z in (-0.6, 0, 0.6):
                objs.append(sphere(0.045, loc=(x, -0.53, z), mat=rivet, seg=16))
    if level >= 3:
        for z in (-0.3, 0.3):
            objs.append(box((2.02, 0.1, 0.2), loc=(0, -0.5, z), mat=steel, bev=0.03))
        objs.append(torus(0.17, 0.055, loc=(0, -0.66, 0.2), rot=(math.radians(90), 0, 0), mat=steel))
        objs.append(box((0.56, 0.22, 0.46), loc=(0, -0.68, -0.06), mat=mat_gold('lock'), bev=0.07))
        objs.append(sphere(0.07, loc=(0, -0.8, -0.08), mat=mat_glossy('keyhole', '#2a1206', rough=0.4), scale=(1, 0.5, 1.3), seg=16))
    return group(objs)


def mat_ash(name, light, dark, embers=1.0, crack_scale=2.2):
    """Soft grey ash with soot noise and ember-lit cracks (the floor layer: cleared by matches on top)."""
    m = mat_glossy(name, light, rough=0.8, coat=0.0)
    nt = m.node_tree; p = _p(m)
    tc = nt.nodes.new('ShaderNodeTexCoord')
    noise = nt.nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 3.2; noise.inputs['Detail'].default_value = 8.0
    nt.links.new(tc.outputs['Object'], noise.inputs['Vector'])
    rp = nt.nodes.new('ShaderNodeValToRGB'); cr = rp.color_ramp
    cr.elements[0].position = 0.35; cr.elements[0].color = srgb(dark)
    cr.elements[1].position = 0.72; cr.elements[1].color = srgb(light)
    nt.links.new(noise.outputs['Fac'], rp.inputs['Fac']); nt.links.new(rp.outputs['Color'], p.inputs['Base Color'])
    bump = nt.nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = 0.4
    nt.links.new(noise.outputs['Fac'], bump.inputs['Height']); nt.links.new(bump.outputs['Normal'], p.inputs['Normal'])
    vor = nt.nodes.new('ShaderNodeTexVoronoi'); vor.feature = 'DISTANCE_TO_EDGE'; vor.inputs['Scale'].default_value = crack_scale
    nt.links.new(tc.outputs['Object'], vor.inputs['Vector'])
    mr = nt.nodes.new('ShaderNodeMapRange'); mr.inputs['From Max'].default_value = 0.03
    mr.inputs['To Min'].default_value = 6.0 * embers; mr.inputs['To Max'].default_value = 0.0
    nt.links.new(vor.outputs['Distance'], mr.inputs['Value']); nt.links.new(mr.outputs['Result'], p.inputs['Emission Strength'])
    p.inputs['Emission Color'].default_value = srgb('#ff7a18')
    return m


def build_floor(level=1):
    """A slab of ash lying on the cell; level 2 is darker and more cracked."""
    if level == 1: ash = mat_ash('ash1', '#7e757f', '#4f4753', embers=0.4)
    else: ash = mat_ash('ash2', '#5c5262', '#2e2734', embers=0.7, crack_scale=3.0)
    return box((1.9, 0.26, 1.9), mat=ash, bev=0.16, seg=4)


def build_lantern():
    """The collectible: a glowing ember lantern, brought down to the bottom of the board."""
    gold = mat_gold('lantern_gold')
    glass = mat_glossy('lantern_glass', '#ffd27a', rough=0.05, emis=2.6, emis_color='#ffa424', sss=0.3)
    objs = [cyl(0.5, 0.95, loc=(0, 0, -0.05), rot=(0, 0, math.radians(22.5)), mat=glass, verts=8, bev=0.03)]
    for i in range(8):
        a = math.radians(i * 45)
        objs.append(cyl(0.05, 0.98, loc=(0.5 * math.cos(a), 0.5 * math.sin(a), -0.05), mat=gold, verts=12))
    objs.append(cyl(0.64, 0.16, loc=(0, 0, 0.5), rot=(0, 0, math.radians(22.5)), mat=gold, verts=8, bev=0.04))
    objs.append(cyl(0.6, 0.42, loc=(0, 0, 0.78), rot=(0, 0, math.radians(22.5)), mat=gold, verts=8, r2=0.12, bev=0.04))
    objs.append(torus(0.2, 0.055, loc=(0, 0, 1.12), rot=(math.radians(90), 0, 0), mat=gold))
    objs.append(cyl(0.62, 0.18, loc=(0, 0, -0.6), rot=(0, 0, math.radians(22.5)), mat=gold, verts=8, bev=0.04))
    return group(objs, loc=(0, 0, -0.2), scale=0.9)


def build_homing():
    """The 2x2 special: a little ember bird that flies to its target."""
    body = mat_glossy('bird', '#ff6414', rough=0.14, emis=0.25, emis_color='#ff9a30', sss=0.2)
    belly = mat_glossy('belly', '#ffe08a', rough=0.18)
    wing = mat_glossy('wing', '#ffc21a', rough=0.12, emis=0.15, emis_color='#ffd060')
    tail = mat_glossy('tail', '#ff3d1a', rough=0.12, emis=0.3, emis_color='#ff6a20')
    beak = mat_glossy('beak', '#ffb000', rough=0.2)
    eye = mat_glossy('eye', '#1a0a04', rough=0.05)
    objs = [sphere(0.62, loc=(-0.05, 0, -0.12), mat=body, scale=(1.15, 0.85, 0.9))]
    objs.append(sphere(0.42, loc=(0.1, -0.22, -0.32), mat=belly, scale=(1.0, 0.6, 0.7)))
    objs.append(sphere(0.42, loc=(0.55, 0, 0.32), mat=body))
    objs.append(cyl(0.13, 0.36, loc=(0.98, -0.02, 0.28), rot=(0, math.radians(90), 0), mat=beak, r2=0.0))
    objs.append(sphere(0.1, loc=(0.7, -0.33, 0.42), mat=eye))
    objs.append(sphere(0.035, loc=(0.73, -0.42, 0.46), mat=mat_emit('glint', '#ffffff', 6), seg=12))
    for x, z, r in ((0.4, 0.78, 0.11), (0.55, 0.84, 0.12), (0.7, 0.76, 0.1)):
        objs.append(sphere(r, loc=(x, 0, z), mat=tail, scale=(0.8, 0.8, 1.5)))
    w = prism('wing', round_corners([(-0.55, 0.0), (0.25, 0.05), (0.05, 0.55), (-0.45, 1.05), (-0.95, 0.85), (-0.9, 0.35)], 0.14, 4), 0.16, wing, y0=-0.42)
    bevel(w, 0.06, 3); objs.append(w)
    for ang, ln in ((25, 0.9), (0, 1.0), (-25, 0.85)):
        t = prism('tail', round_corners([(0, 0.13), (-ln, 0.24), (-ln - 0.15, 0.0), (-ln, -0.24), (0, -0.13)], 0.1, 3), 0.14, tail, y0=0.05)
        bevel(t, 0.05, 3); t.location = (-0.55, 0, -0.1); t.rotation_euler = (0, math.radians(ang), 0); objs.append(t)
    return group(objs, loc=(0.18, 0, 0.02), rot=(0, -10, 0), scale=0.84)


def build_ice():
    return box((1.96, 0.7, 1.96), mat=mat_ice(), bev=0.24, seg=6)


# ---------------------------------------------------------------- boosters
def build_hammer():
    wood = mat_glossy('handle', '#a8582a', rough=0.35, coat=0.6)
    steel = mat_glossy('steel', '#9eaac2', rough=0.24, metallic=0.75, coat=1.0)
    ember = mat_glossy('band', '#ff6a0a', rough=0.1, emis=0.9, emis_color='#ffb040')
    objs = [cyl(0.13, 1.45, loc=(0, 0, -0.32), mat=wood, bev=0.04)]
    objs.append(sphere(0.17, loc=(0, 0, -1.08), mat=ember))
    objs.append(box((1.36, 0.58, 0.58), loc=(0, 0, 0.55), mat=steel, bev=0.13, seg=4))
    for sx in (-1, 1):
        objs.append(cyl(0.3, 0.12, loc=(sx * 0.7, 0, 0.55), rot=(0, math.radians(90), 0), mat=steel, bev=0.04))
    objs.append(box((0.34, 0.64, 0.64), loc=(0, 0, 0.55), mat=ember, bev=0.09))
    return group(objs, rot=(0, -32, 0), loc=(0, 0, 0.12))


def arrow_shape():
    return round_corners([(-1.08, 0), (-0.42, 0.6), (-0.42, 0.24), (0.42, 0.24), (0.42, 0.6), (1.08, 0),
                          (0.42, -0.6), (0.42, -0.24), (-0.42, -0.24), (-0.42, -0.6)], 0.09, 3)


def build_arrows(vertical=False):
    m = mat_gem('arrow', '#ff6a00', '#ff9a30', inner=0.2)
    a = prism('arrow', arrow_shape(), 0.42, m, y0=0.21)
    bevel(a, 0.13, 5)
    smooth_by_angle(a, 40)
    return group([a], rot=(0, 90 if vertical else 0, 0))


def build_shuffle():
    objs = []
    for k, (col, glow) in enumerate((('#22b83a', '#8cff7a'), ('#1f6dff', '#8fd0ff'))):
        m = mat_gem('sh%d' % k, col, glow, inner=0.3)
        pts = []
        for i in range(9):
            a = math.radians(205 + i * 15 + k * 180)
            pts.append((0.62 * math.cos(a), 0, 0.62 * math.sin(a)))
        objs.append(tube(pts, 0.15, m))
        a_end = math.radians(205 + 8 * 15 + k * 180)
        tip = Vector((0.62 * math.cos(a_end), 0, 0.62 * math.sin(a_end)))
        tang = Vector((-math.sin(a_end), 0, math.cos(a_end)))
        cone = cyl(0.3, 0.42, loc=tip + tang * 0.12, mat=m, r2=0.02, bev=0.05)
        cone.rotation_euler = tang.to_track_quat('Z', 'Y').to_euler()
        objs.append(cone)
    return group(objs, rot=(0, 0, 0))


def build_gear():
    steel = mat_glossy('steel', '#9eaac2', rough=0.24, metallic=0.75, coat=1.0)
    dark = mat_glossy('hole', '#4a5068', rough=0.3)
    objs = [cyl(0.66, 0.36, rot=(math.radians(90), 0, 0), mat=steel, bev=0.06, verts=64)]
    for i in range(8):
        a = math.radians(i * 45)
        objs.append(box((0.42, 0.36, 0.36), loc=(0.74 * math.cos(a), 0, 0.74 * math.sin(a)), rot=(0, -i * 45, 0), mat=steel, bev=0.06))
    objs.append(cyl(0.24, 0.4, loc=(0, -0.02, 0), rot=(math.radians(90), 0, 0), mat=dark, bev=0.05))
    return group(objs)


# ---------------------------------------------------------------- currency and rewards
def flame_outline(k=1.0):
    pts = [(0, 0.66), (0.1, 0.44), (0.17, 0.53), (0.3, 0.32), (0.37, 0.06), (0.31, -0.2), (0.17, -0.34), (0, -0.38),
           (-0.17, -0.34), (-0.31, -0.2), (-0.37, 0.04), (-0.31, 0.26), (-0.21, 0.4), (-0.15, 0.24), (-0.06, 0.46)]
    return [(x * k, z * k) for x, z in round_corners(pts, 0.05, 3)]


def build_coin():
    gold = mat_gold('gold'); deep = mat_gold('gold_deep', deep=True)
    objs = [cyl(0.96, 0.3, rot=(math.radians(90), 0, 0), mat=gold, bev=0.11, seg=5, verts=72)]
    objs.append(torus(0.74, 0.055, loc=(0, -0.16, 0), rot=(math.radians(90), 0, 0), mat=gold))
    f = prism('flame', flame_outline(1.05), 0.1, deep, y0=-0.13)
    bevel(f, 0.045, 4); smooth_by_angle(f, 50)
    f.location = (0, 0, -0.02)
    objs.append(f)
    return group(objs)


def heart_outline(s=0.06):
    pts = []
    for k in range(64):
        t = k / 64 * 2 * math.pi
        x = 16 * math.sin(t) ** 3
        z = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x * s, z * s + 0.1))
    return pts


def build_heart():
    m = mat_gem('heart', '#e00c28', '#ff5a5a', inner=0.22, rough=0.08)
    h = prism('heart', heart_outline(), 0.62, m, y0=0.31)
    bevel(h, 0.3, 8)
    smooth_by_angle(h, 80)
    return h


def build_star(empty=False):
    if empty:
        m = mat_glossy('star_e', '#4a3466', rough=0.25, coat=0.6)
    else:
        m = mat_gem('star_gold', '#f5a000', '#ffe070', inner=0.3)
    return gem('star', round_corners(star_pts(5, 1.12, 0.56), 0.14, 3), m, table=0.22, mid=0.62, crown=0.55, back=0.4, bev=0.05, dome=0.05)


def build_chest():
    wood = mat_wood('chest', '#8e3614', '#4c1a08', scale=0.45, rough=0.55)
    gold = mat_gold('gold'); dark = mat_glossy('keyhole', '#2a1408', rough=0.4)
    R90 = math.radians(90)
    objs = [box((1.9, 1.15, 0.95), loc=(0, 0, -0.36), mat=wood, bev=0.07)]
    lid = cyl(0.58, 1.9, loc=(0, 0, 0.11), rot=(0, R90, 0), mat=wood, bev=0.06, verts=48)
    lid.scale = (1, 1, 0.82); objs.append(lid)
    for x in (-0.66, 0.66):
        objs.append(box((0.2, 1.2, 1.0), loc=(x, 0, -0.36), mat=gold, bev=0.04))
        objs.append(cyl(0.61, 0.2, loc=(x, 0, 0.11), rot=(0, R90, 0), mat=gold, bev=0.04, verts=48))
    objs[-1].scale = (1, 1, 0.84); objs[-3].scale = (1, 1, 0.84)
    objs.append(box((1.98, 1.22, 0.12), loc=(0, 0, 0.12), mat=gold, bev=0.04))
    objs.append(box((1.98, 1.22, 0.12), loc=(0, 0, -0.82), mat=gold, bev=0.04))
    objs.append(box((0.42, 0.14, 0.5), loc=(0, -0.6, -0.08), mat=gold, bev=0.06))
    objs.append(cyl(0.06, 0.1, loc=(0, -0.68, -0.04), rot=(R90, 0, 0), mat=dark))
    objs.append(box((0.05, 0.1, 0.14), loc=(0, -0.68, -0.14), mat=dark, bev=0.01))
    # a glow of coins escaping the lid seam
    objs.append(box((1.7, 1.0, 0.04), loc=(0, 0, 0.18), mat=mat_emit('seam', '#ffd060', 6), bev=0.01))
    return group(objs, rot=(0, 0, 24))


def build_trophy():
    gold = mat_gold('gold'); deep = mat_gold('gold_deep', deep=True)
    prof = [(0.0, -1.02), (0.58, -1.02), (0.6, -0.86), (0.36, -0.8), (0.14, -0.6), (0.12, -0.3), (0.2, -0.18),
            (0.5, -0.02), (0.68, 0.32), (0.74, 0.78), (0.82, 0.86), (0.76, 0.92), (0.0, 0.92)]
    bm = bmesh.new(); n = 48; rings = []
    for r, z in prof:
        rings.append([bm.verts.new((r * math.cos(2 * math.pi * k / n), r * math.sin(2 * math.pi * k / n), z)) for k in range(n)])
    for a, b in zip(rings, rings[1:]):
        for k in range(n):
            bm.faces.new((a[k], a[(k + 1) % n], b[(k + 1) % n], b[k]))
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-4)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    cup = mesh_obj('cup', bm, gold, smooth=True)
    objs = [cup]
    for sx in (-1, 1):
        objs.append(torus(0.3, 0.075, loc=(sx * 0.78, 0, 0.42), rot=(math.radians(90), 0, 0), mat=gold))
    st = gem('emblem', round_corners(star_pts(5, 0.3, 0.15), 0.03, 2), mat_gem('emb', '#e8102a', '#ff7070', inner=0.4), table=0.25, mid=0.6, crown=0.15, band=0.04, back=0.05, bev=0.012, dome=0.02)
    st.location = (0, -0.66, 0.42); objs.append(st)
    objs.append(box((1.3, 0.8, 0.2), loc=(0, 0, -1.1), mat=mat_wood('base', '#8a4a22', '#5a2a10', scale=0.8), bev=0.05))
    return group(objs, loc=(0, 0, 0.05), scale=0.95)


def build_castle_icon():
    stone = mat_glossy('stone', '#c49a68', rough=0.5, coat=0.1)
    roof = mat_glossy('roof', '#f24a10', rough=0.14, emis=0.05)
    door = mat_glossy('door', '#5a2a12', rough=0.4)
    glow = mat_emit('window', '#ffd25a', 3)
    R90 = math.radians(90)
    objs = [box((1.5, 0.7, 0.8), loc=(0, 0, -0.55), mat=stone, bev=0.04)]
    for x in (-0.64, 0.64):
        objs.append(cyl(0.28, 1.25, loc=(x, 0, -0.3), mat=stone, bev=0.03))
        objs.append(cyl(0.38, 0.6, loc=(x, 0, 0.6), mat=roof, r2=0.02, bev=0.03))
        objs.append(box((0.12, 0.06, 0.2), loc=(x, -0.29, 0.0), mat=glow, bev=0.04))
    objs.append(box((0.62, 0.6, 1.25), loc=(0, 0.1, -0.12), mat=stone, bev=0.04))
    objs.append(cyl(0.46, 0.72, loc=(0, 0.1, 0.84), rot=(0, 0, math.radians(45)), mat=roof, r2=0.02, verts=4, bev=0.03))
    objs.append(box((0.3, 0.1, 0.42), loc=(0, -0.36, -0.68), mat=door, bev=0.1))
    objs.append(box((0.14, 0.06, 0.22), loc=(0, -0.22, 0.2), mat=glow, bev=0.05))
    for x in (-0.5, -0.25, 0.25, 0.5):
        objs.append(box((0.14, 0.7, 0.16), loc=(x * 1.0, 0, -0.08), mat=stone, bev=0.02))
    flag = prism('flag', [(0, 0), (0.42, -0.1), (0, -0.24)], 0.04, roof, y0=0.02)
    flag.location = (0.02, 0.1, 1.42); objs.append(flag)
    objs.append(cyl(0.025, 0.4, loc=(0, 0.1, 1.3), mat=mat_metal('pole', '#c9a26a')))
    return group(objs, rot=(0, 0, 18), loc=(0, 0, -0.08))


def build_scroll():
    paper = mat_glossy('paper', '#fff2d2', rough=0.55, coat=0.1)
    wood = mat_wood('rod', '#c8783a', '#8e4a1c', scale=0.8)
    ink = mat_glossy('ink', '#b07a52', rough=0.6, coat=0)
    red = mat_gem('seal', '#d8102a', '#ff6060', inner=0.3)
    R90 = math.radians(90)
    objs = [box((1.2, 0.08, 1.45), mat=paper, bev=0.03)]
    for z in (0.78, -0.78):
        objs.append(cyl(0.17, 1.3, loc=(0, 0, z), rot=(0, R90, 0), mat=paper, bev=0.03))
        for x in (-0.72, 0.72):
            objs.append(sphere(0.13, loc=(x, 0, z), mat=wood))
    for i, z in enumerate((0.38, 0.14, -0.1, -0.34)):
        objs.append(box((0.8 - 0.12 * (i % 2), 0.02, 0.06), loc=(0, -0.05, z), mat=ink, bev=0.02))
    objs.append(cyl(0.2, 0.08, loc=(0.32, -0.08, -0.52), rot=(R90, 0, 0), mat=red, bev=0.03))
    return group(objs, rot=(0, -10, 14))


# ---------------------------------------------------------------- mascot: the ember spirit
def lathe(name, prof, mat, n=48, curl=0.0, curl_from=0.4):
    bm = bmesh.new(); rings = []
    for r, z in prof:
        if r < 1e-4:
            rings.append([bm.verts.new((0, 0, z))] * n)
        else:
            rings.append([bm.verts.new((r * math.cos(2 * math.pi * k / n), r * math.sin(2 * math.pi * k / n), z)) for k in range(n)])
    for a, b in zip(rings, rings[1:]):
        for k in range(n):
            q = [a[k], a[(k + 1) % n], b[(k + 1) % n], b[k]]
            uniq = []
            for v in q:
                if v not in uniq: uniq.append(v)
            if len(uniq) >= 3: bm.faces.new(uniq)
    zmax = max(z for _, z in prof)
    for v in bm.verts:
        if v.co.z > curl_from:
            t = (v.co.z - curl_from) / (zmax - curl_from)
            v.co.x += curl * t * t
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    ob = mesh_obj(name, bm, mat, smooth=True)
    sub = ob.modifiers.new('Sub', 'SUBSURF'); sub.levels = 2; sub.render_levels = 2
    return ob


def mat_flame(name='flame', core=1.4):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; p = _p(m)
    tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    nt.links.new(tc.outputs['Object'], sep.inputs[0])
    mr = nt.nodes.new('ShaderNodeMapRange'); mr.inputs['From Min'].default_value = -0.9; mr.inputs['From Max'].default_value = 1.25
    nt.links.new(sep.outputs['Z'], mr.inputs['Value'])
    rp = nt.nodes.new('ShaderNodeValToRGB'); cr = rp.color_ramp
    cr.elements[0].position = 0.0; cr.elements[0].color = srgb('#ff3a0e')
    cr.elements[1].position = 1.0; cr.elements[1].color = srgb('#ffe45a')
    e = cr.elements.new(0.5); e.color = srgb('#ff8a12')
    nt.links.new(mr.outputs['Result'], rp.inputs['Fac'])
    nt.links.new(rp.outputs['Color'], p.inputs['Base Color'])
    lw = nt.nodes.new('ShaderNodeLayerWeight'); lw.inputs['Blend'].default_value = 0.45
    inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
    pw = nt.nodes.new('ShaderNodeMath'); pw.operation = 'POWER'; pw.inputs[1].default_value = 3.0
    nt.links.new(lw.outputs['Facing'], inv.inputs[1]); nt.links.new(inv.outputs[0], pw.inputs[0])
    mixc = nt.nodes.new('ShaderNodeMix'); mixc.data_type = 'RGBA'; mixc.inputs['B'].default_value = srgb('#fff2a0')
    nt.links.new(pw.outputs[0], mixc.inputs['Factor']); nt.links.new(rp.outputs['Color'], mixc.inputs['A'])
    nt.links.new(mixc.outputs['Result'], p.inputs['Emission Color'])
    st = nt.nodes.new('ShaderNodeMath'); st.operation = 'MULTIPLY_ADD'; st.inputs[1].default_value = core; st.inputs[2].default_value = 0.55
    nt.links.new(pw.outputs[0], st.inputs[0]); nt.links.new(st.outputs[0], p.inputs['Emission Strength'])
    p.inputs['Roughness'].default_value = 0.22; p.inputs['Coat Weight'].default_value = 1.0; p.inputs['Coat Roughness'].default_value = 0.05
    return m


DROP = [(0.0, -0.92), (0.38, -0.86), (0.64, -0.66), (0.8, -0.38), (0.84, -0.06), (0.78, 0.26), (0.62, 0.54),
        (0.42, 0.8), (0.22, 1.02), (0.08, 1.18), (0.0, 1.26)]


def build_mascot(mood='happy'):
    flame = mat_flame()
    white = mat_glossy('eye', '#ffffff', rough=0.1, coat=1.0)
    pupil = mat_glossy('pupil', '#2a1206', rough=0.08, coat=1.0)
    shine = mat_emit('shine', '#ffffff', 6)
    blush = mat_glossy('blush', '#ff4a6a', rough=0.4, coat=0.0, emis=0.6)
    mouth = mat_glossy('mouth', '#7a1606', rough=0.3, coat=0.5)
    tongue = mat_glossy('tongue', '#ff6a7a', rough=0.3, coat=0.5)
    body = lathe('body', DROP, flame, curl=0.32, curl_from=0.3)
    objs = [body]
    for sx in (-1, 1):
        t = lathe('lick', [(r * 0.42, z * 0.42) for r, z in DROP], flame, curl=0.1 * sx * -1, curl_from=0.0)
        t.location = (sx * 0.66, 0.05, 0.36); t.rotation_euler = (0, math.radians(sx * 34), 0)
        objs.append(t)
    if mood == 'joy':
        for sx in (-1, 1):
            pts = [(sx * 0.28 + 0.16 * math.cos(math.radians(a)), -0.79, 0.02 + 0.13 * math.sin(math.radians(a))) for a in range(20, 161, 20)]
            objs.append(tube(pts, 0.04, pupil))
        objs.append(sphere(0.2, loc=(0, -0.76, -0.3), mat=mouth, scale=(1, 0.4, 0.75)))
        objs.append(sphere(0.12, loc=(0, -0.8, -0.38), mat=tongue, scale=(1, 0.4, 0.6)))
    else:
        for sx in (-1, 1):
            objs.append(sphere(0.25, loc=(sx * 0.28, -0.66, 0.04), mat=white, scale=(1, 0.55, 1.2)))
            objs.append(sphere(0.16, loc=(sx * 0.26, -0.78, 0.0), mat=pupil, scale=(1, 0.45, 1.25)))
            objs.append(sphere(0.06, loc=(sx * 0.26 - 0.06, -0.85, 0.1), mat=shine))
            objs.append(sphere(0.028, loc=(sx * 0.26 + 0.05, -0.85, -0.08), mat=shine))
        objs.append(sphere(0.13, loc=(0, -0.77, -0.32), mat=mouth, scale=(1, 0.4, 0.62)))
        objs.append(sphere(0.08, loc=(0, -0.81, -0.36), mat=tongue, scale=(1, 0.4, 0.5)))
    for sx in (-1, 1):
        objs.append(sphere(0.13, loc=(sx * 0.52, -0.63, -0.24), mat=blush, scale=(1, 0.3, 0.6)))
    return group(objs, rot=(0, -6, -8))


# ---------------------------------------------------------------- the full sprite list
# name: (builder, sprite() keyword arguments). The values are the ones the style frames were rendered with.
ASSETS = {
    **{'piece_' + k: (lambda k=k: build_piece(k), {}) for k in 'ROYGBP'},
    'sp_rocket': (lambda: build_rocket(False), {}),
    'sp_rocket_v': (lambda: build_rocket(True), {}),
    'sp_bomb': (build_bomb, {'scale': 2.7}),
    'sp_orb': (build_orb, {}),
    'ob_box1': (lambda: build_crate(1), {'scale': 2.45, 'samples': 96}),
    'ob_box2': (lambda: build_crate(2), {'scale': 2.45, 'samples': 96}),
    'ob_box3': (lambda: build_crate(3), {'scale': 2.45, 'samples': 96}),
    'ob_floor1': (lambda: build_floor(1), {'scale': 2.1, 'samples': 96, 'rig': 0.75}),
    'ob_floor2': (lambda: build_floor(2), {'scale': 2.1, 'samples': 96, 'rig': 0.75}),
    'ob_lantern': (build_lantern, {'scale': 2.1, 'tilt': 12, 'samples': 128}),
    'sp_homing': (build_homing, {'scale': 2.7, 'samples': 128}),
    'ob_ice': (build_ice, {'scale': 2.3, 'glass': True}),
    'bo_hammer': (build_hammer, {'scale': 2.9}),
    'bo_row': (lambda: build_arrows(False), {}),
    'bo_col': (lambda: build_arrows(True), {}),
    'bo_shuffle': (build_shuffle, {'scale': 2.4}),
    'ic_gear': (build_gear, {'scale': 2.4}),
    'cur_coin': (build_coin, {'scale': 2.3}),
    'cur_heart': (build_heart, {'scale': 2.5}),
    'ui_star': (lambda: build_star(False), {'scale': 2.55}),
    'ui_star_empty': (lambda: build_star(True), {'scale': 2.55}),
    'ic_chest': (build_chest, {'scale': 2.6, 'tilt': 20, 'samples': 128}),
    'ic_trophy': (build_trophy, {'scale': 2.5, 'tilt': 8, 'samples': 128}),
    'ic_castle': (build_castle_icon, {'scale': 2.6, 'tilt': 16, 'samples': 128}),
    'ic_scroll': (build_scroll, {'scale': 2.3, 'tilt': 8, 'samples': 128}),
    'mascot': (lambda: build_mascot('happy'), {'scale': 2.9, 'tilt': 4, 'samples': 128}),
    'mascot_joy': (lambda: build_mascot('joy'), {'scale': 2.9, 'tilt': 4, 'samples': 128}),
}


def render_assets(names=None):
    """Render the named sprites (all of them when names is None). Each takes about 10-25 s on the GPU."""
    done = []
    for name in (names or ASSETS):
        build, kw = ASSETS[name]
        sprite(name, build, **kw)
        done.append(name)
    return done
