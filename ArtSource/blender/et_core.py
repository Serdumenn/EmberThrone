"""EmberThrone render pipeline: shared scene, light, camera and material helpers.

Runs inside Blender 5.1 (through the MCP bridge or Blender's Python console):
    exec(open(r"<repo>/ArtSource/blender/et_core.py", encoding="utf-8").read())
Asset builders live in et_assets.py; render outputs go to OUT_DIR as transparent PNG sprites.
Every look decision (colors, light positions, bevel sizes) is here so all assets share one style.
"""
import bpy, bmesh, math, os
from mathutils import Vector

REPO = globals().get("ET_REPO", r"C:\Users\fatih\Documents\GitHub\EmberThrone")
OUT_DIR = globals().get("ET_OUT", os.path.join(REPO, "docs", "design", "style-frames", "img"))
os.makedirs(OUT_DIR, exist_ok=True)


def srgb(h, a=1.0):
    h = h.lstrip('#'); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in c) + (a,)


# ---------------------------------------------------------------- scene
def reset_scene():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.curves, bpy.data.lights, bpy.data.cameras, bpy.data.metaballs):
        for d in list(coll):
            if d.users == 0: coll.remove(d)
    sc = bpy.context.scene
    for c in list(sc.collection.children): sc.collection.children.unlink(c)
    return sc


def setup_render(w=512, h=512, samples=160, transparent=True, glass=False):
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.samples = samples
    sc.cycles.use_denoising = True
    sc.cycles.max_bounces = 10; sc.cycles.transmission_bounces = 10; sc.cycles.glossy_bounces = 6
    sc.render.film_transparent = transparent
    sc.cycles.film_transparent_glass = glass
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    sc.view_settings.exposure = 0.0
    sc.view_settings.gamma = 1.0
    sc.render.resolution_x = w; sc.render.resolution_y = h; sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = 'PNG'; sc.render.image_settings.color_mode = 'RGBA'
    sc.render.image_settings.compression = 90
    try:
        prefs = bpy.context.preferences.addons['cycles'].preferences
        prefs.get_devices()
        for t in ('OPTIX', 'CUDA', 'HIP', 'ONEAPI', 'METAL'):
            if any(d.type == t for d in prefs.devices):
                prefs.compute_device_type = t
                for d in prefs.devices: d.use = True
                sc.cycles.device = 'GPU'
                break
    except Exception:
        pass
    return sc


def setup_world(hdri='studio.exr', strength=0.55, rot_z=40.0, tint=None):
    sc = bpy.context.scene
    world = bpy.data.worlds.get('ET_World') or bpy.data.worlds.new('ET_World')
    sc.world = world; world.use_nodes = True
    nt = world.node_tree; nt.nodes.clear()
    coord = nt.nodes.new('ShaderNodeTexCoord'); mapn = nt.nodes.new('ShaderNodeMapping')
    mapn.inputs['Rotation'].default_value[2] = math.radians(rot_z)
    env = nt.nodes.new('ShaderNodeTexEnvironment')
    env.image = bpy.data.images.load(os.path.join(bpy.utils.system_resource('DATAFILES', path='studiolights/world'), hdri), check_existing=True)
    bg = nt.nodes.new('ShaderNodeBackground'); bg.inputs['Strength'].default_value = strength
    out = nt.nodes.new('ShaderNodeOutputWorld')
    nt.links.new(coord.outputs['Generated'], mapn.inputs['Vector']); nt.links.new(mapn.outputs['Vector'], env.inputs['Vector'])
    if tint:
        mix = nt.nodes.new('ShaderNodeMix'); mix.data_type = 'RGBA'; mix.blend_type = 'MULTIPLY'; mix.inputs['Factor'].default_value = 1.0
        mix.inputs['B'].default_value = srgb(tint)
        nt.links.new(env.outputs['Color'], mix.inputs['A']); nt.links.new(mix.outputs['Result'], bg.inputs['Color'])
    else:
        nt.links.new(env.outputs['Color'], bg.inputs['Color'])
    nt.links.new(bg.outputs['Background'], out.inputs['Surface'])


def area(name, loc, target=(0, 0, 0), energy=1000, size=2.0, color='#ffffff', shape='DISK'):
    ld = bpy.data.lights.new(name, 'AREA'); ld.energy = energy; ld.size = size; ld.color = srgb(color)[:3]
    ld.shape = shape
    ob = bpy.data.objects.new(name, ld); bpy.context.scene.collection.objects.link(ob)
    ob.location = loc
    d = Vector(target) - Vector(loc)
    ob.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    return ob


def studio_rig(scale=1.0, warm_rim=True):
    """Sprite light rig: crisp key from top-left, warm and cool rims for the glow edge, soft fill, big top box."""
    s = scale
    area('Key', (-5 * s, -7 * s, 7 * s), energy=2400 * s * s, size=1.6 * s, color='#fff6ea')
    area('Fill', (7 * s, -6 * s, 1 * s), energy=160 * s * s, size=7 * s, color='#fff0dc')
    area('Top', (0, -2 * s, 9 * s), energy=260 * s * s, size=10 * s, shape='SQUARE')
    area('RimL', (-6 * s, 5 * s, 3 * s), energy=1900 * s * s, size=2.5 * s, color='#ffcf80' if warm_rim else '#ffffff')
    area('RimR', (6 * s, 5 * s, 4 * s), energy=1500 * s * s, size=2.5 * s, color='#a6dcff')
    area('Spec', (-2.6 * s, -9 * s, 3.6 * s), energy=1400 * s * s, size=0.9 * s, color='#ffffff')
    area('Glint', (3.5 * s, -8 * s, 5.5 * s), energy=700 * s * s, size=0.5 * s, color='#ffffff')


def ortho_camera(scale=2.6, tilt=10.0, dist=30.0, target=(0, 0, 0)):
    cd = bpy.data.cameras.new('Cam'); cd.type = 'ORTHO'; cd.ortho_scale = scale
    cam = bpy.data.objects.new('Cam', cd); bpy.context.scene.collection.objects.link(cam)
    t = math.radians(tilt)
    cam.location = (target[0], target[1] - dist * math.cos(t), target[2] + dist * math.sin(t))
    cam.rotation_euler = (math.radians(90) - t, 0, 0)
    bpy.context.scene.camera = cam
    return cam


def render(name, out_dir=None):
    sc = bpy.context.scene
    path = os.path.join(out_dir or OUT_DIR, name + ('.png' if sc.render.image_settings.file_format == 'PNG' else '.jpg'))
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


# ---------------------------------------------------------------- materials
def _p(m):
    return m.node_tree.nodes['Principled BSDF']


def mat_glossy(name, color, rough=0.16, coat=1.0, emis=0.0, emis_color=None, metallic=0.0, sss=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True; p = _p(m)
    p.inputs['Base Color'].default_value = srgb(color)
    p.inputs['Roughness'].default_value = rough
    p.inputs['Metallic'].default_value = metallic
    p.inputs['Coat Weight'].default_value = coat; p.inputs['Coat Roughness'].default_value = 0.04
    if sss:
        p.inputs['Subsurface Weight'].default_value = sss
        p.inputs['Subsurface Radius'].default_value = (1.0, 0.6, 0.4)
    if emis:
        p.inputs['Emission Color'].default_value = srgb(emis_color or color)
        p.inputs['Emission Strength'].default_value = emis
    return m


def mat_gem(name, color, glow, inner=0.55, rough=0.06):
    """Candy-gem shader: saturated glossy body, coat for the wet shine, and an inner glow that is
    strongest on faces looking at the camera (fakes light passing through the stone)."""
    m = mat_glossy(name, color, rough=rough, coat=1.0)
    nt = m.node_tree; p = _p(m)
    p.inputs['IOR'].default_value = 1.75
    p.inputs['Specular IOR Level'].default_value = 0.7
    p.inputs['Emission Color'].default_value = srgb(glow)
    lw = nt.nodes.new('ShaderNodeLayerWeight'); lw.inputs['Blend'].default_value = 0.35
    inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
    pw = nt.nodes.new('ShaderNodeMath'); pw.operation = 'POWER'; pw.inputs[1].default_value = 2.2
    mul = nt.nodes.new('ShaderNodeMath'); mul.operation = 'MULTIPLY'; mul.inputs[1].default_value = inner
    nt.links.new(lw.outputs['Facing'], inv.inputs[1]); nt.links.new(inv.outputs[0], pw.inputs[0])
    nt.links.new(pw.outputs[0], mul.inputs[0]); nt.links.new(mul.outputs[0], p.inputs['Emission Strength'])
    return m


def mat_metal(name, color, rough=0.2, emis=0.0):
    return mat_glossy(name, color, rough=rough, coat=0.4, metallic=1.0, emis=emis)


def mat_gold(name='Gold', deep=False):
    return mat_metal(name, '#e88a00' if deep else '#ffc22e', rough=0.24 if deep else 0.18, emis=0.05 if deep else 0.1)


def mat_emit(name, color, strength=4.0):
    m = bpy.data.materials.new(name); m.use_nodes = True; p = _p(m)
    p.inputs['Base Color'].default_value = srgb(color)
    p.inputs['Emission Color'].default_value = srgb(color); p.inputs['Emission Strength'].default_value = strength
    return m


def mat_wood(name='Wood', light='#f0a85a', dark='#a85a24', scale=6.0, rough=0.45):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; p = _p(m)
    tc = nt.nodes.new('ShaderNodeTexCoord')
    wave = nt.nodes.new('ShaderNodeTexWave'); wave.wave_type = 'BANDS'; wave.bands_direction = 'Z'
    wave.inputs['Scale'].default_value = scale; wave.inputs['Distortion'].default_value = 1.6; wave.inputs['Detail'].default_value = 1.5
    wave.inputs['Detail Scale'].default_value = 0.6
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color = srgb(dark); ramp.color_ramp.elements[1].color = srgb(light)
    ramp.color_ramp.elements[0].position = 0.0; ramp.color_ramp.elements[1].position = 0.55
    nt.links.new(tc.outputs['Object'], wave.inputs['Vector']); nt.links.new(wave.outputs['Fac'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], p.inputs['Base Color'])
    p.inputs['Roughness'].default_value = rough; p.inputs['Coat Weight'].default_value = 0.35
    bump = nt.nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = 0.1
    nt.links.new(wave.outputs['Fac'], bump.inputs['Height']); nt.links.new(bump.outputs['Normal'], p.inputs['Normal'])
    return m


def mat_ice(name='Ice'):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; p = _p(m)
    p.inputs['Base Color'].default_value = srgb('#8fdcff')
    p.inputs['Transmission Weight'].default_value = 1.0
    p.inputs['Roughness'].default_value = 0.12; p.inputs['IOR'].default_value = 1.31
    p.inputs['Emission Color'].default_value = srgb('#c8f2ff'); p.inputs['Emission Strength'].default_value = 0.35
    p.inputs['Coat Weight'].default_value = 1.0; p.inputs['Coat Roughness'].default_value = 0.02
    noise = nt.nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 4.0; noise.inputs['Detail'].default_value = 8.0
    bump = nt.nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = 0.35
    nt.links.new(noise.outputs['Fac'], bump.inputs['Height']); nt.links.new(bump.outputs['Normal'], p.inputs['Normal'])
    # frosty cracks: thin white lines from a Voronoi edge pattern
    vor = nt.nodes.new('ShaderNodeTexVoronoi'); vor.feature = 'DISTANCE_TO_EDGE'; vor.inputs['Scale'].default_value = 1.6
    mr = nt.nodes.new('ShaderNodeMapRange'); mr.inputs['From Max'].default_value = 0.012; mr.inputs['To Min'].default_value = 0.8; mr.inputs['To Max'].default_value = 0.3
    nt.links.new(vor.outputs['Distance'], mr.inputs['Value']); nt.links.new(mr.outputs['Result'], p.inputs['Emission Strength'])
    return m


# ---------------------------------------------------------------- geometry
def link(ob):
    bpy.context.scene.collection.objects.link(ob); return ob


def mesh_obj(name, bm, mat=None, smooth=False):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = link(bpy.data.objects.new(name, me))
    if mat: me.materials.append(mat)
    for poly in me.polygons: poly.use_smooth = smooth
    return ob


def bevel(ob, width, seg=3, harden=False, limit='NONE', clamp=True):
    b = ob.modifiers.new('Bevel', 'BEVEL'); b.width = width; b.segments = seg; b.limit_method = limit
    b.use_clamp_overlap = clamp; b.harden_normals = harden
    return b


def smooth_by_angle(ob, angle=35):
    for poly in ob.data.polygons: poly.use_smooth = True
    try:
        ob.data.set_sharp_from_angle(angle=math.radians(angle))
    except Exception:
        pass


def round_corners(pts, r, seg=4):
    """Replace each polygon corner by a small arc so shapes read soft and toy-like."""
    out = []
    n = len(pts)
    for i in range(n):
        p0, p1, p2 = Vector(pts[i - 1]), Vector(pts[i]), Vector(pts[(i + 1) % n])
        a = (p0 - p1).normalized(); b = (p2 - p1).normalized()
        rr = min(r, (p0 - p1).length * 0.45, (p2 - p1).length * 0.45)
        s0 = p1 + a * rr; s2 = p1 + b * rr
        for k in range(seg + 1):
            t = k / seg
            q = (1 - t) ** 2 * s0 + 2 * (1 - t) * t * p1 + t ** 2 * s2
            out.append((q.x, q.y))
    return out


def ring(n, r, rot=90.0, sx=1.0, sy=1.0):
    return [(sx * r * math.cos(math.radians(rot + k * 360 / n)), sy * r * math.sin(math.radians(rot + k * 360 / n))) for k in range(n)]


def star_pts(n=5, ro=1.0, ri=0.5, rot=90.0):
    return [((ri if k % 2 else ro) * math.cos(math.radians(rot + k * 180 / n)), (ri if k % 2 else ro) * math.sin(math.radians(rot + k * 180 / n))) for k in range(n * 2)]


def prism(name, pts2d, depth, mat=None, y0=0.0):
    """Extrude a 2D outline (x, z) toward the camera (-Y) by depth."""
    bm = bmesh.new()
    vs = [bm.verts.new((x, y0, z)) for x, z in pts2d]
    f = bm.faces.new(vs)
    ext = bmesh.ops.extrude_face_region(bm, geom=[f])
    for v in [e for e in ext['geom'] if isinstance(e, bmesh.types.BMVert)]: v.co.y -= depth
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return mesh_obj(name, bm, mat)


def gem(name, outline, mat, table=0.56, mid=0.82, crown=0.42, band=0.12, back=0.6, bev=0.035, dome=0.12):
    """Faceted gem facing the camera: table, two crown tiers, a girdle band and a pavilion."""
    bm = bmesh.new(); n = len(outline)
    cx = sum(p[0] for p in outline) / n; cz = sum(p[1] for p in outline) / n
    sc = lambda k, y: [bm.verts.new((cx + (x - cx) * k, y, cz + (z - cz) * k)) for x, z in outline]
    g0 = sc(1.0, 0.0); g1 = sc(1.0, band); m1 = sc(mid, -crown * 0.55); tb = sc(table, -crown)
    bk = bm.verts.new((cx, band + back, cz))
    tc = bm.verts.new((cx, -crown - dome, cz))  # low dome on the table: its facets catch the light at different angles
    for i in range(n):
        bm.faces.new((tb[(i + 1) % n], tb[i], tc))
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((g0[i], g0[j], m1[j], m1[i]))
        bm.faces.new((m1[i], m1[j], tb[j], tb[i]))
        bm.faces.new((g1[i], g1[j], g0[j], g0[i]))
        bm.faces.new((g1[j], g1[i], bk))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    ob = mesh_obj(name, bm, mat)
    bevel(ob, bev, 3)
    return ob
