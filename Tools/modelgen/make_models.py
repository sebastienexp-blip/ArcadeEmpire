"""Génère tous les modèles du jeu (FBX) dans Assets/_Project/Art/Models. Usage : python3 make_models.py"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from fbxlib import *

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Art", "Models")
os.makedirs(OUT, exist_ok=True)
MODELS = {}


def model(fn):
    MODELS[fn.__name__] = fn
    return fn


def eyes(m, y, z, dx, r=0.04):
    for s in (-1, 1): m.add(sphere(r, seg=8, rings=6, mat="Dark"), (s * dx, y, z))


@model
def prize_bunny():
    m = Model("prize_bunny")
    m.add(superellipsoid((0.34, 0.36, 0.3), 0.85))                      # corps
    m.add(superellipsoid((0.28, 0.26, 0.26), 0.9), (0, 0.55, 0.03))     # tête
    for s in (-1, 1):
        m.add(superellipsoid((0.08, 0.3, 0.05), 1.0), (s * 0.12, 0.98, 0), rot(0, 0, -s * 8))   # longues oreilles
        m.add(superellipsoid((0.045, 0.22, 0.03), 1.0, mat="Light"), (s * 0.12, 0.98, 0.04), rot(0, 0, -s * 8))
        m.add(superellipsoid((0.1, 0.17, 0.1), 1.0), (s * 0.36, 0.05, 0.08), rot(0, 0, -s * 20))   # bras
        m.add(superellipsoid((0.13, 0.1, 0.17), 1.0), (s * 0.18, -0.3, 0.12))                         # pieds
    m.add(sphere(0.12, seg=10, rings=7, mat="Light"), (0, -0.05, -0.3))   # queue pompon
    m.add(superellipsoid((0.1, 0.075, 0.08), 1.0, mat="Light"), (0, 0.49, 0.26))
    m.add(sphere(0.035, seg=8, rings=6, mat="Dark"), (0, 0.52, 0.33))
    eyes(m, 0.6, 0.24, 0.1)
    return m.finish()


@model
def prize_bear():
    m = Model("prize_bear")
    m.add(superellipsoid((0.36, 0.38, 0.32), 0.9))
    m.add(superellipsoid((0.31, 0.28, 0.28), 0.9), (0, 0.5, 0.02))
    for s in (-1, 1):
        m.add(sphere(0.115, seg=10, rings=7), (s * 0.23, 0.76, 0))
        m.add(sphere(0.065, seg=8, rings=6, mat="Light"), (s * 0.23, 0.76, 0.05))
        m.add(superellipsoid((0.11, 0.2, 0.11), 1.0), (s * 0.4, 0.05, 0.05), rot(0, 0, -s * 25))
        m.add(superellipsoid((0.14, 0.12, 0.17), 1.0), (s * 0.2, -0.3, 0.12))
    m.add(superellipsoid((0.13, 0.095, 0.09), 1.0, mat="Light"), (0, 0.43, 0.27))
    m.add(sphere(0.045, seg=8, rings=6, mat="Dark"), (0, 0.47, 0.35))
    eyes(m, 0.57, 0.25, 0.11)
    return m.finish()


@model
def prize_robot():
    m = Model("prize_robot")
    m.add(superellipsoid((0.34, 0.32, 0.26), 0.3))                       # torse
    m.add(superellipsoid((0.28, 0.22, 0.24), 0.3), (0, 0.52, 0))         # tête
    m.add(box((0.3, 0.07, 0.02), "Accent"), (0, 0.0, 0.265))             # plaque sur le torse
    for s in (-1, 1):
        m.add(sphere(0.055, seg=8, rings=6, mat="Accent"), (s * 0.1, 0.55, 0.235))      # yeux lumineux
        m.add(cylinder(0.06, 0.06, 0.28, seg=10), (s * 0.43, -0.22, 0), rot(0, 0, s * 90 + 0) , )  # bras
        m.add(box((0.14, 0.2, 0.18), "Dark"), (s * 0.15, -0.42, 0))      # jambes
    m.add(cylinder(0.02, 0.02, 0.18, seg=8, mat="Dark"), (0, 0.72, 0))   # antenne
    m.add(sphere(0.06, seg=8, rings=6, mat="Accent"), (0, 0.94, 0))
    return m.finish()


@model
def prize_capsule():
    m = Model("prize_capsule")
    m.add(superellipsoid((0.42, 0.5, 0.42), 1.0, t0=0, t1=math.pi / 2, mat="Main"), (0, 0.02, 0))
    m.add(superellipsoid((0.42, 0.5, 0.42), 1.0, t0=math.pi / 2, t1=math.pi, mat="Light"), (0, 0.02, 0))
    m.add(torus(0.42, 0.035, seg=18, tube=6, mat="Dark"), (0, 0.02, 0))
    m.add(sphere(0.09, seg=8, rings=6, mat="Accent"), (0, 0.5, 0))
    return m.finish()


@model
def prize_cat_gold():
    m = Model("prize_cat_gold")
    m.add(box((0.7, 0.16, 0.55), "Dark"), (0, -0.52, 0))                 # socle
    m.add(superellipsoid((0.3, 0.34, 0.26), 0.85), (0, -0.2, 0))         # corps
    m.add(superellipsoid((0.27, 0.23, 0.23), 0.85), (0, 0.27, 0.02))     # tête
    for s in (-1, 1):
        m.add(cylinder(0.11, 0.0, 0.2, seg=4), (s * 0.15, 0.4, 0), rot(0, 45, -s * 12))          # oreilles
        m.add(sphere(0.035, seg=8, rings=6, mat="Dark"), (s * 0.09, 0.3, 0.21))
    m.add(superellipsoid((0.07, 0.17, 0.07), 1.0), (0.3, 0.0, 0.1), rot(0, 0, 18))               # patte levée
    m.add(superellipsoid((0.06, 0.2, 0.06), 1.0), (-0.1, -0.35, 0.18), rot(0, 0, 0))
    m.add(torus(0.22, 0.04, seg=10, tube=6, a0=0.3, a1=2.6, mat="Main"), (-0.26, -0.3, -0.05), rot(90, 0, 0))   # queue
    m.add(sphere(0.07, seg=8, rings=6, mat="Accent"), (0, 0.04, 0.24))                           # médaillon
    return m.finish()


@model
def decor_plant():
    m = Model("decor_plant")
    m.add(cylinder(0.2, 0.28, 0.42, seg=14, mat="Pot"), (0, 0, 0))
    m.add(cylinder(0.27, 0.27, 0.02, seg=14, mat="Soil"), (0, 0.425, 0))
    for k in range(9):
        a = k * 40
        tilt = 25 + (k % 3) * 14
        m.add(superellipsoid((0.07, 0.34, 0.025), 1.0), (0, 0.5, 0), rot(0, a, 0) @ rot(0, 0, tilt), )
    return m.finish(ground=True)


@model
def decor_pouf():
    m = Model("decor_pouf")
    m.add(superellipsoid((0.45, 0.24, 0.45), 0.55), (0, 0.24, 0))
    m.add(sphere(0.06, seg=8, rings=6, mat="Light"), (0, 0.47, 0))
    m.add(torus(0.4, 0.025, seg=18, tube=6, mat="Dark"), (0, 0.26, 0))
    return m.finish(ground=True)


@model
def decor_neon_pillar():
    m = Model("decor_neon_pillar")
    m.add(box((0.5, 0.12, 0.5), "Dark"), (0, 0.06, 0))
    m.add(box((0.32, 1.9, 0.32), "Dark"), (0, 1.07, 0))
    m.add(box((0.5, 0.12, 0.5), "Dark"), (0, 2.08, 0))
    m.add(box((0.08, 1.6, 0.04), "Accent"), (0, 1.07, -0.17))
    m.add(box((0.08, 1.6, 0.04), "Accent"), (0, 1.07, 0.17))
    return m.finish(ground=True)


@model
def decor_trophy():
    m = Model("decor_trophy")
    m.add(box((0.6, 0.2, 0.6), "Dark"), (0, 0.1, 0))
    m.add(cylinder(0.18, 0.1, 0.1, seg=12, mat="Dark"), (0, 0.2, 0))
    m.add(cylinder(0.05, 0.05, 0.3, seg=10, mat="Main"), (0, 0.3, 0))
    m.add(superellipsoid((0.26, 0.3, 0.26), 1.0, t0=math.pi / 2, t1=math.pi, mat="Main"), (0, 0.9, 0), S=(1, 1, 1))
    m.add(cylinder(0.26, 0.28, 0.12, seg=14, mat="Main"), (0, 0.9, 0))
    for s in (-1, 1): m.add(torus(0.12, 0.025, seg=10, tube=6, a0=-1.6, a1=1.6, mat="Main"), (s * 0.3, 0.88, 0), rot(90, 0, 0) @ rot(0, 0, 90 if s > 0 else -90))
    m.add(sphere(0.09, seg=8, rings=6, mat="Accent"), (0, 1.12, 0))
    return m.finish(ground=True)


if __name__ == "__main__":
    for name, fn in MODELS.items():
        m = fn()
        path = os.path.join(OUT, name + ".fbx")
        write_fbx(m, path)
        nv, nt, nm = read_fbx_counts(path)
        print("%-18s %5d sommets %5d triangles %d matériaux  taille %s" % (name, nv, nt, nm, np.round(m.size, 2)))
