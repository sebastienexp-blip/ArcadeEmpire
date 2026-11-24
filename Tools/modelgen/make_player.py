"""Personnage du joueur refait : surfaces lisses (champ de distance + marching cubes) comme un personnage de jeu stylisé.
Sweat à capuche avec poche kangourou et cordons, jean à revers, baskets à semelle blanche, mains avec pouce, visage expressif
(yeux brillants avec reflet, sourcils, sourire), coupe de cheveux avec mèche. Mêmes pivots que l'ancien personnage
(torse en coordonnées monde, tête au cou, bras à l'épaule, jambes à la hanche) : PlayerVisualAnimator reste inchangé."""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_plush import Plush
from fbxlib import tube, sphere
from make_models import model


@model
def player_torso():
    p = Plush("player_torso", res=56)
    p.ell((0, 1.2, 0.0), (0.2, 0.27, 0.13), mat="Shirt", k=0.06)              # buste
    p.ell((0, 1.36, 0.01), (0.215, 0.13, 0.135), mat="Shirt", k=0.06)         # poitrine
    for s in (-1, 1):
        p.ell((s * 0.205, 1.43, 0), (0.085, 0.07, 0.085), mat="Shirt", k=0.05)   # épaules
    p.ell((0, 1.0, 0), (0.185, 0.07, 0.125), mat="Jacket", k=0.03)            # bas côtelé du sweat
    p.ell((0, 0.94, 0), (0.17, 0.06, 0.115), mat="Pants", k=0.03)             # haut du jean
    p.ell((0, 1.08, 0.11), (0.12, 0.065, 0.035), mat="Jacket", k=0.015)       # poche kangourou
    p.ell((0, 1.5, -0.07), (0.15, 0.065, 0.085), mat="Jacket", k=0.04)        # capuche roulée derrière le cou
    p.cone((0, 1.47, 0.0), (0, 1.6, 0.005), 0.058, 0.052, mat="Skin", k=0.02)  # cou
    m = p.build(uv=3.0, keep=True)
    for s in (-1, 1):   # cordons de capuche
        m.add(tube([(s * 0.05, 1.47, 0.12), (s * 0.055, 1.38, 0.14), (s * 0.06, 1.3, 0.14)], 0.007, seg=6, mat="Light"))
        m.add(sphere(0.012, seg=8, rings=6, mat="Accent"), (s * 0.06, 1.29, 0.14))
    m.add(sphere((0.04, 0.04, 0.008), seg=12, rings=6, mat="Accent"), (0.08, 1.33, 0.14))   # petit logo
    return m


@model
def player_head():
    p = Plush("player_head", res=56)
    p.ell((0, 0.02, 0), (0.1, 0.118, 0.105), mat="Skin", k=0.04)               # crâne
    p.ell((0, -0.045, 0.02), (0.085, 0.075, 0.085), mat="Skin", k=0.05)        # joues et menton
    p.ell((0, -0.005, 0.105), (0.016, 0.022, 0.02), mat="Skin", k=0.012)       # nez
    for s in (-1, 1):
        p.ell((s * 0.1, -0.005, -0.005), (0.018, 0.03, 0.014), mat="Skin", k=0.01)   # oreilles
    # cheveux : volume au-dessus et derrière, mèche sur le front, pattes sur les côtés
    p.ell((0, 0.07, -0.02), (0.112, 0.085, 0.112), mat="Hair", k=0.03)
    p.ell((0, 0.02, -0.06), (0.105, 0.09, 0.08), mat="Hair", k=0.03)
    p.ell((0.025, 0.1, 0.06), (0.085, 0.04, 0.05), R=(15, 0, -12), mat="Hair", k=0.03)   # mèche
    p.ell((-0.04, 0.125, 0.0), (0.05, 0.035, 0.05), R=(0, 0, 20), mat="Hair", k=0.03)     # épi
    for s in (-1, 1):
        p.ell((s * 0.092, 0.03, 0.02), (0.02, 0.045, 0.035), mat="Hair", k=0.015)
    for s in (-1, 1):
        p.eye((s * 0.038, 0.012, 0.0), (s * 0.25, 0.0, 1.0), r=0.021, sink=0.35)
        p.stitch((0, 0.0, 0.0), [(s * 0.022, 0.047), (s * 0.04, 0.053), (s * 0.058, 0.047)], thick=0.006)   # sourcils
    p.stitch((0, 0.0, 0.0), [(-0.028, -0.052), (-0.012, -0.062), (0.0, -0.064), (0.012, -0.062), (0.028, -0.052)], thick=0.0045)   # sourire
    m = p.build(uv=3.0, keep=True)
    for part in m.parts: part.V = part.V * 1.12   # tête un peu plus grande : style « jeu »
    return m.finish(keep=True)


@model
def player_arm():
    p = Plush("player_arm", res=60)
    p.ell((0, -0.01, 0), (0.06, 0.07, 0.06), mat="Shirt", k=0.03)
    p.cone((0, -0.02, 0), (0, -0.25, 0.0), 0.054, 0.047, mat="Shirt", k=0.03)   # manche
    p.ell((0, -0.275, 0), (0.05, 0.03, 0.05), mat="Jacket", k=0.015)           # poignet côtelé
    p.cone((0, -0.29, 0), (0, -0.5, 0.0), 0.038, 0.031, mat="Skin", k=0.02)     # avant-bras
    p.ell((0, -0.555, 0.004), (0.033, 0.048, 0.022), mat="Skin", k=0.02)       # paume
    p.cone((0, -0.585, 0.004), (0, -0.625, 0.008), 0.024, 0.017, mat="Skin", k=0.02)   # doigts
    p.cone((-0.022, -0.54, 0.01), (-0.03, -0.585, 0.028), 0.012, 0.009, mat="Skin", k=0.012)   # pouce (côté intérieur)
    return p.build(uv=3.0, keep=True)


@model
def player_leg():
    p = Plush("player_leg", res=64)
    p.cone((0, 0.0, 0), (0, -0.42, 0.0), 0.085, 0.062, mat="Pants", k=0.03)     # cuisse
    p.cone((0, -0.42, 0), (0, -0.76, 0.0), 0.06, 0.053, mat="Pants", k=0.03)    # mollet
    p.ell((0, -0.775, 0), (0.062, 0.022, 0.062), mat="Jacket", k=0.01)         # revers du jean
    p.ell((0, -0.86, 0.035), (0.058, 0.045, 0.125), mat="Sneaker", k=0.03)     # basket
    p.ell((0, -0.8, -0.015), (0.05, 0.04, 0.06), mat="Sneaker", k=0.03)        # col de la basket
    p.ell((0, -0.9, 0.04), (0.064, 0.018, 0.135), mat="Light", k=0.008)        # semelle
    p.ell((0, -0.85, 0.12), (0.045, 0.03, 0.05), mat="Light", k=0.01)          # bout renforcé
    p.ell((0.055, -0.86, 0.02), (0.008, 0.022, 0.07), mat="Accent", k=0.005)   # bande latérale
    p.ell((-0.055, -0.86, 0.02), (0.008, 0.022, 0.07), mat="Accent", k=0.005)
    p.ell((0, -0.815, 0.07), (0.03, 0.012, 0.05), R=(-25, 0, 0), mat="Light", k=0.005)   # lacets
    return p.build(uv=3.0, keep=True)
