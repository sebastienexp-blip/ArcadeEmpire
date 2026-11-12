"""Aperçu de contrôle des machines et du personnage (rendu logiciel, pas Unity) : python3 preview2.py sortie.png"""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image, ImageDraw
import make_models, make_machines
from make_models import MODELS

PAL = {"Paint": (46, 44, 66), "Metal": (170, 175, 190), "Rubber": (30, 30, 34), "NeonPink": (255, 60, 170), "NeonCyan": (40, 230, 255), "NeonYellow": (255, 220, 40),
       "SignClaw": (30, 170, 210), "SignPusher": (255, 80, 170), "DecalClaw": (255, 220, 40), "DecalPusher": (255, 220, 40), "Dark": (25, 25, 32), "Plastic": (235, 235, 240),
       "StickBall": (255, 60, 170), "ButtonColor": (255, 60, 170), "Rim": (240, 190, 40), "Face": (255, 225, 110), "Shirt": (240, 140, 40), "Jacket": (50, 60, 90),
       "Skin": (232, 180, 140), "Hair": (70, 45, 30), "Light": (245, 245, 245), "Pants": (50, 90, 160), "Sneaker": (240, 240, 245), "Accent": (255, 220, 40), "Mouth": (150, 60, 60)}


def draw(img, box, items, ang, el=18):
    ox, oy, W, H = box
    a, e = math.radians(ang), math.radians(el)
    ry = np.array([[math.cos(a), 0, math.sin(a)], [0, 1, 0], [-math.sin(a), 0, math.cos(a)]])
    rx = np.array([[1, 0, 0], [0, math.cos(e), -math.sin(e)], [0, math.sin(e), math.cos(e)]])
    R = rx @ ry
    tris = []
    allv = []
    for name, off, rotm in items:
        m = MODELS[name]()
        for p in m.parts:
            V = (p.V @ rotm.T + np.array(off)); N = p.N @ rotm.T
            V = V @ R.T; N = N @ R.T; allv.append(V)
            base = PAL.get(p.mat, (200, 120, 160))
            for f in p.F:
                x, y, z = V[f]
                if np.cross(y - x, z - x)[2] <= 0: continue
                vn = N[f].mean(0); vn /= np.linalg.norm(vn) + 1e-9
                l = max(0, vn @ np.array([-0.4, 0.6, 0.7]) / 0.9)
                tris.append(((x[2] + y[2] + z[2]) / 3, [x, y, z], tuple(int(c * (0.35 + 0.65 * l)) for c in base)))
    tris.sort(key=lambda t: t[0])
    allv = np.vstack(allv); ext = (allv.max(0) - allv.min(0)); s = min(W / ext[0], H / ext[1]) * 0.9; c = (allv.max(0) + allv.min(0)) / 2
    d = ImageDraw.Draw(img)
    for _, pts, col in tris:
        d.polygon([(ox + W / 2 + (q[0] - c[0]) * s, oy + H / 2 - (q[1] - c[1]) * s) for q in pts], fill=col)


I = np.eye(3)
img = Image.new("RGB", (1500, 900), (24, 24, 36))
draw(img, (0, 0, 500, 600), [("claw_shell", (0, 0, 0), I), ("claw_head", (-0.4, 1.55, -0.3), I)] + [("claw_prong", (-0.4 + math.cos(math.radians(k * 120)) * 0.06, 1.52, -0.3 + math.sin(math.radians(k * 120)) * 0.06), I) for k in range(3)] + [("arcade_stick", (-0.3, 0.65, -0.85), I), ("arcade_button", (0.3, 0.665, -0.85), I)], 200)
draw(img, (500, 0, 500, 600), [("pusher_shell", (0, 0, 0), I), ("pusher_plate", (0, 0.96, 0.7), I), ("pusher_aim", (0, 1.48, 0.25), I), ("arcade_button", (0, 0.665, -1.15), I)], 200)
leg = lambda x, rotz: ("player_leg", (x, 0.92, 0), I)
draw(img, (1000, 0, 500, 600), [("player_torso", (0, 0, 0), I), ("player_head", (0, 1.64, 0.0), I), ("player_arm", (-0.3, 1.45, 0), I), ("player_arm", (0.3, 1.45, 0), I), ("player_leg", (-0.11, 0.95, 0), I), ("player_leg", (0.11, 0.95, 0), I)], 25, 8)
draw(img, (0, 600, 500, 300), [("claw_shell", (0, 0, 0), I)], 160, 10)
draw(img, (500, 600, 500, 300), [("coin_unit", (0, 0, 0), np.diag([1, 0.25, 1]))], 20, 35)
draw(img, (1000, 600, 500, 300), [("pusher_shell", (0, 0, 0), I)], 340, 12)
img.save(sys.argv[1])
