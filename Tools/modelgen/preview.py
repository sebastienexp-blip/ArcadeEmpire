"""Aperçu de contrôle (rendu logiciel simple, pas Unity) : python3 preview.py sortie.png"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
import numpy as np
from PIL import Image, ImageDraw
from make_models import MODELS
from fbxlib import COLORS

PAL = {"Main": (230, 120, 170), "Dark": (40, 40, 55), "Light": (245, 245, 245), "Accent": (255, 205, 50), "Pot": (150, 90, 55), "Soil": (70, 45, 30)}
OVR = {"prize_bear": (90, 150, 255), "prize_robot": (60, 220, 110), "prize_capsule": (180, 80, 255), "prize_cat_gold": (255, 200, 40), "decor_plant": (50, 170, 70), "decor_pouf": (230, 70, 150), "decor_trophy": (255, 200, 40)}
W = 300
names = list(MODELS)
cols = 5; rows = math.ceil(len(names) / cols)
img = Image.new("RGB", (cols * W, rows * W), (24, 24, 36))
for n, name in enumerate(names):
    m = MODELS[name]()
    ang = math.radians(35); el = math.radians(18)
    ry = np.array([[math.cos(ang), 0, math.sin(ang)], [0, 1, 0], [-math.sin(ang), 0, math.cos(ang)]])
    rx = np.array([[1, 0, 0], [0, math.cos(el), -math.sin(el)], [0, math.sin(el), math.cos(el)]])
    R = rx @ ry
    tris = []
    for p in m.parts:
        V = p.V @ R.T; N = p.N @ R.T
        base = OVR.get(name, PAL["Main"]) if p.mat == "Main" else PAL.get(p.mat, (200, 200, 200))
        for f in p.F:
            a, b, c = V[f]
            nn = np.cross(b - a, c - a)
            if nn[2] <= 0: continue
            vn = N[f].mean(0); vn /= (np.linalg.norm(vn) + 1e-9)
            l = max(0.0, vn @ np.array([-0.4, 0.6, 0.7]) / np.linalg.norm([-0.4, 0.6, 0.7]))
            sh = 0.35 + 0.65 * l
            tris.append(((a[2] + b[2] + c[2]) / 3, [a, b, c], tuple(int(x * sh) for x in base)))
    tris.sort(key=lambda t: t[0])
    allv = np.vstack([p.V @ R.T for p in m.parts]); ext = (allv.max(0) - allv.min(0)).max(); ctr = (allv.max(0) + allv.min(0)) / 2
    s = (W * 0.8) / ext
    d = ImageDraw.Draw(img); ox, oy = (n % cols) * W, (n // cols) * W
    for _, pts, col in tris:
        d.polygon([(ox + W / 2 + (q[0] - ctr[0]) * s, oy + W / 2 - (q[1] - ctr[1]) * s) for q in pts], fill=col)
    d.text((ox + 8, oy + 6), name, fill=(200, 200, 220))
img.save(sys.argv[1] if len(sys.argv) > 1 else "preview.png")
