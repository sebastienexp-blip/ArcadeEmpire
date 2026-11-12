"""Phase 9, 3e série : course d'arcade et stacker. Repère local : sol y=0, façade vers -Z."""
import math
import numpy as np
from make_models import model
from make_games import sign_quads, feet, coin_mech
from fbxlib import *


@model
def game_race():
    m = Model("game_race")
    m.add(rbox((0.95, 0.95, 0.9), 0.12, mat="Paint"), (0, 0.475, 0))
    m.add(rbox((0.9, 1.4, 0.6), 0.1, mat="Paint"), (0, 1.65, 0))
    m.add(rbox((0.98, 0.08, 0.5), 0.3, mat="Dark"), (0, 0.97, -0.3))
    m.add(quad(0.64, 0.84, "RaceScreen", flip=True), (0, 1.6, -0.304))
    for s in (-1, 1):
        m.add(box((0.04, 0.9, 0.03), "Dark"), (s * 0.34, 1.6, -0.31))
    for y in (1.16, 2.04): m.add(box((0.72, 0.04, 0.03), "Dark"), (0, y, -0.31))
    m.add(rbox((1.0, 0.3, 0.5), 0.12, mat="Paint"), (0, 2.55, 0.05))
    sign_quads(m, "SignRace", 0.9, 0.26, 0, 2.55, -0.2, back_z=0.3)
    m.add(cylinder(0.02, 0.02, 0.14, seg=10, mat="Metal"), (0, 1.04, -0.38))
    m.add(torus(0.1, 0.013, seg=28, tube=8, mat="Rubber"), (0, 1.12, -0.42), R=rot(-25, 0, 0))
    m.add(sphere(0.03, seg=10, rings=7, mat="NeonPink"), (0, 1.12, -0.42))
    feet(m, 0.4, (-0.38, 0.38))
    coin_mech(m, 0.45, -0.455)
    m.add(box((0.9, 0.02, 0.02), "NeonPink"), (0, 0.12, -0.455))
    return m.finish(keep=True)


@model
def race_car():
    """Voiture vue de l'arrière : pivot au sol et au centre, arrière vers -Z, avant vers +Z."""
    m = Model("race_car")
    m.add(rbox((0.1, 0.035, 0.16), 0.3, mat="Main"), (0, 0.035, 0))
    m.add(rbox((0.07, 0.03, 0.08), 0.3, mat="Dark"), (0, 0.065, 0.015))
    m.add(rbox((0.1, 0.012, 0.03), 0.2, mat="Main"), (0, 0.09, -0.07))
    for s in (-1, 1):
        m.add(rbox((0.02, 0.04, 0.055), 0.3, mat="Rubber"), (s * 0.055, 0.02, -0.04))
        m.add(rbox((0.02, 0.04, 0.055), 0.3, mat="Rubber"), (s * 0.055, 0.02, 0.05))
        m.add(box((0.022, 0.012, 0.008), "NeonPink"), (s * 0.03, 0.04, -0.082))
    return m.finish(keep=True)


@model
def game_stacker():
    m = Model("game_stacker")
    m.add(rbox((0.95, 0.9, 0.9), 0.12, mat="Paint"), (0, 0.45, -0.05))
    m.add(rbox((0.9, 1.7, 0.6), 0.1, mat="Paint"), (0, 1.75, 0.05))
    m.add(box((0.76, 1.22, 0.01), "Dark"), (0, 1.75, -0.255))
    for s in (-1, 1): m.add(box((0.04, 1.26, 0.03), "NeonCyan"), (s * 0.39, 1.75, -0.27))
    for y in (1.14, 2.36): m.add(box((0.82, 0.04, 0.03), "NeonCyan"), (0, y, -0.27))
    m.add(rbox((1.0, 0.3, 0.5), 0.12, mat="Paint"), (0, 2.75, 0.0))
    sign_quads(m, "SignStacker", 0.9, 0.26, 0, 2.75, -0.25, back_z=0.25)
    m.add(rbox((0.9, 0.06, 0.4), 0.3, mat="Dark"), (0, 0.93, -0.35))
    feet(m, 0.4, (-0.4, 0.3))
    coin_mech(m, 0.45, -0.505)
    m.add(box((0.9, 0.02, 0.02), "NeonYellow"), (0, 0.12, -0.505))
    return m.finish(keep=True)


@model
def stack_block():
    m = Model("stack_block")
    m.add(rbox((0.088, 0.088, 0.05), 0.2, mat="Main"))
    return m.finish(keep=True)
