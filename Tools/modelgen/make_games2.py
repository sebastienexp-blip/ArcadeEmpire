"""Phase 9, 2e série : air hockey, stand de tir, pêche. Repère local : sol y=0, façade vers -Z."""
import math
import numpy as np
from make_models import model
from make_games import sign_quads, feet, coin_mech
from fbxlib import *


def top_quad(w, d, y, mat, uvscale=(1, 1), mirror=True):
    """Rectangle horizontal (normale +Y). UV 0..1 (x miroir pour l'affichage à l'endroit après l'inversion X de l'import)."""
    V = [[-w / 2, y, -d / 2], [w / 2, y, -d / 2], [w / 2, y, d / 2], [-w / 2, y, d / 2]]
    UV = [[1, 0], [0, 0], [0, 1], [1, 1]] if mirror else [[0, 0], [1, 0], [1, 1], [0, 1]]
    UV = [[u * uvscale[0], v * uvscale[1]] for u, v in UV]
    return Part(V, [[0, 1, 0]] * 4, [[0, 2, 1], [0, 3, 2]], mat, UV)


# ----------------------------------------------------------------- AIR HOCKEY
@model
def game_airhockey():
    m = Model("game_airhockey")
    m.add(rbox((1.1, 0.2, 1.3), 0.15, mat="Paint"), (0, 0.1, 0))
    m.add(rbox((1.34, 0.6, 1.54), 0.1, mat="Paint"), (0, 0.5, 0))
    m.add(top_quad(1.1, 1.3, 0.801, "HockeySurface"))
    # rails : côtés pleins, extrémités avec but de 0.34 m
    for s in (-1, 1):
        m.add(rbox((0.1, 0.12, 1.54), 0.2, mat="Plastic"), (s * 0.62, 0.84, 0))
        for z in (-0.72, 0.72):
            m.add(rbox((0.38, 0.12, 0.1), 0.2, mat="Plastic"), (s * 0.36, 0.84, z))
            m.add(box((0.02, 0.02, 0.02), "Metal"), (s * 0.19, 0.91, z))
    for z, col in ((-0.74, "NeonCyan"), (0.74, "NeonPink")):
        m.add(box((0.34, 0.02, 0.02), col), (0, 0.9, z))
    m.add(rbox((1.0, 0.08, 0.2), 0.2, mat="Dark"), (0, 0.8, -0.7), R=None) if False else None
    # montants + enseigne
    for x in (-0.6, 0.6): m.add(rbox((0.08, 1.1, 0.08), 0.2, mat="Metal"), (x, 1.35, 0.7))
    m.add(rbox((1.4, 0.34, 0.2), 0.12, mat="Paint"), (0, 2.0, 0.7))
    sign_quads(m, "SignHockey", 1.3, 0.28, 0, 2.0, 0.6, back_z=0.8)
    for k in range(7): m.add(sphere(0.025, seg=8, rings=6, mat="NeonYellow"), (-0.45 + k * 0.15, 1.8, 0.585))
    feet(m, 0.55, (-0.65, 0.65))
    coin_mech(m, 0.45, -0.78)
    m.add(box((1.2, 0.02, 0.02), "NeonCyan"), (0, 0.2, -0.775))
    return m.finish(keep=True)


@model
def hockey_puck():
    m = Model("hockey_puck")
    m.add(lathe([(0.0, 0.02), (0.034, 0.02), (0.04, 0.012), (0.04, 0.0), (0.0, 0.0)], seg=24, mat="Accent"))
    return m.finish(keep=True)


@model
def hockey_mallet():
    m = Model("hockey_mallet")
    m.add(lathe([(0.0, 0.0), (0.06, 0.0), (0.062, 0.012), (0.05, 0.02), (0.02, 0.026)], seg=24, mat="Mallet"))
    m.add(cylinder(0.014, 0.014, 0.04, seg=12, mat="Metal"), (0, 0.02, 0))
    m.add(sphere(0.026, seg=14, rings=9, mat="Mallet"), (0, 0.075, 0))
    return m.finish(keep=True)


# ----------------------------------------------------------------- STAND DE TIR
@model
def game_shooting():
    m = Model("game_shooting")
    m.add(rbox((1.5, 0.9, 0.7), 0.1, mat="Paint"), (0, 0.45, -0.75))
    m.add(box((1.4, 0.015, 0.5), "Metal"), (0, 0.907, -0.75))
    feet(m, 0.65, (-0.95, -0.55))
    for s in (-1, 1): m.add(rbox((0.1, 2.6, 2.3), 0.12, mat="Paint"), (s * 0.7, 1.3, 0.25))
    m.add(rbox((1.5, 2.6, 0.1), 0.1, mat="Paint"), (0, 1.3, 1.35))
    m.add(rbox((1.5, 0.1, 2.3), 0.12, mat="Paint"), (0, 2.55, 0.25))
    # décor du fond : bandes de couleur
    for k, col in enumerate(("NeonCyan", "NeonPink", "NeonYellow")): m.add(box((1.3, 0.04, 0.02), col), (0, 0.5 + k * 0.05, 1.29))
    m.add(rbox((1.5, 0.34, 0.5), 0.12, mat="Paint"), (0, 2.85, -0.5))
    sign_quads(m, "SignShoot", 1.4, 0.28, 0, 2.85, -0.75, back_z=-0.25, side_x=0.75, side_w=0.44)
    m.add(box((1.6, 0.03, 0.6), "Metal"), (0, 3.035, -0.5))
    # baffles où se cachent les cibles (3 rangées)
    for y in (0.95, 1.45, 1.95): m.add(rbox((1.3, 0.08, 0.35), 0.2, mat="Dark"), (0, y - 0.16, 1.0))
    m.add(rbox((0.5, 0.12, 0.3), 0.2, mat="Dark"), (0, 0.62, -0.5)) if False else None
    coin_mech(m, 0.4, -1.12)
    m.add(box((1.4, 0.02, 0.02), "NeonPink"), (0, 0.12, -1.105))
    return m.finish(keep=True)


@model
def shoot_target():
    """Cible plate, face vers -Z, pivot au bas de la cible (y=0), hauteur 0.34."""
    m = Model("shoot_target")
    R = 0.17
    ps = np.linspace(0, 2 * math.pi, 41)
    c = np.array([[0, R, -0.012]] + [[R * math.cos(p), R + R * math.sin(p), -0.012] for p in ps[:-1]])
    UV = np.column_stack([0.5 - (c[:, 0]) / (2 * R), 0.5 + (c[:, 1] - R) / (2 * R)])
    F = np.array([[0, 1 + k, 1 + (k + 1) % 40] for k in range(40)])
    m.parts.append(Part(c, np.tile([0, 0, -1], (len(c), 1)), F, "TargetFace", UV))
    m.add(cylinder(R, R, 0.024, seg=40, caps=False, mat="Metal"), (0, R, -0.012), R=rot(90, 0, 0))
    m.add(fan_back(R), (0, R, 0.012))
    m.add(box((0.05, 0.12, 0.03), "Metal"), (0, 0.04, 0.03))
    return m.finish(keep=True)


def fan_back(R, seg=40):
    ps = np.linspace(0, 2 * math.pi, seg + 1)
    c = np.array([[0, 0, 0]] + [[R * math.cos(p), R * math.sin(p), 0] for p in ps[:-1]])
    F = np.array([[0, 1 + k, 1 + (k + 1) % seg] for k in range(seg)])
    return Part(c, np.tile([0, 0, 1], (len(c), 1)), F, "Dark", np.zeros((len(c), 2)))


@model
def shoot_crosshair():
    m = Model("shoot_crosshair")
    m.add(torus(0.05, 0.004, seg=28, tube=6, mat="NeonYellow"), R=rot(90, 0, 0))
    for dx, dy, sx, sy in ((0.07, 0, 0.04, 0.006), (-0.07, 0, 0.04, 0.006), (0, 0.07, 0.006, 0.04), (0, -0.07, 0.006, 0.04)):
        m.add(box((sx, sy, 0.004), "NeonYellow"), (dx, dy, 0))
    return m.finish(keep=True)


# ----------------------------------------------------------------- PÊCHE
@model
def game_fishing():
    m = Model("game_fishing")
    m.add(rbox((1.5, 0.7, 1.5), 0.1, mat="Paint"), (0, 0.35, 0))
    feet(m, 0.65, (-0.65, 0.65))
    # bassin : bords + eau
    for z in (-0.68, 0.68): m.add(rbox((1.5, 0.14, 0.14), 0.2, mat="Plastic"), (0, 0.77, z))
    for x in (-0.68, 0.68): m.add(rbox((0.14, 0.14, 1.22), 0.2, mat="Plastic"), (x, 0.77, 0))
    m.add(top_quad(1.22, 1.22, 0.74, "Water", uvscale=(1.2, 1.2)))
    m.add(rbox((1.2, 0.05, 1.2), 0.2, mat="Dark"), (0, 0.71, 0))
    # piliers + toit
    for x in (-0.68, 0.68):
        for z in (-0.68, 0.68): m.add(rbox((0.07, 1.2, 0.07), 0.2, mat="Metal"), (x, 1.4, z))
    m.add(rbox((1.5, 0.1, 1.5), 0.15, mat="Paint"), (0, 2.05, 0))
    m.add(rbox((1.5, 0.34, 0.5), 0.12, mat="Paint"), (0, 2.34, -0.5))
    sign_quads(m, "SignFishing", 1.4, 0.28, 0, 2.34, -0.75, back_z=-0.25, side_x=0.75, side_w=0.44)
    m.add(box((1.6, 0.03, 0.6), "Metal"), (0, 2.525, -0.5))
    for k in range(9): m.add(sphere(0.025, seg=8, rings=6, mat="NeonCyan"), (-0.6 + k * 0.15, 1.98, -0.755)) if False else None
    m.add(box((1.4, 0.02, 0.02), "NeonCyan"), (0, 0.12, -0.755))
    coin_mech(m, 0.4, -0.76)
    return m.finish(keep=True)


@model
def fishing_hook():
    m = Model("fishing_hook")
    m.add(cylinder(0.02, 0.02, 0.04, seg=10, mat="Metal"), (0, -0.04, 0))
    m.add(tube([(0, -0.04, 0), (0, -0.1, 0), (0.025, -0.15, 0), (0.05, -0.13, 0), (0.045, -0.09, 0)], 0.006, seg=6, mat="Metal"))
    m.add(sphere(0.02, seg=10, rings=7, mat="Rubber"), (0, 0.0, 0))
    return m.finish(keep=True)


@model
def fishing_carriage():
    m = Model("fishing_carriage")
    m.add(rbox((0.2, 0.08, 0.2), 0.2, mat="Metal"), (0, 0, 0))
    m.add(box((0.14, 0.012, 0.14), "NeonCyan"), (0, -0.04, 0))
    return m.finish(keep=True)


def fish(name, body, tail, uv=2.0):
    m = Model(name, uv=uv)
    bx, by, bz = body
    m.add(superellipsoid((bx, by, bz), 0.9, seg=16, rings=10, mat="Main"), R=rot(90, 0, 0) if False else None)
    # le poisson est orienté : tête vers +Z
    m.parts[-1] = m.parts[-1].transformed(rot(90, 0, 0).T @ np.eye(3) if False else np.eye(3))
    m.add(cylinder(0.0, tail, tail * 1.1, seg=6, mat="Main"), (0, 0, -bz * 0.85), R=rot(-90, 0, 0), S=(0.4, 1, 1))
    m.add(superellipsoid((0.004, by * 0.7, bz * 0.45), 1.0, seg=8, rings=6, mat="Main"), (0, by * 0.9, -bz * 0.1))
    for s in (-1, 1):
        m.add(sphere(by * 0.28, seg=8, rings=6, mat="Light"), (s * bx * 0.8, by * 0.2, bz * 0.62))
        m.add(sphere(by * 0.14, seg=6, rings=5, mat="Dark"), (s * bx * 0.92, by * 0.2, bz * 0.68))
        m.add(superellipsoid((0.004, by * 0.35, bz * 0.18), 1.0, seg=8, rings=6, mat="Main"), (s * bx * 0.9, -by * 0.2, bz * 0.1), R=rot(0, 0, s * 25))
    return m.finish(keep=True)


@model
def fish_small():
    return fish("fish_small", (0.035, 0.045, 0.085), 0.05)


@model
def fish_big():
    return fish("fish_big", (0.05, 0.065, 0.13), 0.08)
