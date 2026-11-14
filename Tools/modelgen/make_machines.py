"""Machines, pièces, commandes et personnage. Coordonnées = repère local du prefab (sol y=0, avant de la machine vers -Z).
Les coques sont symétriques en X (pas de dépendance à l'inversion d'axe X à l'import)."""
import math
from make_models import model, MODELS
from fbxlib import *


def led(m, size, t, mat):
    m.add(box(size, mat), t)


# ------------------------------------------------------------------ MACHINE À PINCES : coque visuelle
@model
def claw_shell():
    m = Model("claw_shell", uv=1.0)
    m.add(rbox((1.4, 0.3, 1.2), 0.12, mat="Paint"), (0, 0.15, 0))
    for s in (-1, 1):
        m.add(rbox((0.1, 0.25, 1.2), 0.14, mat="Paint"), (s * 0.65, 0.425, 0))
        for z in (-0.52, 0.52):
            m.add(cylinder(0.05, 0.05, 0.03, seg=12, mat="Rubber"), (s * 0.6, 0.0, z))           # pieds
    m.add(rbox((1.2, 0.25, 0.1), 0.14, mat="Paint"), (0, 0.425, 0.55))
    m.add(rbox((1.2, 0.25, 0.1), 0.14, mat="Paint"), (0, 0.425, -0.55))
    led(m, (1.3, 0.02, 0.02), (0, 0.06, -0.605), "NeonPink")                    # liseré LED façade basse
    for z in (-0.5, 0.5):
        for s in (-1, 1): led(m, (0.02, 0.02, 1.0), (s * 0.705, 0.1, 0), "NeonPink") if z < 0 and s else None
    # montants et rail
    for x, z in ((-0.62, -0.52), (0.62, -0.52), (-0.62, 0.52), (0.62, 0.52)):
        m.add(rbox((0.07, 1.22, 0.07), 0.2, mat="Metal"), (x, 1.15, z))
    for y in (0.56, 1.76):
        m.add(box((1.3, 0.025, 0.025), "Metal"), (0, y, -0.58)); m.add(box((1.3, 0.025, 0.025), "Metal"), (0, y, 0.58))
    # plafond + enseigne
    m.add(rbox((1.42, 0.1, 1.22), 0.14, mat="Paint"), (0, 1.8, 0))
    for x in (-0.55, 0.55):
        for z in (-0.4, 0.4): m.add(cylinder(0.06, 0.04, 0.02, seg=14, mat="Plastic"), (x * 0.9, 1.74, z * 0.8), R=rot(180, 0, 0), )   # spots
    m.add(rbox((1.44, 0.34, 0.84), 0.12, mat="Paint"), (0, 2.0, -0.1))
    m.add(quad(1.3, 0.26, "SignClaw", flip=True), (0, 2.0, -0.1 - 0.42 - 0.004))
    m.add(quad(1.3, 0.26, "SignClaw"), (0, 2.0, -0.1 + 0.42 + 0.004))
    for s in (-1, 1): m.add(quad(0.7, 0.26, "SignClaw"), (s * 0.725, 2.0, -0.1), R=rot(0, s * 90, 0))
    m.add(box((1.5, 0.03, 0.9), "Metal"), (0, 2.185, -0.1))
    # pupitre
    m.add(rbox((1.4, 0.6, 0.5), 0.1, mat="Paint"), (0, 0.3, -0.85))
    m.add(rbox((1.42, 0.05, 0.52), 0.15, mat="Paint"), (0, 0.625, -0.85))
    m.add(box((1.0, 0.012, 0.34), "Metal"), (0, 0.653, -0.83))                    # plaque de commande
    m.add(quad(0.38, 0.19, "DecalClaw"), (0, 0.661, -0.95), R=rot(0, 180, 0) @ rot(-90, 0, 0))
    led(m, (1.3, 0.015, 0.015), (0, 0.5, -1.105), "NeonCyan")
    # monnayeur central
    m.add(rbox((0.24, 0.3, 0.04), 0.2, mat="Metal"), (0, 0.33, -1.11))
    m.add(box((0.012, 0.09, 0.012), "Dark"), (0, 0.4, -1.133)); m.add(box((0.012, 0.09, 0.012), "Dark"), (0, 0.4, -1.133))
    m.add(box((0.05, 0.1, 0.01), "Dark"), (0, 0.4, -1.132))
    m.add(box((0.14, 0.07, 0.01), "Dark"), (0, 0.24, -1.132))
    m.add(torus(0.05, 0.008, seg=18, tube=6, mat="NeonYellow"), (0, 0.4, -1.133), R=rot(90, 0, 0))
    return m.finish(keep=True)


@model
def claw_head():
    m = Model("claw_head", uv=1.0)
    m.add(lathe([(0.0, 0.07), (0.05, 0.07), (0.075, 0.045), (0.08, 0.0), (0.075, -0.02), (0.0, -0.02)], seg=24, mat="Metal"))
    m.add(lathe([(0.0, 0.1), (0.035, 0.1), (0.04, 0.07)], seg=14, mat="Dark"))
    m.add(torus(0.062, 0.008, seg=24, tube=6, mat="NeonCyan"), (0, 0.0, 0))
    for k in range(3):
        a = math.radians(k * 120)
        m.add(rbox((0.05, 0.03, 0.03), 0.3, mat="Metal"), (math.cos(a) * 0.062, -0.025, math.sin(a) * 0.062), R=rot(0, -k * 120, 0))
    return m.finish(keep=True)


@model
def claw_prong():
    m = Model("claw_prong", uv=1.0)
    m.add(sphere(0.02, seg=10, rings=7, mat="Metal"), (0, 0.0, 0))
    m.add(tube([(0, 0, 0), (0, -0.05, 0), (0, -0.12, 0), (0, -0.18, 0)], [0.013, 0.014, 0.011, 0.007], seg=10, mat="Metal"))
    m.add(sphere(0.011, seg=8, rings=6, mat="Rubber"), (0, -0.19, 0))
    return m.finish(keep=True)


@model
def claw_bridge():
    """Pont mobile (axe X) du portique : poutre, chariots d'extrémité et galets. Centré en X, symétrique."""
    m = Model("claw_bridge", uv=1.0)
    m.add(rbox((1.28, 0.05, 0.05), 0.2, mat="Metal"), (0, 0.0, 0))
    m.add(box((1.24, 0.012, 0.012), "NeonCyan"), (0, 0.032, -0.027))
    for s in (-1, 1):
        m.add(rbox((0.1, 0.09, 0.12), 0.2, mat="Paint"), (s * 0.6, 0.0, 0))
        for z in (-0.045, 0.045):
            m.add(cylinder(0.022, 0.022, 0.02, seg=14, mat="Rubber"), (s * 0.6, 0.05, z), R=rot(0, 0, 90))
    return m.finish(keep=True)


@model
def claw_carriage():
    """Chariot qui roule sur le pont et porte le câble. Centré sur le câble."""
    m = Model("claw_carriage", uv=1.0)
    m.add(rbox((0.14, 0.05, 0.1), 0.25, mat="Paint"), (0, 0.0, 0))
    m.add(cylinder(0.035, 0.035, 0.03, seg=16, mat="Metal"), (0, -0.04, 0))
    m.add(torus(0.045, 0.006, seg=18, tube=6, mat="NeonYellow"), (0, 0.027, 0), R=rot(90, 0, 0))
    for s in (-1, 1):
        for z in (-0.03, 0.03):
            m.add(cylinder(0.018, 0.018, 0.012, seg=12, mat="Rubber"), (s * 0.075, 0.0, z), R=rot(0, 0, 90))
    return m.finish(keep=True)


@model
def arcade_stick():
    m = Model("arcade_stick", uv=1.0)
    m.add(lathe([(0.055, 0.0), (0.05, 0.012), (0.03, 0.03), (0.012, 0.05)], seg=18, mat="Rubber", smooth=True))
    m.add(cylinder(0.01, 0.01, 0.18, seg=10, mat="Metal"), (0, 0.04, 0))
    m.add(sphere(0.037, seg=16, rings=10, mat="StickBall"), (0, 0.235, 0))
    return m.finish(keep=True)


@model
def arcade_button():
    m = Model("arcade_button", uv=1.0)
    m.add(lathe([(0.1, -0.012), (0.1, 0.0), (0.085, 0.01)], seg=24, mat="Metal"))
    m.add(lathe([(0.075, -0.01), (0.075, 0.004), (0.06, 0.012), (0.03, 0.016), (0.0, 0.017)], seg=24, mat="ButtonColor", smooth=True))
    return m.finish(keep=True)


# ------------------------------------------------------------------ COIN PUSHER
@model
def pusher_shell():
    m = Model("pusher_shell", uv=1.0)
    m.add(rbox((1.5, 0.5, 1.85), 0.1, mat="Paint"), (0, 0.25, 0.025))
    led(m, (1.35, 0.02, 0.02), (0, 0.12, -0.905), "NeonPink")
    for s in (-1, 1):
        m.add(rbox((0.1, 0.4, 0.35), 0.15, mat="Paint"), (s * 0.7, 0.7, -0.725))
        m.add(rbox((0.1, 0.6, 0.1), 0.2, mat="Metal"), (s * 0.7, 1.2, -0.55))
        m.add(rbox((0.06, 0.6, 0.06), 0.2, mat="Metal"), (s * 0.675, 1.2, 0.95))
        m.add(rbox((0.1, 0.4, 1.5), 0.15, mat="Paint"), (s * 0.7, 0.7, 0.2))            # joues du plateau
        for z in (-0.82, 0.86): m.add(cylinder(0.05, 0.05, 0.03, seg=12, mat="Rubber"), (s * 0.65, 0.0, z))
    m.add(rbox((1.5, 0.4, 0.05), 0.15, mat="Paint"), (0, 0.7, -0.925))
    m.add(rbox((1.4, 0.6, 0.05), 0.12, mat="Paint"), (0, 1.2, 0.975))
    led(m, (1.2, 0.02, 0.02), (0, 0.525, -0.9), "NeonYellow")
    m.add(rbox((1.5, 0.25, 1.6), 0.12, mat="Paint"), (0, 1.62, 0.2))
    m.add(rbox((1.54, 0.4, 1.04), 0.1, mat="Paint"), (0, 1.95, 0.45))
    m.add(quad(1.4, 0.34, "SignPusher", flip=True), (0, 1.95, 0.45 - 0.52 - 0.004))
    m.add(quad(1.4, 0.34, "SignPusher"), (0, 1.95, 0.45 + 0.52 + 0.004))
    for s in (-1, 1): m.add(quad(0.9, 0.34, "SignPusher"), (s * 0.775, 1.95, 0.45), R=rot(0, s * 90, 0))
    m.add(box((1.62, 0.03, 1.12), "Metal"), (0, 2.16, 0.45))
    # pupitre
    m.add(rbox((1.0, 0.6, 0.4), 0.1, mat="Paint"), (0, 0.3, -1.15))
    m.add(rbox((1.02, 0.05, 0.42), 0.15, mat="Paint"), (0, 0.625, -1.15))
    m.add(box((0.8, 0.012, 0.28), "Metal"), (0, 0.653, -1.13))
    m.add(quad(0.34, 0.17, "DecalPusher"), (0, 0.661, -1.24), R=rot(0, 180, 0) @ rot(-90, 0, 0))
    led(m, (0.9, 0.015, 0.015), (0, 0.5, -1.355), "NeonCyan")
    m.add(rbox((0.22, 0.28, 0.04), 0.2, mat="Metal"), (0, 0.33, -1.36))
    m.add(box((0.05, 0.1, 0.01), "Dark"), (0, 0.4, -1.382)); m.add(box((0.14, 0.07, 0.01), "Dark"), (0, 0.24, -1.382))
    m.add(torus(0.05, 0.008, seg=18, tube=6, mat="NeonYellow"), (0, 0.4, -1.383), R=rot(90, 0, 0))
    # visserie, charnières et rebords
    for s in (-1, 1):
        for y in (0.95, 1.2, 1.45):
            m.add(cylinder(0.014, 0.014, 0.008, seg=8, mat="Metal"), (s * 0.7, y, -0.604), R=rot(90, 0, 0))      # vis des montants avant
            m.add(cylinder(0.02, 0.02, 0.1, seg=10, mat="Metal"), (s * 0.71, y, 0.955))                          # charnières des portes
        for x, z in ((0.46, -1.0), (0.46, -1.3)):
            m.add(cylinder(0.014, 0.014, 0.006, seg=8, mat="Metal"), (s * x, 0.657, z))                          # vis du pupitre
        m.add(rbox((0.05, 0.03, 0.4), 0.3, mat="Metal"), (s * 0.78, 0.52, -0.2))                                 # rebords latéraux du bac
    m.add(rbox((1.0, 0.025, 0.05), 0.4, mat="Metal"), (0, 0.515, -0.89))                                         # lèvre de la zone de récupération
    m.add(rbox((1.1, 0.03, 0.14), 0.3, mat="Dark"), (0, 1.505, 0.78))                                            # rail d'insertion (sous le plafond)
    for x in (-0.5, -0.25, 0.0, 0.25, 0.5):
        m.add(cylinder(0.012, 0.012, 0.006, seg=8, mat="Metal"), (x, 1.746, -0.54), R=rot(0, 0, 0))             # vis du plafond
    return m.finish(keep=True)


@model
def pusher_plate():
    m = Model("pusher_plate", uv=1.0)
    m.add(rbox((1.28, 0.12, 0.62), 0.12, mat="Metal"))
    for i in range(9):
        m.add(box((1.2, 0.01, 0.018), "Metal"), (0, 0.062, -0.26 + i * 0.065))               # nervures (les pièces glissent vers l'avant)
    m.add(rbox((1.3, 0.06, 0.03), 0.3, mat="NeonPink"), (0, 0.0, -0.315))                    # bord lumineux avant
    return m.finish(keep=True)


@model
def pusher_aim():
    m = Model("pusher_aim", uv=1.0)
    m.add(cylinder(0.075, 0.0, 0.1, seg=4, mat="NeonYellow"), (0, -0.05, 0), R=rot(180, 45, 0), )
    return m.finish(keep=True)


# ------------------------------------------------------------------ PIÈCES (cylindre unité : diamètre 1, hauteur 2)
@model
def coin_unit():
    m = Model("coin_unit", uv=1.0)
    prof = [(0.0, 0.72), (0.34, 0.72), (0.37, 0.86), (0.44, 0.9), (0.5, 0.62), (0.5, -0.62), (0.44, -0.9), (0.37, -0.86), (0.34, -0.72), (0.0, -0.72)]
    m.add(lathe(prof[4:6], seg=28, mat="Rim", tile=2.0))
    for sgn in (1, -1):
        pts = [(r, sgn * y) for r, y in prof[:5]]
        m.add(lathe(pts[1:], seg=28, mat="Rim", tile=2.0, smooth=True))
        # face : disque plan avec UV planaires (image de la pièce)
        ps = np.linspace(0, 2 * math.pi, 29)
        c = np.array([[0, sgn * 0.72, 0]] + [[0.34 * math.cos(p), sgn * 0.72, 0.34 * math.sin(p)] for p in ps[:-1]])
        UV = np.column_stack([0.5 + c[:, 0] / 0.68, 0.5 + c[:, 2] / 0.68 * sgn])
        F = np.array([[0, 1 + k, 1 + (k + 1) % 28] for k in range(28)])
        m.add(Part(c, np.tile([0, sgn, 0], (len(c), 1)), F, "Face", UV))
    return m.finish(keep=True)


# ------------------------------------------------------------------ PERSONNAGE (pivots aux articulations)
@model
def player_torso():
    m = Model("player_torso", uv=2.0)
    m.add(rbox((0.44, 0.58, 0.25), 0.55, seg=20, rings=12, mat="Shirt"), (0, 1.22, 0))
    m.add(rbox((0.46, 0.1, 0.27), 0.4, mat="Jacket"), (0, 0.98, 0))                      # ceinture
    m.add(cylinder(0.065, 0.075, 0.09, seg=14, mat="Skin"), (0, 1.5, 0))                  # cou
    m.add(box((0.14, 0.14, 0.02), "Accent"), (0, 1.26, 0.13))                             # logo sur le torse
    m.add(rbox((0.34, 0.4, 0.14), 0.5, mat="Jacket"), (0, 1.22, -0.18))                   # sac à jetons
    for s in (-1, 1): m.add(box((0.05, 0.5, 0.03), "Jacket"), (s * 0.11, 1.24, 0.12))     # bretelles
    return m.finish(keep=True)


@model
def player_head():
    m = Model("player_head", uv=2.0)
    m.add(superellipsoid((0.105, 0.125, 0.11), 0.9, seg=22, rings=14, mat="Skin"))
    m.add(superellipsoid((0.115, 0.1, 0.12), 0.9, seg=22, rings=10, t1=math.pi / 2 + 0.25, mat="Hair"), (0, 0.035, -0.01))
    m.add(superellipsoid((0.075, 0.05, 0.06), 0.8, seg=12, rings=8, mat="Hair"), (0, 0.06, -0.09))
    for s in (-1, 1):
        m.add(sphere(0.02, seg=10, rings=7, mat="Light"), (s * 0.04, 0.012, 0.095))
        m.add(sphere(0.011, seg=8, rings=6, mat="Dark"), (s * 0.04, 0.012, 0.113))
        m.add(superellipsoid((0.018, 0.03, 0.012), 1.0, seg=8, rings=6, mat="Skin"), (s * 0.108, -0.005, 0))   # oreilles
        m.add(box((0.045, 0.008, 0.01), "Hair"), (s * 0.04, 0.045, 0.108))                                   # sourcils
    m.add(superellipsoid((0.016, 0.022, 0.022), 1.0, seg=8, rings=6, mat="Skin"), (0, -0.015, 0.118))        # nez
    m.add(box((0.05, 0.008, 0.008), "Mouth"), (0, -0.065, 0.108))
    return m.finish(keep=True)


@model
def player_arm():
    m = Model("player_arm", uv=2.0)
    m.add(sphere(0.062, seg=14, rings=9, mat="Shirt"), (0, 0, 0))
    m.add(tube([(0, 0, 0), (0, -0.14, 0), (0, -0.26, 0)], [0.052, 0.05, 0.046], seg=12, mat="Shirt"))
    m.add(tube([(0, -0.26, 0), (0, -0.4, 0), (0, -0.52, 0)], [0.04, 0.036, 0.03], seg=12, mat="Skin"))
    m.add(superellipsoid((0.034, 0.05, 0.026), 1.0, seg=12, rings=8, mat="Skin"), (0, -0.57, 0))
    return m.finish(keep=True)


@model
def player_leg():
    m = Model("player_leg", uv=2.0)
    m.add(tube([(0, 0, 0), (0, -0.2, 0), (0, -0.42, 0)], [0.085, 0.075, 0.06], seg=14, mat="Pants"))
    m.add(tube([(0, -0.42, 0), (0, -0.62, 0), (0, -0.82, 0)], [0.058, 0.052, 0.05], seg=14, mat="Pants"))
    m.add(rbox((0.11, 0.07, 0.24), 0.5, mat="Sneaker"), (0, -0.865, 0.04))
    m.add(rbox((0.115, 0.03, 0.25), 0.3, mat="Light"), (0, -0.9, 0.04))
    m.add(box((0.06, 0.02, 0.02), "Accent"), (0, -0.85, 0.15))
    return m.finish(keep=True)


if __name__ == "__main__":
    pass
