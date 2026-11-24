"""Personnage du joueur (v3) : surfaces lisses (champ de distance + marching cubes), style personnage de jeu.
Membres articulés en deux segments (bras + avant-bras, cuisse + tibia) pour plier coudes et genoux à l'animation.
Pivots : torse en coordonnées monde ; tête au cou (1,64) ; bras à l'épaule, avant-bras au coude (-0,27) ;
cuisse à la hanche, tibia au genou (-0,42)."""
import os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_plush import Plush, _rot_to
from fbxlib import tube, sphere
from make_models import model


def human_eye(p, origin, direction, r=0.026):
    """Œil : blanc légèrement aplati, iris coloré, pupille, double reflet."""
    s, n = p.surface(origin, direction)
    R = _rot_to(n)
    c = s - n * r * 0.55
    p.solid(sphere((r, r * 0.92, r * 0.7), seg=16, rings=10, mat="Tag"), c, R)
    p.solid(sphere((r * 0.62, r * 0.66, r * 0.2), seg=16, rings=8, mat="Iris"), c + n * r * 0.6, R)
    p.solid(sphere((r * 0.32, r * 0.34, r * 0.12), seg=12, rings=6, mat="Eye"), c + n * r * 0.7, R)
    up = np.array([0, 1.0, 0]); side = np.cross(n, up); side /= np.linalg.norm(side) + 1e-9
    p.solid(sphere(r * 0.16, seg=8, rings=6, mat="Tag"), c + n * r * 0.78 + up * r * 0.25 + side * r * 0.2, None)
    p.solid(sphere(r * 0.08, seg=6, rings=4, mat="Tag"), c + n * r * 0.76 - up * r * 0.25 - side * r * 0.2, None)


@model
def player_torso():
    p = Plush("player_torso", res=60)
    p.ell((0, 1.2, 0.0), (0.19, 0.26, 0.125), mat="Shirt", k=0.06)              # buste
    p.ell((0, 1.36, 0.01), (0.205, 0.13, 0.13), mat="Shirt", k=0.06)            # poitrine
    for s in (-1, 1):
        p.ell((s * 0.2, 1.43, 0), (0.08, 0.068, 0.08), mat="Shirt", k=0.05)     # épaules
    p.ell((0, 1.0, 0), (0.18, 0.065, 0.12), mat="Jacket", k=0.03)               # bas côtelé du sweat
    p.ell((0, 0.94, 0), (0.165, 0.065, 0.11), mat="Pants", k=0.03)              # haut du jean
    p.ell((0, 1.08, 0.108), (0.12, 0.065, 0.03), mat="Jacket", k=0.012)         # poche kangourou
    p.ell((0, 1.47, -0.09), (0.16, 0.1, 0.07), mat="Jacket", k=0.04)            # capuche dans le dos
    p.ell((0, 1.36, -0.13), (0.12, 0.09, 0.03), mat="Jacket", k=0.03)
    p.cone((0, 1.46, 0.0), (0, 1.6, 0.005), 0.056, 0.05, mat="Skin", k=0.02)    # cou
    m = p.build(uv=3.0, keep=True)
    for s in (-1, 1):   # cordons de capuche
        m.add(tube([(s * 0.05, 1.47, 0.12), (s * 0.055, 1.38, 0.138), (s * 0.06, 1.3, 0.138)], 0.007, seg=6, mat="Light"))
        m.add(sphere(0.012, seg=8, rings=6, mat="Light"), (s * 0.06, 1.29, 0.138))
    m.add(sphere((0.045, 0.045, 0.008), seg=14, rings=6, mat="Accent"), (0.085, 1.33, 0.133))   # écusson
    return m


@model
def player_head():
    p = Plush("player_head", res=64)
    p.ell((0, 0.02, 0), (0.1, 0.115, 0.104), mat="Skin", k=0.04)                # crâne
    p.ell((0, -0.045, 0.02), (0.086, 0.074, 0.085), mat="Skin", k=0.05)         # joues, menton
    p.ell((0, -0.008, 0.103), (0.015, 0.02, 0.018), mat="Skin", k=0.014)        # nez
    for s in (-1, 1):
        p.ell((s * 0.1, -0.005, -0.005), (0.017, 0.03, 0.014), mat="Skin", k=0.01)   # oreilles
    # cheveux : volume, mèches pointues, frange asymétrique, nuque dégradée
    p.ell((0, 0.068, -0.02), (0.113, 0.088, 0.114), mat="Hair", k=0.03)
    p.ell((0, 0.015, -0.06), (0.106, 0.09, 0.08), mat="Hair", k=0.03)
    p.ell((0.02, 0.098, 0.062), (0.09, 0.04, 0.05), R=(18, 0, -14), mat="Hair", k=0.025)
    for x, z, h in ((-0.06, 0.02, 0.17), (-0.01, 0.0, 0.185), (0.045, -0.01, 0.175), (-0.03, -0.06, 0.165), (0.03, -0.07, 0.16)):
        p.cone((x * 0.7, 0.1, z * 0.7), (x, h, z), 0.04, 0.008, mat="Hair", k=0.03)   # épis
    p.cone((0.05, 0.1, 0.08), (0.085, 0.03, 0.11), 0.028, 0.006, mat="Hair", k=0.02)   # mèche sur l'œil
    for s in (-1, 1):
        p.ell((s * 0.093, 0.025, 0.02), (0.018, 0.045, 0.034), mat="Hair", k=0.015)   # pattes
    for s in (-1, 1):
        human_eye(p, (s * 0.037, 0.008, 0.0), (s * 0.3, 0.0, 1.0))
        p.stitch((0, 0.0, 0.0), [(s * 0.02, 0.046), (s * 0.04, 0.054), (s * 0.06, 0.049)], thick=0.0065)   # sourcils
    p.stitch((0, 0.0, 0.0), [(-0.026, -0.05), (-0.012, -0.059), (0.0, -0.061), (0.013, -0.059), (0.027, -0.049)], thick=0.0045)   # sourire
    m = p.build(uv=3.0, keep=True)
    for part in m.parts: part.V = part.V * 1.2   # tête un peu plus grande : style « jeu »
    return m.finish(keep=True)


@model
def player_uarm():
    p = Plush("player_uarm", res=48)
    p.ell((0, -0.01, 0), (0.06, 0.07, 0.06), mat="Shirt", k=0.03)
    p.cone((0, -0.02, 0), (0, -0.27, 0.0), 0.054, 0.047, mat="Shirt", k=0.03)
    return p.build(uv=3.0, keep=True)


@model
def player_farm():
    p = Plush("player_farm", res=56)
    p.ell((0, 0.0, 0), (0.043, 0.04, 0.043), mat="Shirt", k=0.02)              # coude (manche)
    p.ell((0, -0.03, 0), (0.05, 0.03, 0.05), mat="Jacket", k=0.015)             # poignet côtelé
    p.cone((0, -0.03, 0), (0, -0.23, 0.0), 0.039, 0.031, mat="Skin", k=0.02)    # avant-bras
    p.ell((0, -0.285, 0.004), (0.034, 0.048, 0.023), mat="Skin", k=0.02)        # paume
    for i, x in enumerate((-0.021, -0.007, 0.007, 0.021)):
        L = 0.04 if i in (1, 2) else 0.033
        p.cone((x, -0.315, 0.006), (x * 1.05, -0.315 - L, 0.012), 0.0095, 0.008, mat="Skin", k=0.006)   # doigts
    p.cone((-0.024, -0.27, 0.012), (-0.033, -0.31, 0.03), 0.012, 0.009, mat="Skin", k=0.01)   # pouce
    return p.build(uv=3.0, keep=True)


@model
def player_thigh():
    p = Plush("player_thigh", res=48)
    p.cone((0, 0.0, 0), (0, -0.42, 0.0), 0.085, 0.062, mat="Pants", k=0.03)
    return p.build(uv=3.0, keep=True)


@model
def player_shin():
    p = Plush("player_shin", res=60)
    p.ell((0, 0.0, 0), (0.062, 0.05, 0.062), mat="Pants", k=0.02)               # genou
    p.cone((0, 0.0, 0), (0, -0.34, 0.0), 0.06, 0.053, mat="Pants", k=0.03)
    p.ell((0, -0.355, 0), (0.062, 0.022, 0.062), mat="Jacket", k=0.01)         # revers
    p.ell((0, -0.44, 0.035), (0.058, 0.045, 0.125), mat="Sneaker", k=0.03)     # basket
    p.ell((0, -0.38, -0.015), (0.05, 0.04, 0.06), mat="Sneaker", k=0.03)
    p.ell((0, -0.48, 0.04), (0.064, 0.018, 0.135), mat="Light", k=0.008)       # semelle
    p.ell((0, -0.43, 0.12), (0.045, 0.03, 0.05), mat="Light", k=0.01)          # bout renforcé
    for s in (-1, 1): p.ell((s * 0.055, -0.44, 0.02), (0.008, 0.022, 0.07), mat="Accent", k=0.005)
    p.ell((0, -0.395, 0.07), (0.03, 0.012, 0.05), R=(-25, 0, 0), mat="Light", k=0.005)   # lacets
    return p.build(uv=3.0, keep=True)
