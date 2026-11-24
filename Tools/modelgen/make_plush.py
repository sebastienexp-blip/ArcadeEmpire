"""Peluches réalistes : chaque peluche est UNE surface lisse (champ de distance signée + marching cubes), comme un tissu
rembourré où tête, oreilles et pattes se fondent sans couture visible. Détails rapportés : yeux de sécurité brillants avec reflet,
truffe, bouche brodée (fil), étiquette « tush tag ». Les zones de couleur (ventre, museau, intérieur des oreilles…) sont des
matériaux séparés : Main, Light, Second, Dark (fil, truffe), Eye, Accent (bec, corne, plastique), Tag.
Usage : importé par build_all.py ; aperçu : python3 make_plush.py apercu.png"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from skimage.measure import marching_cubes
from fbxlib import Part, Model, sphere, tube, rbox, rot, _norm, lathe, torus, cylinder
from make_models import model


def _smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0.0, 1.0)
    return b * (1 - h) + a * h - k * h * (1 - h)


class Plush:
    """Accumule des primitives douces puis produit le maillage."""

    default_res = 48

    def __init__(self, name, res=None):
        self.name = name; self.res = res or Plush.default_res; self.prims = []; self.details = []

    # --- primitives (centre, rayons, rotation en degrés, matériau, douceur de la fusion k)
    def ell(self, c, r, R=(0, 0, 0), mat="Main", k=0.07):
        c = np.asarray(c, float); r = np.asarray(r, float); M = rot(*R)
        def f(P):
            q = (P - c) @ M / r
            k0 = np.linalg.norm(q, axis=-1)
            k1 = np.linalg.norm(q / r, axis=-1)
            return k0 * (k0 - 1.0) / np.maximum(k1, 1e-9)
        self.prims.append((f, mat, k, c, float(r.max())))
        return self

    def cone(self, a, b, r0, r1, mat="Main", k=0.06):
        """Capsule à rayon variable (oreille pointue, corne, patte effilée)."""
        a = np.asarray(a, float); b = np.asarray(b, float); ba = b - a; L2 = ba @ ba
        def f(P):
            pa = P - a
            h = np.clip((pa @ ba) / L2, 0, 1)[..., None]
            return np.linalg.norm(pa - ba * h, axis=-1) - (r0 + (r1 - r0) * h[..., 0])
        self.prims.append((f, mat, k, (a + b) / 2, math.sqrt(L2) / 2 + max(r0, r1)))
        return self

    def chain(self, pts, r0, r1, mat="Main", k=0.05):
        """Chaîne de cônes (queue, tentacule, crinière)."""
        n = len(pts)
        for i in range(n - 1):
            ra = r0 + (r1 - r0) * i / (n - 1); rb = r0 + (r1 - r0) * (i + 1) / (n - 1)
            self.cone(pts[i], pts[i + 1], ra, rb, mat, k)
        return self

    def sym(self, fn, *args, **kw):
        """Ajoute la primitive et son symétrique (miroir X)."""
        fn(*args, **kw)
        mir = lambda v: (-v[0], v[1], v[2])
        a2 = [mir(a) if (isinstance(a, (tuple, list)) and len(a) == 3 and not isinstance(a[0], (tuple, list))) else
              ([mir(p) for p in a] if isinstance(a, list) and a and isinstance(a[0], (tuple, list)) else a) for a in args]
        if "R" in kw: kw = dict(kw); kw["R"] = (kw["R"][0], -kw["R"][1], -kw["R"][2])
        fn(*a2, **kw)
        return self

    # --- évaluation
    def field(self, P):
        d = None
        for f, mat, k, c, rr in self.prims:
            v = f(P)
            d = v if d is None else _smin(d, v, k)
        return d

    def labels(self, P):
        D = np.stack([f(P) for f, *_ in self.prims], -1)
        return D.argmin(-1)

    def grad(self, P, e=1e-3):
        g = np.zeros_like(P)
        for i in range(3):
            o = np.zeros(3); o[i] = e
            g[:, i] = self.field(P + o) - self.field(P - o)
        return _norm(g)

    def surface(self, origin, direction, inside_start=True):
        """Point de surface atteint depuis l'intérieur (origin) dans la direction donnée."""
        o = np.asarray(origin, float); d = _norm(np.asarray(direction, float)[None])[0]
        t = 0.0
        for _ in range(200):
            v = self.field((o + d * t)[None])[0]
            if v > 0: break
            t += 0.004
        lo, hi = max(0.0, t - 0.004), t
        for _ in range(25):
            m = (lo + hi) / 2
            if self.field((o + d * m)[None])[0] > 0: hi = m
            else: lo = m
        p = o + d * hi
        return p, self.grad(p[None])[0]

    # --- détails rapportés
    def eye(self, origin, direction, r=0.055, sink=0.45):
        p, n = self.surface(origin, direction)
        c = p - n * r * sink
        self.details.append((sphere(r, seg=14, rings=10, mat="Eye"), c, None))
        # reflet : petite bille blanche en haut, côté extérieur
        up = np.array([0, 1.0, 0]); side = _norm(np.cross(n, up)[None])[0]
        h = c + n * r * 0.82 + up * r * 0.38 + side * r * 0.25 * np.sign(direction[0] or 1)
        self.details.append((sphere(r * 0.22, seg=8, rings=6, mat="Tag"), h, None))
        return self

    def nose(self, origin, direction, r=(0.06, 0.045, 0.04), sink=0.5):
        p, n = self.surface(origin, direction)
        self.details.append((sphere(r, seg=12, rings=8, mat="Dark"), p - n * r[2] * sink, None))
        return self

    def stitch(self, origin, curve, thick=0.008):
        """Fil brodé : courbe 2D (x, y) projetée sur la surface depuis l'intérieur, vers l'avant (+Z)."""
        o = np.asarray(origin, float); pts = []
        for x, y in curve:
            p, n = self.surface(o + np.array([x, y, 0]), (x * 0.6, y * 0.6, 1.0))
            pts.append(p + n * thick * 0.4)
        self.details.append((tube(pts, thick, seg=6, mat="Dark"), (0, 0, 0), None))
        return self

    def tag(self, origin, direction, size=(0.12, 0.16, 0.012)):
        p, n = self.surface(origin, direction)
        R = _rot_to(n)
        self.details.append((rbox(size, e=0.3, seg=8, rings=6, mat="Tag"), p + n * size[2] * 0.4, R))
        return self

    def solid(self, part, t=(0, 0, 0), R=None):
        self.details.append((part, t, R))
        return self

    # --- maillage
    def build(self, uv=2.2, keep=False):
        cs = np.array([c for *_, c, rr in self.prims]); rs = np.array([rr for *_, c, rr in self.prims])
        lo = (cs - rs[:, None]).min(0) - 0.08; hi = (cs + rs[:, None]).max(0) + 0.08
        vox = (hi - lo).max() / self.res
        axes = [np.arange(lo[i], hi[i] + vox, vox) for i in range(3)]
        G = np.stack(np.meshgrid(*axes, indexing="ij"), -1)
        vol = self.field(G.reshape(-1, 3)).reshape(G.shape[:3])
        V, F, _, _ = marching_cubes(vol, 0.0, spacing=(vox, vox, vox))
        V = V + lo
        N = self.grad(V)
        cent = V[F].mean(1)
        lab = self.labels(cent)
        fn = _norm(N[F].sum(1))
        axis = np.abs(fn).argmax(1)
        m = Model(self.name, uv=1.0)
        mats = [p[1] for p in self.prims]
        for li in np.unique(lab):
            for ax in range(3):
                sel = F[(lab == li) & (axis == ax)]
                if len(sel) == 0: continue
                idx, inv = np.unique(sel, return_inverse=True)
                v = V[idx]; others = [a for a in range(3) if a != ax]
                uvs = v[:, others] * uv
                m.parts.append(Part(v, N[idx], inv.reshape(-1, 3), mats[li], uvs))
        for part, t, R in self.details:
            m.add(part, t, R)
        for p in m.parts[-len(self.details):] if self.details else []:
            p.UV = p.UV * uv
        return m.finish(keep=keep)


def _rot_to(n):
    """Rotation qui envoie +Z sur n."""
    z = _norm(np.asarray(n, float)[None])[0]
    a = np.array([0, 1.0, 0]) if abs(z[1]) < 0.9 else np.array([1.0, 0, 0])
    x = _norm(np.cross(a, z)[None])[0]; y = np.cross(z, x)
    return np.column_stack([x, y, z])


# ------------------------------------------------------------------ gabarit « peluche assise »
def seated(p, head=(0.29, 0.26, 0.26), body=(0.3, 0.32, 0.26), muzzle=True, belly=True, arms=True, legs=True, head_y=0.8):
    p.ell((0, 0.33, 0), body, k=0.08)
    p.ell((0, head_y, 0.03), head, k=0.1)
    if belly: p.ell((0, 0.32, 0.12), (body[0] * 0.62, body[1] * 0.66, body[2] * 0.62), mat="Light", k=0.02)
    if muzzle: p.ell((0, head_y - 0.07, 0.2), (0.12, 0.09, 0.09), mat="Light", k=0.04)
    if arms:
        p.cone((0.22, 0.52, 0.05), (0.33, 0.32, 0.17), 0.085, 0.075, k=0.06)
        p.cone((-0.22, 0.52, 0.05), (-0.33, 0.32, 0.17), 0.085, 0.075, k=0.06)
    if legs:
        p.cone((0.15, 0.13, 0.04), (0.18, 0.1, 0.3), 0.105, 0.1, k=0.05)
        p.cone((-0.15, 0.13, 0.04), (-0.18, 0.1, 0.3), 0.105, 0.1, k=0.05)
        p.ell((0.18, 0.1, 0.38), (0.07, 0.07, 0.03), mat="Light", k=0.015)
        p.ell((-0.18, 0.1, 0.38), (0.07, 0.07, 0.03), mat="Light", k=0.015)
    return p


def face(p, head_y=0.8, eye_dx=0.1, eye_y=0.06, eye_r=0.05, nose=True, mouth=True, nose_r=(0.055, 0.04, 0.04)):
    hc = (0, head_y, 0.03)
    for s in (-1, 1): p.eye(hc, (s * eye_dx * 2.2, eye_y * 2.0, 1.0), r=eye_r)
    if nose: p.nose(hc, (0, -0.15, 1.0), r=nose_r)
    if mouth:
        o = (0, head_y - 0.11, 0.0)
        p.stitch(o, [(0, 0.035), (0, 0.0)])
        p.stitch(o, [(-0.055, 0.005), (-0.03, -0.015), (0, 0.0), (0.03, -0.015), (0.055, 0.005)])
    p.tag((0, 0.18, 0), (0.75, -0.2, -0.65))
    return p


@model
def plush_bear():
    p = seated(Plush("plush_bear"))
    for s in (-1, 1):
        p.ell((s * 0.2, 1.01, 0.0), (0.095, 0.095, 0.06), k=0.04)
        p.ell((s * 0.2, 1.0, 0.035), (0.055, 0.06, 0.03), mat="Light", k=0.01)
    return face(p).build()


@model
def plush_bunny():
    p = seated(Plush("plush_bunny"), head=(0.27, 0.25, 0.25))
    for s in (-1, 1):
        p.ell((s * 0.1, 1.2, -0.02), (0.075, 0.25, 0.045), R=(0, 0, -s * 9), k=0.05)
        p.ell((s * 0.105, 1.18, 0.022), (0.045, 0.19, 0.02), R=(0, 0, -s * 9), mat="Light", k=0.01)
    p.ell((0, 0.18, -0.27), (0.09, 0.09, 0.08), mat="Light", k=0.03)
    return face(p, nose_r=(0.04, 0.03, 0.03)).build()


@model
def plush_cat():
    p = seated(Plush("plush_cat"), head=(0.3, 0.25, 0.25), muzzle=False)
    p.ell((0, 0.73, 0.21), (0.11, 0.065, 0.07), mat="Light", k=0.04)
    for s in (-1, 1):
        p.cone((s * 0.16, 0.95, 0.0), (s * 0.22, 1.12, -0.01), 0.085, 0.012, k=0.04)
        p.cone((s * 0.165, 0.96, 0.03), (s * 0.215, 1.08, 0.025), 0.045, 0.006, mat="Light", k=0.005)
    p.chain([(0.18, 0.1, -0.2), (0.32, 0.12, -0.32), (0.4, 0.3, -0.36), (0.35, 0.5, -0.32)], 0.055, 0.045, mat="Second")
    p = face(p, eye_r=0.055, nose_r=(0.035, 0.025, 0.025))
    for s in (-1, 1):   # moustaches brodées
        o = (0, 0.72, 0.0)
        p.stitch(o, [(s * 0.07, 0.0), (s * 0.13, 0.012)], thick=0.004)
        p.stitch(o, [(s * 0.07, -0.02), (s * 0.13, -0.028)], thick=0.004)
    return p.build()


@model
def plush_dog():
    p = seated(Plush("plush_dog"), head=(0.28, 0.26, 0.27))
    p.ell((0, 0.72, 0.24), (0.13, 0.1, 0.11), mat="Light", k=0.05)
    for s in (-1, 1):
        p.ell((s * 0.29, 0.78, -0.01), (0.06, 0.19, 0.11), R=(0, 0, s * 18), mat="Second", k=0.04)
    p.ell((0.12, 0.88, 0.2), (0.08, 0.07, 0.04), mat="Second", k=0.02)   # tache sur l'œil
    p.chain([(0, 0.15, -0.24), (0, 0.22, -0.36), (0, 0.33, -0.4)], 0.04, 0.03)
    return face(p, nose_r=(0.065, 0.048, 0.045)).build()


@model
def plush_penguin():
    p = Plush("plush_penguin")
    p.ell((0, 0.45, 0), (0.31, 0.45, 0.28), k=0.08)
    p.ell((0, 0.88, 0.0), (0.25, 0.23, 0.24), k=0.1)
    p.ell((0, 0.42, 0.12), (0.23, 0.36, 0.19), mat="Light", k=0.02)
    p.ell((0, 0.86, 0.12), (0.17, 0.15, 0.14), mat="Light", k=0.02)
    for s in (-1, 1):
        p.ell((s * 0.3, 0.5, 0.0), (0.06, 0.24, 0.12), R=(0, 0, -s * 22), k=0.04)
        p.ell((s * 0.12, 0.03, 0.14), (0.1, 0.04, 0.14), mat="Accent", k=0.03)
    p.cone((0, 0.83, 0.2), (0, 0.8, 0.34), 0.06, 0.012, mat="Accent", k=0.02)
    hc = (0, 0.88, 0.0)
    for s in (-1, 1): p.eye(hc, (s * 0.45, 0.25, 1.0), r=0.045)
    p.tag((0, 0.2, 0), (0.7, -0.2, -0.7))
    return p.build()


@model
def plush_frog():
    p = Plush("plush_frog")
    p.ell((0, 0.3, 0), (0.34, 0.28, 0.29), k=0.08)
    p.ell((0, 0.6, 0.05), (0.35, 0.2, 0.27), k=0.1)
    p.ell((0, 0.28, 0.14), (0.24, 0.2, 0.17), mat="Light", k=0.02)
    p.ell((0, 0.53, 0.18), (0.26, 0.09, 0.12), mat="Light", k=0.02)
    for s in (-1, 1):
        p.ell((s * 0.16, 0.77, 0.08), (0.11, 0.1, 0.1), k=0.05)
        p.cone((s * 0.25, 0.15, 0.0), (s * 0.4, 0.06, 0.2), 0.09, 0.07, k=0.05)
        p.ell((s * 0.43, 0.04, 0.27), (0.08, 0.025, 0.07), k=0.03)
        p.cone((s * 0.27, 0.45, 0.08), (s * 0.36, 0.25, 0.22), 0.065, 0.055, k=0.05)
    for s in (-1, 1): p.eye((s * 0.16, 0.77, 0.08), (0, 0.3, 1.0), r=0.06, sink=0.35)
    p.stitch((0, 0.56, 0.0), [(-0.16, 0.01), (-0.08, -0.025), (0, -0.03), (0.08, -0.025), (0.16, 0.01)])
    p.tag((0, 0.2, 0), (0.7, -0.2, -0.7))
    return p.build()


@model
def plush_duck():
    p = Plush("plush_duck")
    p.ell((0, 0.33, 0), (0.3, 0.3, 0.34), k=0.08)
    p.ell((0, 0.76, 0.06), (0.22, 0.22, 0.21), k=0.1)
    p.ell((0, 0.69, 0.27), (0.12, 0.04, 0.11), mat="Accent", k=0.03)
    p.ell((0, 0.66, 0.26), (0.1, 0.03, 0.09), mat="Accent", k=0.03)
    p.cone((0, 0.4, -0.3), (0, 0.55, -0.4), 0.08, 0.02, k=0.04)
    for s in (-1, 1):
        p.ell((s * 0.29, 0.38, -0.02), (0.06, 0.15, 0.22), R=(10, 0, -s * 12), k=0.04)
        p.ell((s * 0.13, 0.03, 0.18), (0.1, 0.03, 0.12), mat="Accent", k=0.03)
    p.ell((0, 0.97, 0.03), (0.03, 0.06, 0.03), k=0.02)
    hc = (0, 0.76, 0.06)
    for s in (-1, 1): p.eye(hc, (s * 0.55, 0.2, 1.0), r=0.042)
    p.tag((0, 0.2, 0), (0.7, -0.2, -0.7))
    return p.build()


@model
def plush_unicorn():
    p = seated(Plush("plush_unicorn"), head=(0.26, 0.25, 0.28))
    p.ell((0, 0.72, 0.24), (0.13, 0.1, 0.1), mat="Light", k=0.05)
    p.cone((0, 1.0, 0.13), (0, 1.25, 0.2), 0.05, 0.008, mat="Accent", k=0.015)
    for s in (-1, 1): p.cone((s * 0.15, 0.97, 0.0), (s * 0.19, 1.1, -0.02), 0.06, 0.012, k=0.03)
    p.chain([(0, 1.03, 0.04), (0, 1.02, -0.12), (0, 0.9, -0.24), (0, 0.72, -0.28)], 0.06, 0.05, mat="Second", k=0.03)
    p.chain([(0, 0.2, -0.24), (0.05, 0.25, -0.38), (0.02, 0.42, -0.46)], 0.06, 0.04, mat="Second")
    return face(p, nose=False, eye_r=0.05).build()


@model
def plush_dino():
    p = Plush("plush_dino")
    p.ell((0, 0.35, 0), (0.28, 0.3, 0.36), k=0.08)
    p.ell((0, 0.74, 0.26), (0.25, 0.23, 0.29), k=0.1)
    p.ell((0, 0.67, 0.43), (0.15, 0.1, 0.12), mat="Light", k=0.05)
    p.ell((0, 0.3, 0.12), (0.18, 0.2, 0.22), mat="Light", k=0.02)
    p.chain([(0, 0.25, -0.3), (0, 0.18, -0.52), (0.06, 0.12, -0.7), (0.14, 0.08, -0.82)], 0.17, 0.04)
    for s in (-1, 1):
        p.cone((s * 0.17, 0.15, 0.05), (s * 0.19, 0.06, 0.22), 0.1, 0.095, k=0.05)
        p.cone((s * 0.2, 0.45, 0.22), (s * 0.27, 0.36, 0.34), 0.05, 0.04, k=0.04)
    for i, (y, z) in enumerate([(0.9, 0.24), (0.86, 0.06), (0.68, -0.12), (0.52, -0.3), (0.36, -0.48), (0.24, -0.64)]):
        sz = 0.075 - i * 0.008
        p.cone((0, y - 0.04, z), (0, y + sz, z - 0.03), sz * 0.85, 0.01, mat="Second", k=0.02)
    hc = (0, 0.74, 0.26)
    for s in (-1, 1): p.eye(hc, (s * 0.55, 0.45, 0.8), r=0.05)
    p.stitch((0, 0.64, 0.3), [(-0.11, 0.0), (0, -0.025), (0.11, 0.0)])
    p.tag((0, 0.2, 0), (0.75, -0.2, -0.3))
    return p.build()


@model
def plush_panda():
    p = seated(Plush("plush_panda"), belly=False, muzzle=False, arms=False, legs=False)
    p.ell((0, 0.72, 0.2), (0.12, 0.08, 0.09), k=0.04)
    for s in (-1, 1):
        p.ell((s * 0.21, 1.01, 0.0), (0.09, 0.09, 0.06), mat="Second", k=0.04)
        p.ell((s * 0.1, 0.86, 0.23), (0.07, 0.085, 0.05), R=(0, 0, s * 25), mat="Second", k=0.015)
        p.cone((s * 0.22, 0.52, 0.05), (s * 0.33, 0.32, 0.17), 0.085, 0.075, mat="Second", k=0.06)
        p.cone((s * 0.15, 0.13, 0.04), (s * 0.18, 0.1, 0.3), 0.105, 0.1, mat="Second", k=0.05)
    hc = (0, 0.8, 0.03)
    for s in (-1, 1): p.eye(hc, (s * 0.24, 0.13, 1.0), r=0.035)
    p.nose(hc, (0, -0.15, 1.0), r=(0.045, 0.032, 0.03))
    p.stitch((0, 0.69, 0.0), [(-0.04, 0.0), (0, -0.015), (0.04, 0.0)])
    p.tag((0, 0.18, 0), (0.75, -0.2, -0.65))
    return p.build()


@model
def plush_octopus():
    p = Plush("plush_octopus")
    p.ell((0, 0.62, 0), (0.33, 0.36, 0.31), k=0.08)
    for i in range(8):
        a = math.radians(i * 45 + 22.5); c, s = math.cos(a), math.sin(a)
        curl = 1 if i % 2 else -1
        pts = [(c * 0.18, 0.35, s * 0.18), (c * 0.32, 0.16, s * 0.32), (c * 0.45, 0.07, s * 0.45),
               (c * 0.55 - s * 0.06 * curl, 0.1, s * 0.55 + c * 0.06 * curl), (c * 0.56 - s * 0.12 * curl, 0.2, s * 0.56 + c * 0.12 * curl)]
        p.chain(pts, 0.085, 0.035, k=0.05)
    hc = (0, 0.6, 0)
    for s in (-1, 1): p.eye(hc, (s * 0.35, 0.0, 1.0), r=0.06)
    p.stitch((0, 0.5, 0.0), [(-0.06, 0.01), (0, -0.02), (0.06, 0.01)])
    for s in (-1, 1): p.ell((s * 0.17, 0.5, 0.27), (0.05, 0.03, 0.02), mat="Second", k=0.01)   # joues roses
    p.tag((0, 0.4, 0), (0.6, -0.3, -0.7))
    return p.build()


@model
def plush_fox():
    p = seated(Plush("plush_fox"), head=(0.29, 0.25, 0.25), muzzle=False)
    p.ell((0, 0.71, 0.21), (0.15, 0.08, 0.12), mat="Light", k=0.05)
    p.cone((0, 0.73, 0.2), (0, 0.71, 0.33), 0.08, 0.04, k=0.04)
    for s in (-1, 1):
        p.cone((s * 0.15, 0.94, 0.0), (s * 0.23, 1.16, -0.02), 0.095, 0.015, k=0.04)
        p.cone((s * 0.22, 1.09, -0.015), (s * 0.235, 1.17, -0.025), 0.035, 0.008, mat="Dark", k=0.01)
    p.chain([(0.1, 0.12, -0.2), (0.3, 0.18, -0.36), (0.45, 0.36, -0.38)], 0.09, 0.13)
    p.ell((0.5, 0.48, -0.36), (0.1, 0.12, 0.1), mat="Light", k=0.04)
    hc = (0, 0.8, 0.03)
    for s in (-1, 1): p.eye(hc, (s * 0.22, 0.12, 1.0), r=0.045)
    p.nose((0, 0.73, 0.0), (0, 0.0, 1.0), r=(0.035, 0.028, 0.03))
    p.tag((0, 0.18, 0), (0.75, -0.2, -0.65))
    return p.build()


@model
def prize_box():
    """Figurine en boîte (lot classique des UFO catchers) : carton imprimé + fenêtre."""
    m = Model("prize_box", uv=1.0)
    m.add(rbox((0.62, 1.0, 0.42), e=0.06, mat="Main"))
    m.add(rbox((0.46, 0.66, 0.02), e=0.15, mat="Light"), (0, 0.04, 0.205))
    m.add(rbox((0.3, 0.5, 0.18), e=0.5, mat="Accent"), (0, 0.04, 0.1))
    m.add(rbox((0.64, 0.12, 0.44), e=0.08, mat="Second"), (0, -0.45, 0))
    m.add(rbox((0.64, 0.12, 0.44), e=0.08, mat="Second"), (0, 0.45, 0))
    return m.finish()


# ------------------------------------------------------------------ idoles K-pop (personnages inventés, style chibi)
# Main = tenue de scène, Light = peau, Second = cheveux, Accent = détails dorés/argentés, Dark = chaussures/pantalon/fil
def idol(name, hair, girl=False, outfit=None):
    p = Plush(name)
    if outfit: outfit(p)
    p.ell((0, 0.74, 0.0), (0.27, 0.27, 0.25), mat="Light", k=0.06)                 # tête (peau)
    p.ell((0, 0.47, 0.0), (0.06, 0.06, 0.05), mat="Light", k=0.03)                 # cou
    p.ell((0, 0.3, 0.0), (0.16, 0.16, 0.12), k=0.06)                               # buste (tenue)
    p.ell((0, 0.36, 0.1), (0.07, 0.1, 0.03), mat="Accent", k=0.01)                 # col / cravate / bijou
    for s in (-1, 1):
        p.cone((s * 0.14, 0.4, 0.0), (s * 0.2, 0.2, 0.05), 0.055, 0.045, k=0.04)   # bras (manches)
        p.ell((s * 0.21, 0.17, 0.06), (0.04, 0.04, 0.04), mat="Light", k=0.02)     # mains
        p.ell((s * 0.26, 0.74, 0.0), (0.03, 0.05, 0.035), mat="Light", k=0.02)     # oreilles
    if girl:
        p.cone((0, 0.2, 0.0), (0, 0.04, 0.0), 0.15, 0.2, k=0.04)                   # jupe
        for s in (-1, 1):
            p.cone((s * 0.07, 0.06, 0.0), (s * 0.075, -0.13, 0.02), 0.04, 0.035, mat="Light", k=0.02)
            p.ell((s * 0.075, -0.16, 0.04), (0.045, 0.035, 0.07), mat="Dark", k=0.02)
    else:
        for s in (-1, 1):
            p.cone((s * 0.075, 0.16, 0.0), (s * 0.08, -0.12, 0.02), 0.06, 0.05, mat="Dark", k=0.03)
            p.ell((s * 0.08, -0.15, 0.05), (0.05, 0.035, 0.08), mat="Accent", k=0.02)
    hair(p)
    hc = (0, 0.72, 0.0)
    for s in (-1, 1): p.eye(hc, (s * 0.36, -0.05, 1.0), r=0.052, sink=0.5)
    for s in (-1, 1): p.ell((s * 0.15, 0.64, 0.22), (0.04, 0.02, 0.01), mat="Second", k=0.005)   # joues
    p.stitch((0, 0.6, 0.0), [(-0.03, 0.0), (0, -0.012), (0.03, 0.0)], thick=0.006)
    p.tag((0, 0.25, 0), (0.6, -0.2, -0.75), size=(0.08, 0.1, 0.01))
    return p.build()


def hair_short(p):        # coupe courte, raie sur le côté
    p.ell((0, 0.82, -0.02), (0.29, 0.22, 0.27), mat="Second", k=0.03)
    p.ell((0.05, 0.86, 0.16), (0.24, 0.09, 0.12), R=(0, 0, -10), mat="Second", k=0.03)
    p.ell((0, 0.72, -0.12), (0.27, 0.2, 0.17), mat="Second", k=0.03)


def hair_swept(p):        # cheveux relevés en arrière (style scène)
    p.ell((0, 0.84, -0.04), (0.28, 0.2, 0.27), mat="Second", k=0.03)
    p.ell((0, 0.98, 0.07), (0.17, 0.11, 0.17), R=(-25, 0, 0), mat="Second", k=0.04)
    p.ell((0, 0.72, -0.13), (0.27, 0.19, 0.16), mat="Second", k=0.03)


def hair_mullet(p):       # mi-long dégradé avec mèches sur le front
    p.ell((0, 0.83, -0.03), (0.3, 0.22, 0.28), mat="Second", k=0.03)
    p.ell((0, 0.6, -0.14), (0.25, 0.22, 0.13), mat="Second", k=0.04)
    for x in (-0.1, 0.0, 0.1): p.cone((x, 0.92, 0.12), (x * 1.3, 0.8, 0.22), 0.06, 0.02, mat="Second", k=0.02)


def hair_long(p):         # longs cheveux lisses
    p.ell((0, 0.83, -0.02), (0.3, 0.22, 0.28), mat="Second", k=0.03)
    p.ell((0, 0.88, 0.17), (0.25, 0.08, 0.1), mat="Second", k=0.02)
    p.ell((0, 0.5, -0.12), (0.29, 0.36, 0.15), mat="Second", k=0.04)
    for s in (-1, 1): p.ell((s * 0.24, 0.55, 0.05), (0.06, 0.25, 0.08), mat="Second", k=0.03)


def hair_twin(p):         # couettes
    p.ell((0, 0.83, -0.02), (0.29, 0.22, 0.27), mat="Second", k=0.03)
    p.ell((0, 0.88, 0.17), (0.24, 0.08, 0.1), mat="Second", k=0.02)
    for s in (-1, 1):
        p.chain([(s * 0.25, 0.88, -0.05), (s * 0.38, 0.75, -0.08), (s * 0.4, 0.55, -0.06), (s * 0.36, 0.38, -0.04)], 0.08, 0.04, mat="Second", k=0.03)
        p.ell((s * 0.25, 0.88, -0.05), (0.04, 0.04, 0.04), mat="Accent", k=0.01)


def hair_bob(p):          # carré avec frange
    p.ell((0, 0.82, -0.05), (0.31, 0.23, 0.27), mat="Second", k=0.03)
    p.ell((0, 0.89, 0.15), (0.26, 0.07, 0.1), mat="Second", k=0.02)
    p.ell((0, 0.64, -0.09), (0.31, 0.17, 0.22), mat="Second", k=0.03)


def hair_spiky(p):        # épis dressés
    p.ell((0, 0.83, -0.03), (0.29, 0.2, 0.27), mat="Second", k=0.03)
    p.ell((0, 0.72, -0.13), (0.27, 0.19, 0.16), mat="Second", k=0.03)
    for x, z in ((-0.16, 0.05), (-0.05, 0.08), (0.07, 0.07), (0.17, 0.02), (-0.1, -0.1), (0.1, -0.1), (0.0, -0.02)):
        p.cone((x * 0.7, 0.9, z * 0.7), (x * 1.2, 1.08, z * 1.2), 0.08, 0.015, mat="Second", k=0.03)


def hair_curtain(p):      # raie au milieu, mèches rideau
    p.ell((0, 0.84, -0.03), (0.3, 0.21, 0.28), mat="Second", k=0.03)
    p.ell((0, 0.7, -0.12), (0.28, 0.21, 0.17), mat="Second", k=0.03)
    for s in (-1, 1): p.ell((s * 0.12, 0.83, 0.18), (0.13, 0.1, 0.07), R=(0, 0, s * 25), mat="Second", k=0.02)


def hair_beanie(p):       # bonnet (Dark) avec mèches qui dépassent
    p.ell((0, 0.86, -0.02), (0.3, 0.2, 0.29), mat="Dark", k=0.02)
    p.ell((0, 0.79, 0.0), (0.305, 0.05, 0.295), mat="Accent", k=0.01)
    p.ell((0, 0.7, -0.14), (0.26, 0.15, 0.14), mat="Second", k=0.03)
    for x in (-0.12, 0.0, 0.12): p.cone((x, 0.78, 0.2), (x * 1.2, 0.7, 0.25), 0.045, 0.015, mat="Second", k=0.02)


def hair_undercut(p):     # dessus long rabattu, côtés rasés
    p.ell((0, 0.9, 0.0), (0.24, 0.14, 0.26), mat="Second", k=0.03)
    p.ell((0.08, 0.9, 0.18), (0.2, 0.08, 0.1), R=(0, 0, -20), mat="Second", k=0.02)
    p.ell((0, 0.74, -0.14), (0.25, 0.17, 0.13), mat="Second", k=0.03)


def hair_ponytail(p):     # queue de cheval haute
    p.ell((0, 0.83, -0.03), (0.29, 0.22, 0.27), mat="Second", k=0.03)
    p.ell((0, 0.88, 0.17), (0.24, 0.07, 0.09), mat="Second", k=0.02)
    p.ell((0, 0.92, -0.25), (0.05, 0.05, 0.05), mat="Accent", k=0.01)
    p.chain([(0, 0.92, -0.26), (0, 0.85, -0.38), (0, 0.65, -0.4), (0, 0.45, -0.34)], 0.08, 0.035, mat="Second", k=0.03)


def hair_buns(p):         # deux chignons
    p.ell((0, 0.83, -0.02), (0.29, 0.22, 0.27), mat="Second", k=0.03)
    p.ell((0, 0.88, 0.17), (0.24, 0.07, 0.09), mat="Second", k=0.02)
    for sx in (-1, 1): p.ell((sx * 0.2, 1.0, -0.04), (0.1, 0.1, 0.1), mat="Second", k=0.03)


def hair_wavy(p):         # longs cheveux ondulés
    p.ell((0, 0.83, -0.02), (0.31, 0.22, 0.28), mat="Second", k=0.03)
    p.ell((0.04, 0.88, 0.17), (0.24, 0.08, 0.1), R=(0, 0, -12), mat="Second", k=0.02)
    for sx in (-1, 1):
        p.chain([(sx * 0.24, 0.75, -0.02), (sx * 0.3, 0.6, 0.0), (sx * 0.26, 0.45, 0.02), (sx * 0.31, 0.3, 0.0)], 0.09, 0.06, mat="Second", k=0.04)
    p.ell((0, 0.5, -0.13), (0.3, 0.33, 0.14), mat="Second", k=0.04)


def outfit_coat(p):       # long manteau de scène (pans)
    for sx in (-1, 1): p.ell((sx * 0.11, 0.12, -0.02), (0.08, 0.16, 0.1), mat="Main", k=0.03)


def outfit_hood(p):       # sweat à capuche
    p.ell((0, 0.45, -0.11), (0.15, 0.08, 0.08), mat="Main", k=0.03)


for _n, _h, _g, _o in (("idol_boy_a", hair_short, False, None), ("idol_boy_b", hair_swept, False, None), ("idol_boy_c", hair_mullet, False, None),
                   ("idol_boy_d", hair_spiky, False, outfit_coat), ("idol_boy_e", hair_curtain, False, outfit_hood),
                   ("idol_boy_f", hair_beanie, False, outfit_hood), ("idol_boy_g", hair_undercut, False, outfit_coat),
                   ("idol_girl_a", hair_long, True, None), ("idol_girl_b", hair_twin, True, None), ("idol_girl_c", hair_bob, True, None),
                   ("idol_girl_d", hair_ponytail, True, None), ("idol_girl_e", hair_buns, True, None), ("idol_girl_f", hair_wavy, True, None)):
    def _mk(n=_n, h=_h, g=_g, o=_o): return idol(n, h, g, o)
    _mk.__name__ = _n
    model(_mk)


@model
def kpop_lightstick():
    m = Model("kpop_lightstick", uv=2.0)
    m.add(lathe([(0.0, 0.0), (0.05, 0.0), (0.055, 0.04), (0.045, 0.45), (0.06, 0.5), (0.0, 0.5)], seg=18, mat="Dark", smooth=True))
    m.add(rbox((0.07, 0.04, 0.02), 0.4, mat="Accent"), (0, 0.32, 0.045))
    m.add(lathe([(0.0, 0.5), (0.09, 0.5), (0.12, 0.54), (0.06, 0.58), (0.0, 0.58)], seg=18, mat="Accent", smooth=True))
    m.add(sphere(0.2, seg=20, rings=14, mat="Main"), (0, 0.76, 0))                 # globe lumineux
    m.add(torus(0.205, 0.015, seg=24, tube=6, mat="Accent"), (0, 0.76, 0), R=rot(90, 0, 0))
    return m.finish()


@model
def kpop_album():
    m = Model("kpop_album", uv=1.0)
    m.add(rbox((0.8, 0.8, 0.14), e=0.05, mat="Main"))
    m.add(rbox((0.6, 0.6, 0.01), e=0.2, mat="Light"), (0, 0.04, 0.07))
    m.add(rbox((0.82, 0.1, 0.15), e=0.1, mat="Accent"), (0, -0.3, 0))
    m.add(sphere(0.12, seg=14, rings=10, mat="Second"), (0, 0.06, 0.08), S=(1, 1, 0.15))
    return m.finish()


# ------------------------------------------------------------------ confiseries (machine « sweets »)
@model
def sweet_lollipop():
    m = Model("sweet_lollipop", uv=2.0)
    m.add(cylinder(0.025, 0.025, 0.6, seg=10, mat="Light"), (0, -0.6, 0))
    m.add(cylinder(0.38, 0.38, 0.12, seg=36, mat="Main"), (0, 0, -0.06), R=rot(90, 0, 0))
    for zf in (0.06, -0.06):
        for k, r in enumerate((0.33, 0.24, 0.15, 0.07)):
            m.add(torus(r, 0.03, seg=36, tube=6, mat="Second" if k % 2 == 0 else "Light"), (0, 0.0, zf), R=rot(90, 0, 0))
    m.add(rbox((0.1, 0.08, 0.1), 0.3, mat="Accent"), (0, -0.38, 0))
    return m.finish()


@model
def sweet_chocolate():
    m = Model("sweet_chocolate", uv=1.0)
    m.add(rbox((0.5, 1.0, 0.1), e=0.08, mat="Main"))
    m.add(rbox((0.52, 0.18, 0.11), e=0.15, mat="Light"), (0, 0.42, 0))
    m.add(rbox((0.42, 0.3, 0.012), e=0.2, mat="Accent"), (0, 0.0, 0.052))
    for i in range(3): m.add(rbox((0.12, 0.08, 0.02), 0.3, mat="Second"), (-0.12 + i * 0.12, -0.28, 0.05))
    return m.finish()


@model
def sweet_soda():
    m = Model("sweet_soda", uv=2.0)
    m.add(lathe([(0.0, 0.0), (0.2, 0.0), (0.23, 0.04), (0.23, 0.82), (0.19, 0.9), (0.0, 0.9)], seg=24, mat="Main", smooth=True))
    m.add(cylinder(0.232, 0.232, 0.22, seg=24, mat="Accent"), (0, 0.35, 0))
    m.add(cylinder(0.18, 0.18, 0.012, seg=20, mat="Light"), (0, 0.9, 0))
    m.add(rbox((0.06, 0.012, 0.1), 0.4, mat="Light"), (0, 0.912, 0.04))
    return m.finish()


@model
def sweet_gummy():
    p = Plush("sweet_gummy")
    p.ell((0, 0.35, 0), (0.3, 0.33, 0.24), k=0.1)
    p.ell((0, 0.78, 0.02), (0.26, 0.23, 0.22), k=0.12)
    for s in (-1, 1):
        p.ell((s * 0.19, 0.97, 0.0), (0.085, 0.085, 0.06), k=0.05)
        p.cone((s * 0.2, 0.5, 0.02), (s * 0.32, 0.38, 0.1), 0.085, 0.075, k=0.07)
        p.cone((s * 0.14, 0.15, 0.03), (s * 0.17, 0.05, 0.18), 0.1, 0.095, k=0.06)
    return p.build()


@model
def sweet_donut():
    p = Plush("sweet_donut")
    R, r = 0.32, 0.16
    def torus_f(P):
        q = np.stack([np.linalg.norm(P[..., [0, 2]], axis=-1) - R, P[..., 1]], -1)
        return np.linalg.norm(q, axis=-1) - r
    p.prims.append((torus_f, "Main", 0.05, np.zeros(3), R + r))
    def icing(P):
        q = np.stack([np.linalg.norm(P[..., [0, 2]], axis=-1) - R, P[..., 1] - 0.05], -1)
        return np.maximum(np.linalg.norm(q, axis=-1) - (r * 0.98), 0.02 - P[..., 1])
    p.prims.append((icing, "Second", 0.01, np.array([0, 0.05, 0]), R + r))
    rng = np.random.default_rng(5)
    for i in range(26):
        a = rng.uniform(0, 2 * math.pi); rr = R + rng.uniform(-0.1, 0.1)
        x, z = math.cos(a) * rr, math.sin(a) * rr
        y = 0.05 + math.sqrt(max(0.0, (r * 0.98) ** 2 - (rr - R) ** 2)) + 0.004
        b = rng.uniform(0, 2 * math.pi)
        p.solid(tube([(x - 0.02 * math.cos(b), y, z - 0.02 * math.sin(b)), (x + 0.02 * math.cos(b), y, z + 0.02 * math.sin(b))], 0.008, seg=6,
                     mat=("Accent", "Light", "Dark")[i % 3]))
    return p.build()


# ------------------------------------------------------------------ monstres doudous, squishies, porte-clés, consoles
@model
def monster_doll():
    """Petit monstre en peluche (design original) : longues oreilles pointues, grand sourire plein de dents, gros yeux."""
    p = Plush("monster_doll")
    p.ell((0, 0.32, 0), (0.24, 0.3, 0.21), k=0.08)
    p.ell((0, 0.76, 0.02), (0.3, 0.26, 0.26), k=0.1)
    p.ell((0, 0.68, 0.2), (0.17, 0.1, 0.08), mat="Light", k=0.03)            # visage clair
    for s in (-1, 1):
        p.ell((s * 0.12, 1.12, -0.02), (0.07, 0.26, 0.05), R=(0, 0, -s * 14), k=0.05)
        p.ell((s * 0.125, 1.1, 0.02), (0.04, 0.2, 0.02), R=(0, 0, -s * 14), mat="Accent", k=0.01)
        p.cone((s * 0.18, 0.44, 0.03), (s * 0.27, 0.22, 0.12), 0.07, 0.06, k=0.05)
        p.cone((s * 0.11, 0.1, 0.03), (s * 0.13, 0.0, 0.16), 0.085, 0.08, k=0.05)
    hc = (0, 0.76, 0.02)
    for s in (-1, 1): p.eye(hc, (s * 0.32, 0.15, 1.0), r=0.065, sink=0.5)
    mp, mn = p.surface((0, 0.66, 0.0), (0, -0.05, 1.0))
    p.solid(rbox((0.2, 0.05, 0.02), 0.6, mat="Dark"), mp - mn * 0.004)
    for i in range(9):                                                         # rangée de petites dents pointues
        x = -0.08 + i * 0.02
        tp, tn = p.surface((x, 0.67, 0.0), (x, -0.05, 1.0))
        p.solid(sphere((0.009, 0.016, 0.006), seg=6, rings=4, mat="Tag"), tp + tn * 0.006 + np.array([0, -0.004, 0]))
    p.tag((0, 0.2, 0), (0.7, -0.2, -0.7), size=(0.08, 0.1, 0.01))
    return p.build()


@model
def squishy_bun():
    """Squishy (mousse ultra-moelleuse) en forme de brioche kawaii."""
    p = Plush("squishy_bun")
    p.ell((0, 0.3, 0), (0.45, 0.3, 0.42), k=0.1)
    p.ell((0, 0.5, 0), (0.33, 0.12, 0.3), mat="Second", k=0.08)
    hc = (0, 0.3, 0)
    for s in (-1, 1): p.eye(hc, (s * 0.35, 0.05, 1.0), r=0.05, sink=0.55)
    for s in (-1, 1):
        bp, bn = p.surface(hc, (s * 0.55, -0.1, 1.0))
        p.solid(sphere((0.06, 0.03, 0.01), seg=8, rings=5, mat="Accent"), bp + bn * 0.002)
    p.stitch(hc, [(-0.04, -0.03), (0, -0.05), (0.04, -0.03)], thick=0.007)
    return p.build()


@model
def squishy_peach():
    p = Plush("squishy_peach")
    p.ell((0, 0.42, 0), (0.4, 0.42, 0.38), k=0.1)
    p.ell((0, 0.85, -0.02), (0.06, 0.06, 0.04), mat="Dark", k=0.04)
    p.ell((0.12, 0.88, 0.0), (0.18, 0.04, 0.09), R=(0, 0, 20), mat="Second", k=0.02)   # feuille
    hc = (0, 0.42, 0)
    for s in (-1, 1): p.eye(hc, (s * 0.3, 0.0, 1.0), r=0.045, sink=0.55)
    p.stitch(hc, [(-0.035, -0.07), (0, -0.09), (0.035, -0.07)], thick=0.007)
    return p.build()


@model
def keychain_star():
    """Porte-clés : petite étoile en peluche + anneau et chaînette métalliques."""
    p = Plush("keychain_star", res=40)
    for k in range(5):
        a = math.radians(90 + k * 72)
        p.cone((0, 0.4, 0), (math.cos(a) * 0.36, 0.4 + math.sin(a) * 0.36, 0), 0.17, 0.05, k=0.06)
    p.ell((0, 0.4, 0), (0.2, 0.2, 0.13), k=0.06)
    hc = (0, 0.4, 0)
    for s in (-1, 1): p.eye(hc, (s * 0.35, 0.1, 1.0), r=0.04, sink=0.5)
    p.stitch(hc, [(-0.03, -0.04), (0, -0.055), (0.03, -0.04)], thick=0.006)
    p.solid(torus(0.03, 0.008, seg=14, tube=6, mat="Metal"), (0, 0.79, 0), R=rot(90, 0, 0))
    for i in range(4): p.solid(torus(0.018, 0.006, seg=10, tube=5, mat="Metal"), (0, 0.84 + i * 0.035, 0), R=rot(90, 90 * (i % 2), 0))
    p.solid(torus(0.09, 0.012, seg=24, tube=6, mat="Metal"), (0, 1.05, 0), R=rot(90, 0, 0))
    return p.build()


@model
def keychain_heart():
    p = Plush("keychain_heart", res=40)
    for s in (-1, 1): p.ell((s * 0.14, 0.5, 0), (0.2, 0.2, 0.13), k=0.06)
    p.cone((0, 0.42, 0), (0, 0.12, 0), 0.24, 0.03, k=0.1)
    hc = (0, 0.42, 0)
    for s in (-1, 1): p.eye(hc, (s * 0.3, 0.15, 1.0), r=0.04, sink=0.5)
    p.solid(torus(0.03, 0.008, seg=14, tube=6, mat="Metal"), (0, 0.74, 0), R=rot(90, 0, 0))
    for i in range(4): p.solid(torus(0.018, 0.006, seg=10, tube=5, mat="Metal"), (0, 0.79 + i * 0.035, 0), R=rot(90, 90 * (i % 2), 0))
    p.solid(torus(0.09, 0.012, seg=24, tube=6, mat="Metal"), (0, 1.0, 0), R=rot(90, 0, 0))
    return p.build()


# consoles génériques (aucune marque, aucun design copié) : boîte avec visuel + console
@model
def console_box():
    m = Model("console_box", uv=1.0)
    m.add(rbox((1.0, 0.75, 0.28), e=0.04, mat="Main"))
    m.add(rbox((0.7, 0.42, 0.012), e=0.12, mat="Light"), (-0.05, 0.05, 0.142))
    m.add(rbox((0.24, 0.5, 0.06), e=0.3, mat="Second"), (-0.05, 0.05, 0.15))      # console dessinée sur la boîte
    m.add(rbox((1.02, 0.08, 0.29), e=0.1, mat="Accent"), (0, -0.33, 0))
    m.add(rbox((0.18, 0.1, 0.012), e=0.3, mat="Accent"), (0.36, 0.26, 0.142))
    return m.finish()


@model
def console_handheld_box():
    m = Model("console_handheld_box", uv=1.0)
    m.add(rbox((1.0, 0.55, 0.2), e=0.05, mat="Main"))
    m.add(rbox((0.56, 0.3, 0.02), e=0.15, mat="Dark"), (0, 0.02, 0.1))
    m.add(rbox((0.42, 0.22, 0.012), e=0.1, mat="Light"), (0, 0.02, 0.111))
    for s in (-1, 1): m.add(rbox((0.12, 0.3, 0.03), e=0.4, mat="Second" if s < 0 else "Accent"), (s * 0.34, 0.02, 0.1))
    return m.finish()


LOD_SOURCES = ["plush_bear", "plush_bunny", "plush_cat", "plush_dog", "plush_penguin", "plush_frog", "plush_duck", "plush_unicorn",
               "plush_dino", "plush_panda", "plush_octopus", "plush_fox", "idol_boy_a", "idol_boy_b", "idol_boy_c", "idol_girl_a",
               "idol_girl_b", "idol_girl_c", "idol_boy_d", "idol_boy_e", "idol_boy_f", "idol_boy_g", "idol_girl_d", "idol_girl_e", "idol_girl_f",
               "sweet_gummy", "sweet_donut", "monster_doll", "squishy_bun", "squishy_peach"]


def _register_lods():
    """Versions allégées (LOD) des modèles lisses : utilisées par Unity à distance (téléphone)."""
    from make_models import MODELS
    for n in LOD_SOURCES:
        base = MODELS[n]
        def _lod(base=base, n=n):
            old = Plush.default_res; Plush.default_res = 22
            try:
                m = base()
            finally:
                Plush.default_res = old
            m.name = n + "_lod"
            return m
        _lod.__name__ = n + "_lod"
        model(_lod)


_register_lods()


PLUSH = [n for n in ("plush_bear", "plush_bunny", "plush_cat", "plush_dog", "plush_penguin", "plush_frog", "plush_duck",
                     "plush_unicorn", "plush_dino", "plush_panda", "plush_octopus", "plush_fox", "prize_box",
                     "idol_boy_a", "idol_boy_b", "idol_boy_c", "idol_boy_d", "idol_boy_e", "idol_boy_f", "idol_boy_g",
                     "idol_girl_a", "idol_girl_b", "idol_girl_c", "idol_girl_d", "idol_girl_e", "idol_girl_f", "kpop_lightstick", "kpop_album",
                     "sweet_lollipop", "sweet_chocolate", "sweet_soda", "sweet_gummy", "sweet_donut",
                     "monster_doll", "squishy_bun", "squishy_peach", "keychain_star", "keychain_heart", "console_box", "console_handheld_box")]

if __name__ == "__main__":
    from make_models import MODELS
    from PIL import Image, ImageDraw
    PAL = {"Metal": (200, 200, 210), "Main": (200, 140, 90), "Light": (245, 230, 210), "Second": (120, 80, 50), "Dark": (40, 30, 30), "Eye": (15, 15, 20),
           "Accent": (255, 160, 40), "Tag": (250, 250, 250)}
    W = 260; cols = 6; rows = math.ceil(len(PLUSH) / cols)
    img = Image.new("RGB", (cols * W, rows * W), (24, 24, 36))
    for n, name in enumerate(PLUSH):
        mdl = MODELS[name]()
        ang = math.radians(30); el = math.radians(12)
        ry = np.array([[math.cos(ang), 0, math.sin(ang)], [0, 1, 0], [-math.sin(ang), 0, math.cos(ang)]])
        rx = np.array([[1, 0, 0], [0, math.cos(el), -math.sin(el)], [0, math.sin(el), math.cos(el)]])
        R = rx @ ry
        tris = []
        L = np.array([-0.4, 0.6, 0.7]); L = L / np.linalg.norm(L)
        for prt in mdl.parts:
            V = prt.V @ R.T; Nn = prt.N @ R.T
            base = PAL.get(prt.mat, (200, 200, 200))
            for f in prt.F:
                a, b, c = V[f]
                if np.cross(b - a, c - a)[2] <= 0: continue
                vn = Nn[f].mean(0); vn /= (np.linalg.norm(vn) + 1e-9)
                sh = 0.3 + 0.7 * max(0.0, vn @ L) + (0.25 if prt.mat in ("Eye",) and vn @ L > 0.9 else 0)
                tris.append(((a[2] + b[2] + c[2]) / 3, [a, b, c], tuple(min(255, int(x * sh)) for x in base)))
        tris.sort(key=lambda t: t[0])
        allv = np.vstack([q.V @ R.T for q in mdl.parts]); ext = (allv.max(0) - allv.min(0)).max(); ctr = (allv.max(0) + allv.min(0)) / 2
        s = (W * 0.82) / ext
        d = ImageDraw.Draw(img); ox, oy = (n % cols) * W, (n // cols) * W
        for _, pts, col in tris:
            d.polygon([(ox + W / 2 + (q[0] - ctr[0]) * s, oy + W / 2 - (q[1] - ctr[1]) * s) for q in pts], fill=col)
        d.text((ox + 8, oy + 6), "%s  %d tri" % (name, mdl.triangles()), fill=(200, 200, 220))
    img.save(sys.argv[1] if len(sys.argv) > 1 else "plush_preview.png")
