"""Meubles de la maison (lots à gagner) + éléments de la vitrine (cadenas, coffret). Slots de matériaux :
Main (couleur principale du meuble), Second (bois/tissu secondaire), Light (coussins, plan clair), Dark (pieds, poignées),
Metal, Accent (néon/écran lumineux), Glass. Front des meubles vers -Z, posés au sol (y = 0)."""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fbxlib import Model, rbox, box, cylinder, sphere, torus, lathe, tube, rot, quad
from make_models import model


def legs(m, w, d, h, r=0.025, mat="Dark", inset=0.05):
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.add(cylinder(r, r * 0.8, h, seg=10, mat=mat), (sx * (w / 2 - inset), 0, sz * (d / 2 - inset)))


@model
def home_sofa():
    m = Model("home_sofa", uv=1.5)
    m.add(rbox((2.0, 0.42, 0.9), e=0.25), (0, 0.33, 0))                      # assise
    m.add(rbox((2.0, 0.55, 0.25), e=0.25), (0, 0.7, 0.33))                   # dossier
    for s in (-1, 1): m.add(rbox((0.24, 0.5, 0.9), e=0.3), (s * 0.94, 0.5, 0))   # accoudoirs
    for x in (-0.5, 0.5):
        m.add(rbox((0.86, 0.16, 0.68), e=0.4, mat="Light"), (x, 0.6, -0.06))      # coussins d'assise
        m.add(rbox((0.8, 0.42, 0.16), e=0.45, mat="Light"), (x, 0.88, 0.2))       # coussins de dos
    m.add(rbox((0.36, 0.36, 0.12), e=0.5, mat="Second"), (-0.68, 0.86, 0.1), R=rot(-10, 20, 8))
    legs(m, 1.9, 0.8, 0.12, 0.03)
    return m.finish(ground=True)


@model
def home_coffee_table():
    m = Model("home_coffee_table", uv=1.5)
    m.add(rbox((1.1, 0.06, 0.6), e=0.15, mat="Second"), (0, 0.42, 0))
    m.add(rbox((1.0, 0.03, 0.5), e=0.15, mat="Second"), (0, 0.14, 0))
    legs(m, 1.05, 0.55, 0.42, 0.022, "Metal", 0.04)
    m.add(cylinder(0.1, 0.08, 0.05, seg=16, mat="Light"), (0.25, 0.45, 0.05))
    return m.finish(ground=True)


@model
def home_tv():
    m = Model("home_tv", uv=1.0)
    m.add(rbox((1.8, 0.5, 0.45), e=0.08, mat="Second"), (0, 0.25, 0))               # meuble TV
    for x in (-0.45, 0.45): m.add(rbox((0.8, 0.36, 0.01), e=0.2, mat="Dark"), (x, 0.25, -0.226))
    m.add(rbox((1.5, 0.88, 0.05), e=0.05, mat="Dark"), (0, 1.05, 0.05))              # écran
    m.add(quad(1.42, 0.8, "Accent", flip=True), (0, 1.05, 0.024))
    m.add(rbox((0.4, 0.03, 0.22), e=0.3, mat="Dark"), (0, 0.515, 0.05))
    m.add(rbox((0.06, 0.08, 0.04), e=0.3, mat="Dark"), (0, 0.57, 0.05))
    m.add(rbox((0.3, 0.06, 0.2), e=0.3, mat="Metal"), (0.6, 0.53, 0.0))              # console de salon posée
    return m.finish(ground=True)


@model
def home_rug():
    m = Model("home_rug", uv=2.0)
    m.add(rbox((2.4, 0.02, 1.7), e=0.1), (0, 0.01, 0))
    m.add(rbox((2.0, 0.022, 1.3), e=0.1, mat="Light"), (0, 0.012, 0))
    m.add(rbox((1.6, 0.024, 0.9), e=0.1, mat="Second"), (0, 0.014, 0))
    return m.finish(ground=True)


@model
def home_lamp():
    m = Model("home_lamp", uv=1.0)
    m.add(cylinder(0.18, 0.2, 0.04, seg=20, mat="Dark"), (0, 0, 0))
    m.add(cylinder(0.015, 0.015, 1.45, seg=10, mat="Metal"), (0, 0.04, 0))
    m.add(lathe([(0.12, 1.42), (0.24, 1.62), (0.24, 1.64), (0.12, 1.62)], seg=24, mat="Main"), (0, 0, 0))
    m.add(sphere(0.07, seg=12, rings=8, mat="Accent"), (0, 1.52, 0))
    return m.finish(ground=True)


@model
def home_bookshelf():
    m = Model("home_bookshelf", uv=1.5)
    W, H, D = 1.0, 1.9, 0.35
    for s in (-1, 1): m.add(box((0.03, H, D), "Second"), (s * W / 2, H / 2, 0))
    m.add(box((W, H, 0.02), "Second"), (0, H / 2, D / 2))
    for k in range(5): m.add(box((W, 0.03, D), "Second"), (0, 0.02 + k * 0.46, 0))
    import random
    rnd = random.Random(3)
    for k in range(4):
        x = -W / 2 + 0.06
        while x < W / 2 - 0.1:
            w = rnd.uniform(0.03, 0.07); h = rnd.uniform(0.25, 0.38)
            m.add(box((w, h, 0.24), ("Main", "Light", "Accent", "Dark")[rnd.randrange(4)]), (x + w / 2, 0.035 + k * 0.46 + h / 2, -0.02))
            x += w + 0.005
    return m.finish(ground=True)


@model
def home_kitchen():
    m = Model("home_kitchen", uv=1.5)
    m.add(rbox((2.4, 0.86, 0.6), e=0.04, mat="Main"), (0, 0.43, 0))
    m.add(rbox((2.44, 0.04, 0.64), e=0.1, mat="Light"), (0, 0.88, 0))
    for k in range(4):
        x = -0.9 + k * 0.6
        m.add(rbox((0.56, 0.7, 0.01), e=0.1, mat="Main"), (x, 0.45, -0.305))
        m.add(rbox((0.2, 0.02, 0.02), e=0.4, mat="Metal"), (x, 0.74, -0.32))
    m.add(rbox((0.5, 0.04, 0.4), e=0.2, mat="Metal"), (-0.6, 0.895, 0.0))            # évier
    m.add(tube([(-0.6, 0.9, 0.22), (-0.6, 1.15, 0.22), (-0.6, 1.2, 0.12), (-0.6, 1.12, 0.05)], 0.015, mat="Metal"))
    for x in (0.55, 0.85):
        for z in (-0.12, 0.12): m.add(cylinder(0.09, 0.09, 0.01, seg=16, mat="Dark"), (x, 0.9, z))   # plaques de cuisson
    m.add(rbox((2.4, 0.7, 0.35), e=0.04, mat="Main"), (0, 1.95, 0.12))              # meubles hauts
    return m.finish(ground=True)


@model
def home_fridge():
    m = Model("home_fridge", uv=1.0)
    m.add(rbox((0.7, 1.85, 0.68), e=0.06, mat="Main"), (0, 0.925, 0))
    m.add(box((0.7, 0.01, 0.01), "Dark"), (0, 1.25, -0.345))
    for y0, y1 in ((0.3, 1.0), (1.4, 1.75)): m.add(tube([(0.28, y0, -0.36), (0.28, y1, -0.36)], 0.012, mat="Metal"))
    return m.finish(ground=True)


@model
def home_table():
    m = Model("home_table", uv=1.5)
    m.add(rbox((1.4, 0.05, 0.85), e=0.12, mat="Second"), (0, 0.74, 0))
    legs(m, 1.3, 0.75, 0.72, 0.03, "Second", 0.07)
    return m.finish(ground=True)


@model
def home_chair():
    m = Model("home_chair", uv=1.5)
    m.add(rbox((0.44, 0.05, 0.44), e=0.2, mat="Second"), (0, 0.45, 0))
    m.add(rbox((0.44, 0.45, 0.04), e=0.2, mat="Second"), (0, 0.7, 0.2))
    m.add(rbox((0.4, 0.06, 0.38), e=0.5, mat="Light"), (0, 0.49, -0.01))
    legs(m, 0.42, 0.42, 0.45, 0.018, "Dark", 0.03)
    return m.finish(ground=True)


@model
def home_bed():
    m = Model("home_bed", uv=1.5)
    m.add(rbox((1.5, 0.3, 2.1), e=0.08, mat="Second"), (0, 0.2, 0))                    # sommier
    m.add(rbox((1.42, 0.22, 2.0), e=0.25, mat="Light"), (0, 0.45, -0.02))              # matelas
    m.add(rbox((1.46, 0.08, 1.4), e=0.35, mat="Main"), (0, 0.58, -0.3))                # couette
    m.add(rbox((1.5, 0.95, 0.08), e=0.15, mat="Second"), (0, 0.6, 1.05))               # tête de lit
    for x in (-0.35, 0.35): m.add(rbox((0.55, 0.14, 0.32), e=0.5, mat="Light"), (x, 0.62, 0.78))
    return m.finish(ground=True)


@model
def home_nightstand():
    m = Model("home_nightstand", uv=1.0)
    m.add(rbox((0.45, 0.5, 0.4), e=0.08, mat="Second"), (0, 0.3, 0))
    m.add(rbox((0.38, 0.18, 0.01), e=0.2, mat="Second"), (0, 0.38, -0.205))
    m.add(sphere(0.02, seg=8, rings=6, mat="Metal"), (0, 0.38, -0.215))
    legs(m, 0.4, 0.35, 0.06, 0.015, "Dark", 0.03)
    m.add(cylinder(0.06, 0.08, 0.04, seg=14, mat="Dark"), (0.1, 0.55, 0))
    m.add(lathe([(0.06, 0.62), (0.12, 0.76), (0.12, 0.77), (0.06, 0.76)], seg=18, mat="Main"), (0.1, 0, 0))
    m.add(cylinder(0.01, 0.01, 0.1, seg=8, mat="Metal"), (0.1, 0.57, 0))
    return m.finish(ground=True)


@model
def home_wardrobe():
    m = Model("home_wardrobe", uv=1.5)
    m.add(rbox((1.2, 2.0, 0.6), e=0.04, mat="Main"), (0, 1.0, 0))
    m.add(box((0.008, 1.9, 0.01), "Dark"), (0, 1.0, -0.302))
    for s in (-1, 1): m.add(tube([(s * 0.06, 0.85, -0.32), (s * 0.06, 1.2, -0.32)], 0.012, mat="Metal"))
    return m.finish(ground=True)


@model
def home_desk():
    m = Model("home_desk", uv=1.5)
    m.add(rbox((1.4, 0.05, 0.7), e=0.1, mat="Main"), (0, 0.74, 0))
    for s in (-1, 1): m.add(rbox((0.06, 0.72, 0.62), e=0.2, mat="Dark"), (s * 0.64, 0.36, 0))
    m.add(rbox((0.6, 0.36, 0.03), e=0.1, mat="Dark"), (0, 1.0, 0.2))                   # écran
    m.add(quad(0.56, 0.32, "Accent", flip=True), (0, 1.0, 0.184))
    m.add(rbox((0.44, 0.02, 0.15), e=0.3, mat="Dark"), (0, 0.775, -0.15))              # clavier RGB
    m.add(box((0.42, 0.004, 0.13), "Accent"), (0, 0.787, -0.15))
    m.add(rbox((0.2, 0.44, 0.42), e=0.1, mat="Dark"), (0.5, 0.99, 0.08))               # tour PC
    m.add(box((0.005, 0.38, 0.34), "Accent"), (0.399, 0.99, 0.08))
    return m.finish(ground=True)


@model
def home_gaming_chair():
    m = Model("home_gaming_chair", uv=1.5)
    m.add(cylinder(0.3, 0.3, 0.03, seg=5, mat="Dark"), (0, 0.06, 0))
    m.add(cylinder(0.035, 0.035, 0.35, seg=10, mat="Metal"), (0, 0.08, 0))
    m.add(rbox((0.52, 0.1, 0.5), e=0.3), (0, 0.48, 0))
    m.add(rbox((0.5, 0.8, 0.12), e=0.3), (0, 0.95, 0.24), R=rot(-8, 0, 0))
    m.add(rbox((0.3, 0.6, 0.13), e=0.35, mat="Accent"), (0, 0.95, 0.235), R=rot(-8, 0, 0))
    for s in (-1, 1): m.add(rbox((0.06, 0.06, 0.32), e=0.3, mat="Dark"), (s * 0.28, 0.66, 0.02))
    return m.finish(ground=True)


@model
def home_arcade():
    """Borne d'arcade de salon (lot premium)."""
    m = Model("home_arcade", uv=1.0)
    m.add(rbox((0.7, 1.75, 0.75), e=0.04, mat="Main"), (0, 0.875, 0.05))
    m.add(rbox((0.62, 0.48, 0.05), e=0.05, mat="Dark"), (0, 1.32, -0.3), R=rot(-12, 0, 0))
    m.add(quad(0.56, 0.42, "Accent", flip=True), (0, 1.32, -0.33), R=rot(-12, 0, 0))
    m.add(rbox((0.72, 0.08, 0.35), e=0.2, mat="Dark"), (0, 0.98, -0.42), R=rot(10, 0, 0))   # panneau de commande
    m.add(cylinder(0.01, 0.01, 0.07, seg=8, mat="Metal"), (-0.15, 1.0, -0.45))
    m.add(sphere(0.03, seg=10, rings=7, mat="Accent"), (-0.15, 1.08, -0.45))
    for i, x in enumerate((0.05, 0.13, 0.21)): m.add(cylinder(0.025, 0.025, 0.02, seg=12, mat="Second" if i % 2 else "Accent"), (x, 1.02, -0.45))
    m.add(rbox((0.72, 0.2, 0.3), e=0.1, mat="Accent"), (0, 1.84, -0.18))               # fronton lumineux
    return m.finish(ground=True)


@model
def home_plant():
    m = Model("home_plant", uv=1.5)
    m.add(cylinder(0.2, 0.15, 0.42, seg=16, mat="Light"), (0, 0, 0))
    m.add(cylinder(0.19, 0.19, 0.02, seg=16, mat="Dark"), (0, 0.4, 0))
    for k in range(11):
        a = k * 33; tilt = 18 + (k % 4) * 12
        m.add(sphere((0.09, 0.42, 0.03), seg=10, rings=7, mat="Main"), (0, 0.45, 0), rot(0, a, 0) @ rot(0, 0, tilt))
    return m.finish(ground=True)


@model
def home_neon():
    m = Model("home_neon", uv=1.0)
    m.add(rbox((1.2, 0.6, 0.03), e=0.1, mat="Dark"), (0, 0.3, 0))
    pts = [(-0.45, 0.15, -0.03), (-0.45, 0.45, -0.03), (-0.25, 0.15, -0.03), (-0.25, 0.45, -0.03)]
    m.add(tube(pts, 0.015, mat="Accent"))
    m.add(torus(0.13, 0.015, seg=24, tube=6, mat="Second"), (0.05, 0.3, -0.03), R=rot(90, 0, 0))
    m.add(tube([(0.3, 0.15, -0.03), (0.42, 0.45, -0.03), (0.5, 0.15, -0.03)], 0.015, mat="Accent"))
    return m.finish(ground=True)


@model
def home_shelf():
    """Étagère murale d'exposition des lots (3 niveaux)."""
    m = Model("home_shelf", uv=1.5)
    for k in range(3):
        m.add(rbox((1.6, 0.04, 0.4), e=0.1, mat="Second"), (0, 0.6 + k * 0.55, 0))
        m.add(box((1.56, 0.012, 0.012), "Accent"), (0, 0.585 + k * 0.55, -0.195))
    for s in (-1, 1): m.add(rbox((0.04, 1.8, 0.4), e=0.1, mat="Second"), (s * 0.8, 0.9, 0))
    return m.finish(ground=True)


@model
def padlock():
    m = Model("padlock", uv=1.0)
    m.add(rbox((0.5, 0.42, 0.18), e=0.2, mat="Main"), (0, 0, 0))
    m.add(torus(0.17, 0.04, seg=20, tube=8, a0=0, a1=math.pi, mat="Metal"), (0, 0.2, 0), R=rot(90, 0, 0))
    m.add(cylinder(0.04, 0.04, 0.02, seg=12, mat="Dark"), (0, -0.02, -0.09), R=rot(90, 0, 0))
    m.add(box((0.02, 0.07, 0.01), "Dark"), (0, -0.07, -0.095))
    return m.finish()


@model
def furniture_kit():
    """Carton de meuble en kit (affiché dans la vitrine)."""
    m = Model("furniture_kit", uv=1.0)
    m.add(rbox((1.0, 0.7, 0.3), e=0.03, mat="Main"))
    m.add(rbox((0.6, 0.4, 0.01), e=0.1, mat="Light"), (-0.1, 0.03, -0.152))
    m.add(rbox((1.02, 0.06, 0.31), e=0.1, mat="Accent"), (0, 0.28, 0))
    return m.finish()
