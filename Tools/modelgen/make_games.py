"""Phase 9 : machines de jeux (roue, machine à sous, marteau, basket, réflexes). Repère local : sol y=0, façade vers -Z."""
import math
import numpy as np
from make_models import model
from fbxlib import *


def sign_quads(m, slot, w, h, cx, cy, front_z, back_z=None, side_x=None, side_w=None):
    m.add(quad(w, h, slot, flip=True), (cx, cy, front_z - 0.004))
    if back_z is not None: m.add(quad(w, h, slot), (cx, cy, back_z + 0.004))
    if side_x is not None:
        for s in (-1, 1): m.add(quad(side_w, h, slot), (s * (side_x + 0.004), cy, 0), R=rot(0, s * 90, 0))


def feet(m, hx, zs):
    for s in (-1, 1):
        for z in zs: m.add(cylinder(0.05, 0.05, 0.03, seg=12, mat="Rubber"), (s * hx, 0.0, z))


def coin_mech(m, y, z):
    m.add(rbox((0.22, 0.3, 0.04), 0.2, mat="Metal"), (0, y, z))
    m.add(box((0.05, 0.1, 0.01), "Dark"), (0, y + 0.07, z - 0.022))
    m.add(box((0.14, 0.07, 0.01), "Dark"), (0, y - 0.09, z - 0.022))
    m.add(torus(0.05, 0.008, seg=18, tube=6, mat="NeonYellow"), (0, y + 0.07, z - 0.023), R=rot(90, 0, 0))


def fan_disc(r, z, uvs, seg=48, mat="Face", facing=-1):
    """Disque plan dans le plan XY à la profondeur z, normale -Z (facing=-1), UV planaires miroir X (vue de face)."""
    ps = np.linspace(0, 2 * math.pi, seg + 1)
    c = np.array([[0, 0, z]] + [[r * math.cos(p), r * math.sin(p), z] for p in ps[:-1]])
    UV = np.column_stack([0.5 - c[:, 0] / (2 * r), 0.5 + c[:, 1] / (2 * r)])
    F = np.array([[0, 1 + k, 1 + (k + 1) % seg] for k in range(seg)])
    return Part(c, np.tile([0, 0, facing], (len(c), 1)), F, mat, UV)


@model
def orientation_probe():      # diagnostic d'import : boîte décalée vers +X, +Y et +Z (centre attendu dans Unity, à comparer)
    m = Model("orientation_probe")
    m.add(box((0.2, 0.2, 0.2), "Main"), (0.5, 0.3, 0.7))
    return m.finish(keep=True)


# ----------------------------------------------------------------- ROUE DE LA FORTUNE
@model
def game_wheel():
    m = Model("game_wheel")
    m.add(rbox((1.4, 0.75, 1.0), 0.12, mat="Paint"), (0, 0.375, 0))
    feet(m, 0.6, (-0.4, 0.4))
    m.add(rbox((1.4, 2.1, 0.3), 0.1, mat="Paint"), (0, 1.55, 0.35))
    m.add(torus(0.64, 0.035, seg=40, tube=8, mat="Metal"), (0, 1.55, 0.2), R=rot(90, 0, 0))
    m.add(cylinder(0.0, 0.07, 0.17, seg=14, mat="NeonYellow"), (0, 2.1, 0.1))
    m.add(rbox((1.4, 0.35, 0.4), 0.12, mat="Paint"), (0, 2.8, 0.2))
    sign_quads(m, "SignWheel", 1.3, 0.3, 0, 2.8, 0.0, back_z=0.4, side_x=0.7, side_w=0.36)
    m.add(box((1.5, 0.03, 0.5), "Metal"), (0, 2.985, 0.2))
    m.add(rbox((1.3, 0.12, 0.5), 0.15, mat="Paint"), (0, 0.81, -0.72))
    m.add(box((1.0, 0.012, 0.3), "Metal"), (0, 0.876, -0.72))
    coin_mech(m, 0.4, -0.52)
    m.add(box((1.3, 0.02, 0.02), "NeonPink"), (0, 0.1, -0.505))
    for s in (-1, 1): m.add(box((0.02, 0.02, 0.9), "NeonCyan"), (s * 0.705, 0.1, 0))
    for k in range(9): m.add(sphere(0.025, seg=8, rings=6, mat="NeonYellow"), (-0.6 + k * 0.15, 2.58, -0.012))
    return m.finish(keep=True)


@model
def wheel_disc():
    m = Model("wheel_disc")
    R = 0.6
    rim = cylinder(R, R, 0.08, seg=48, caps=False, mat="Metal")
    m.add(rim, (0, 0, -0.04), R=rot(90, 0, 0))
    m.add(fan_disc(R, -0.04, None, 48, "WheelFace"))
    m.add(fan_disc(R, 0.04, None, 48, "Dark", facing=1))
    for k in range(12):
        a = math.radians(k * 30 - 90)
        m.add(sphere(0.022, seg=8, rings=6, mat="Metal"), (R * 0.97 * math.cos(a), R * 0.97 * math.sin(a), -0.05))
    m.add(sphere(0.07, seg=14, rings=8, mat="NeonYellow"), (0, 0, -0.05))
    return m.finish(keep=True)


# ----------------------------------------------------------------- MACHINE À SOUS
def reel_part(width, r, seg=36, mat="Reel"):
    """Rouleau d'axe X. ψ=0 pointe vers -Z (face), ψ croît vers +Y. UV : u le long de l'axe (miroir X), v = ψ/360 + 1/12."""
    xs = np.linspace(-width / 2, width / 2, 3); ps = np.linspace(0, 2 * math.pi, seg + 1)
    P = np.array([[[x, r * math.sin(p), -r * math.cos(p)] for p in ps] for x in xs])
    V = P.reshape(-1, 3); Rn, S = len(xs), seg + 1
    N = np.array([[0, math.sin(p), -math.cos(p)] for x in xs for p in ps])
    UV = np.array([[0.5 - x / width, p / (2 * math.pi) + 1 / 12] for x in xs for p in ps])
    F = []
    for i in range(Rn - 1):
        for j in range(S - 1):
            a = i * S + j; F += [[a, a + 1, a + S + 1], [a, a + S + 1, a + S]]
    return Part(V, N, F, mat, UV)


@model
def slot_reel():
    m = Model("slot_reel")
    m.add(reel_part(0.26, 0.22))
    for s in (-1, 1): m.add(cylinder(0.215, 0.215, 0.01, seg=24, mat="Dark"), (s * 0.135, 0, 0), R=rot(0, 0, 90) if s > 0 else rot(0, 0, -90))
    return m.finish(keep=True)


@model
def game_slots():
    m = Model("game_slots")
    m.add(rbox((1.3, 1.0, 0.9), 0.12, mat="Paint"), (0, 0.5, 0))
    feet(m, 0.56, (-0.38, 0.38))
    m.add(rbox((1.3, 0.42, 0.8), 0.1, mat="Paint"), (0, 1.21, 0.05))
    m.add(rbox((1.3, 0.55, 0.8), 0.1, mat="Paint"), (0, 2.175, 0.05))
    for s in (-1, 1): m.add(rbox((0.15, 0.5, 0.8), 0.1, mat="Paint"), (s * 0.575, 1.66, 0.05))
    m.add(rbox((1.3, 0.5, 0.1), 0.1, mat="Dark"), (0, 1.66, 0.4))
    for y in (1.42, 1.9): m.add(box((1.0, 0.04, 0.03), "Metal"), (0, y, -0.355))
    for x in (-0.5, 0.5): m.add(box((0.04, 0.5, 0.03), "Metal"), (x, 1.66, -0.355))
    m.add(box((1.0, 0.02, 0.02), "NeonPink"), (0, 1.445, -0.37)); m.add(box((1.0, 0.02, 0.02), "NeonPink"), (0, 1.875, -0.37))
    m.add(rbox((1.3, 0.4, 0.5), 0.12, mat="Paint"), (0, 2.65, 0.1))
    sign_quads(m, "SignSlots", 1.2, 0.3, 0, 2.65, -0.15, back_z=0.35, side_x=0.65, side_w=0.4)
    m.add(box((1.4, 0.03, 0.6), "Metal"), (0, 2.865, 0.1))
    for k in range(9): m.add(sphere(0.028, seg=8, rings=6, mat="NeonYellow"), (-0.6 + k * 0.15, 2.4, -0.16))
    m.add(rbox((1.3, 0.1, 0.45), 0.15, mat="Paint"), (0, 1.0, -0.62))
    m.add(box((0.9, 0.012, 0.3), "Metal"), (0, 1.056, -0.62))
    m.add(rbox((0.5, 0.12, 0.03), 0.2, mat="Dark"), (0, 0.55, -0.46))
    coin_mech(m, 0.28, -0.46)
    for s in (-1, 1): m.add(box((0.02, 0.02, 0.8), "NeonCyan"), (s * 0.655, 1.0, 0.05))
    return m.finish(keep=True)


# ----------------------------------------------------------------- TESTEUR DE FORCE (MARTEAU)
@model
def game_hammer():
    m = Model("game_hammer")
    m.add(rbox((1.3, 0.3, 1.4), 0.12, mat="Paint"), (0, 0.15, 0))
    feet(m, 0.56, (-0.6, 0.6))
    m.add(rbox((0.55, 0.06, 0.5), 0.2, mat="Rubber"), (0, 0.33, -0.15))
    m.add(torus(0.27, 0.02, seg=24, tube=6, mat="NeonYellow"), (0, 0.365, -0.15))
    m.add(rbox((0.7, 2.3, 0.1), 0.1, mat="Paint"), (0, 1.45, 0.55))
    for s in (-1, 1): m.add(cylinder(0.03, 0.03, 2.1, seg=12, mat="Metal"), (s * 0.18, 0.3, 0.46))
    for k in range(11): m.add(box((0.34 if k % 5 == 0 else 0.22, 0.02, 0.01), "Accent"), (0, 0.62 + k * 0.17, 0.495))
    m.add(superellipsoid((0.17, 0.17, 0.17), 1.0, t0=0, t1=math.pi / 2, mat="Metal"), (0, 2.45, 0.46))
    m.add(sphere(0.03, seg=8, rings=6, mat="NeonYellow"), (0, 2.65, 0.46))
    m.add(rbox((0.9, 0.3, 0.3), 0.12, mat="Paint"), (0, 2.8, 0.5))
    sign_quads(m, "SignHammer", 0.84, 0.24, 0, 2.8, 0.35, back_z=0.65)
    m.add(rbox((0.7, 0.55, 0.4), 0.1, mat="Paint"), (0, 0.575, -0.55))
    m.add(box((0.5, 0.012, 0.25), "Metal"), (0, 0.856, -0.55))
    coin_mech(m, 0.45, -0.76)
    m.add(box((0.6, 0.02, 0.02), "NeonCyan"), (0, 0.32, -0.755))
    return m.finish(keep=True)


@model
def hammer_puck():
    m = Model("hammer_puck")
    m.add(lathe([(0.0, 0.04), (0.06, 0.04), (0.075, 0.0), (0.075, -0.03), (0.0, -0.03)], seg=20, mat="Accent"))
    m.add(torus(0.075, 0.01, seg=20, tube=6, mat="Metal"), (0, 0.005, 0))
    return m.finish(keep=True)


# ----------------------------------------------------------------- BASKET
@model
def game_basket():
    m = Model("game_basket")
    m.add(rbox((1.4, 0.5, 1.6), 0.12, mat="Paint"), (0, 0.25, 0))
    feet(m, 0.62, (-0.7, 0.7))
    for s in (-1, 1): m.add(rbox((0.06, 1.8, 1.5), 0.1, mat="Paint"), (s * 0.67, 1.4, 0))
    m.add(rbox((1.4, 0.1, 1.5), 0.15, mat="Paint"), (0, 2.35, 0))
    m.add(rbox((1.0, 0.7, 0.06), 0.1, mat="Light"), (0, 1.95, 0.6))
    for dx, dy, sx, sy in ((0, 0.18, 0.5, 0.025), (0, -0.0, 0.5, 0.025), (-0.25, 0.09, 0.025, 0.2), (0.25, 0.09, 0.025, 0.2)):
        m.add(box((sx, sy, 0.01), "Accent"), (dx, 1.86 + dy, 0.565))
    m.add(box((0.06, 0.04, 0.12), "Metal"), (0, 1.74, 0.5))
    m.add(torus(0.23, 0.017, seg=28, tube=6, mat="Ring"), (0, 1.75, 0.33))
    for k in range(12):
        a = k * 2 * math.pi / 12
        top = (0.23 * math.cos(a), 1.75, 0.33 + 0.23 * math.sin(a)); bot = (0.13 * math.cos(a + 0.3), 1.45, 0.33 + 0.13 * math.sin(a + 0.3))
        m.add(tube([top, ((top[0] + bot[0]) / 2 * 0.95, 1.6, (top[2] + bot[2]) / 2 * 0.95 + 0.0), bot], 0.006, seg=5, mat="Light"))
    m.add(rbox((1.4, 0.35, 0.5), 0.12, mat="Paint"), (0, 2.72, -0.45))
    sign_quads(m, "SignBasket", 1.3, 0.3, 0, 2.72, -0.7, back_z=-0.2, side_x=0.7, side_w=0.44)
    m.add(box((1.5, 0.03, 0.6), "Metal"), (0, 2.905, -0.45))
    m.add(rbox((1.3, 0.12, 0.5), 0.15, mat="Paint"), (0, 0.62, -0.9))
    m.add(box((0.9, 0.012, 0.3), "Metal"), (0, 0.686, -0.9))
    coin_mech(m, 0.3, -0.81)
    m.add(box((1.3, 0.02, 0.02), "NeonYellow"), (0, 0.1, -0.805))
    return m.finish(keep=True)


@model
def basketball():
    m = Model("basketball")
    seg, rings = 24, 16
    p = sphere(0.12, seg=seg, rings=rings, mat="BallSkin")
    p.UV = np.array([[j / seg, 1 - i / rings] for i in range(rings + 1) for j in range(seg + 1)])
    m.parts.append(p)
    return m.finish(keep=True)


# ----------------------------------------------------------------- RÉFLEXES
@model
def game_reflex():
    m = Model("game_reflex")
    m.add(rbox((1.3, 0.8, 0.9), 0.12, mat="Paint"), (0, 0.4, 0))
    feet(m, 0.56, (-0.38, 0.38))
    m.add(rbox((1.3, 1.5, 0.3), 0.1, mat="Paint"), (0, 1.55, 0.3))
    m.add(rbox((1.24, 0.08, 0.66), 0.15, mat="Dark"), (0, 0.98, -0.06), R=rot(-20, 0, 0))
    m.add(rbox((1.3, 0.35, 0.5), 0.12, mat="Paint"), (0, 2.5, 0.1))
    sign_quads(m, "SignReflex", 1.2, 0.3, 0, 2.5, -0.15, back_z=0.35, side_x=0.65, side_w=0.4)
    m.add(box((1.4, 0.03, 0.6), "Metal"), (0, 2.685, 0.1))
    m.add(box((1.24, 0.02, 0.02), "NeonCyan"), (0, 0.8, -0.455))
    coin_mech(m, 0.45, -0.46)
    for s in (-1, 1): m.add(box((0.02, 0.02, 0.7), "NeonPink"), (s * 0.655, 0.82, 0))
    return m.finish(keep=True)


@model
def reflex_pad():
    m = Model("reflex_pad")
    m.add(lathe([(0.145, 0.0), (0.145, 0.025), (0.125, 0.032)], seg=28, mat="Metal"))
    ps = np.linspace(0, 2 * math.pi, 29)
    c = np.array([[0, 0.034, 0]] + [[0.125 * math.cos(p), 0.034, 0.125 * math.sin(p)] for p in ps[:-1]])
    UV = np.column_stack([0.5 - c[:, 0] / 0.25, 0.5 + c[:, 2] / 0.25])
    F = np.array([[0, 1 + k, 1 + (k + 1) % 28] for k in range(28)])
    m.parts.append(Part(c, np.tile([0, 1, 0], (len(c), 1)), F, "Pad", UV))
    return m.finish(keep=True)
