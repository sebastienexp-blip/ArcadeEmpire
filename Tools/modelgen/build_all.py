"""Génère tous les modèles FBX : python3 build_all.py"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_models, make_machines, make_games, make_games2, make_games3, make_plush  # noqa : l'import enregistre les modèles
from make_models import MODELS, OUT
from fbxlib import write_fbx, read_fbx_counts
import numpy as np

total = 0
for name, fn in MODELS.items():
    m = fn()
    path = os.path.join(OUT, name + ".fbx")
    write_fbx(m, path)
    nv, nt, nm = read_fbx_counts(path)
    total += nt
    print("%-18s %5d sommets %5d triangles %d matériaux  taille %s" % (name, nv, nt, nm, np.round(m.size, 2)))
print("total :", total, "triangles,", len(MODELS), "modèles")
