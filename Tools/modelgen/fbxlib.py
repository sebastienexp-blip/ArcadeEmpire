"""Mini bibliothèque : maillages procéduraux (numpy) avec UV + export FBX ASCII 7.4.
Un modèle = plusieurs parties, chacune avec un nom de matériau (slot). Les UV sont en mètres / `tile`."""
import math
import numpy as np


def _norm(v):
    l = np.linalg.norm(v, axis=-1, keepdims=True)
    l[l == 0] = 1
    return v / l


class Part:
    def __init__(self, V, N, F, mat, UV=None):
        self.V = np.asarray(V, float); self.N = np.asarray(N, float)
        self.F = np.asarray(F, int); self.mat = mat
        self.UV = np.zeros((len(self.V), 2)) if UV is None else np.asarray(UV, float)
        self._fix_winding()

    def _fix_winding(self):
        a, b, c = (self.V[self.F[:, i]] for i in range(3))
        gn = np.cross(b - a, c - a)
        vn = self.N[self.F[:, 0]] + self.N[self.F[:, 1]] + self.N[self.F[:, 2]]
        flip = (gn * vn).sum(1) < 0
        self.F[flip] = self.F[flip][:, ::-1]

    def transformed(self, M=None, t=(0, 0, 0)):
        M = np.eye(3) if M is None else np.asarray(M, float)
        V = self.V @ M.T + np.asarray(t, float)
        N = _norm(self.N @ np.linalg.inv(M))
        return Part(V, N, self.F.copy(), self.mat, self.UV.copy())


def rot(ax=0, ay=0, az=0):
    ax, ay, az = map(math.radians, (ax, ay, az))
    rx = np.array([[1, 0, 0], [0, math.cos(ax), -math.sin(ax)], [0, math.sin(ax), math.cos(ax)]])
    ry = np.array([[math.cos(ay), 0, math.sin(ay)], [0, 1, 0], [-math.sin(ay), 0, math.cos(ay)]])
    rz = np.array([[math.cos(az), -math.sin(az), 0], [math.sin(az), math.cos(az), 0], [0, 0, 1]])
    return rz @ ry @ rx


def _grid(P, tile=1.0):
    """Surface paramétrique (R, S, 3) → (V, N, F, UV). Normales lisses (moyenne par position), UV en longueurs d'arc."""
    R, S, _ = P.shape
    V = P.reshape(-1, 3)
    F = []
    for i in range(R - 1):
        for j in range(S - 1):
            a = i * S + j; b = a + 1; c = a + S + 1; d = a + S
            F += [[a, b, c], [a, c, d]]
    F = np.array(F)
    acc = {}
    key = lambda p: tuple(np.round(p, 5))
    keys = [key(p) for p in V]
    for f in F:
        n = np.cross(V[f[1]] - V[f[0]], V[f[2]] - V[f[0]])
        for k in f: acc[keys[k]] = acc.get(keys[k], 0) + n
    N = _norm(np.array([acc[k] for k in keys]))
    area = np.array([np.linalg.norm(np.cross(V[f[1]] - V[f[0]], V[f[2]] - V[f[0]])) for f in F])
    seg_v = np.linalg.norm(np.diff(P, axis=0), axis=2).mean(axis=1)
    vv = np.concatenate([[0], np.cumsum(seg_v)]) / tile
    UV = np.zeros((R, S, 2))
    for i in range(R):
        ring = np.linalg.norm(np.diff(P[i], axis=0), axis=1)
        UV[i, :, 0] = np.concatenate([[0], np.cumsum(ring)]) / tile
        UV[i, :, 1] = vv[i]
    return V, N, F[area > 1e-9], UV.reshape(-1, 2)


def _part(P, mat, tile=1.0):
    V, N, F, UV = _grid(P, tile)
    return Part(V, N, F, mat, UV)


def superellipsoid(r, e=1.0, seg=14, rings=9, t0=0.0, t1=math.pi, mat="Main", tile=1.0):
    rx, ry, rz = r
    ts = np.linspace(t0, t1, rings + 1); ps = np.linspace(0, 2 * math.pi, seg + 1)
    sp = lambda x, k: np.sign(x) * np.abs(x) ** k
    P = np.zeros((rings + 1, seg + 1, 3))
    for i, t in enumerate(ts):
        for j, p in enumerate(ps):
            P[i, j] = [rx * sp(math.sin(t), e) * sp(math.cos(p), e), ry * sp(math.cos(t), e), rz * sp(math.sin(t), e) * sp(math.sin(p), e)]
    return _part(P, mat, tile)


def sphere(r, **kw):
    r = (r, r, r) if np.isscalar(r) else r
    return superellipsoid(r, **kw)


def rbox(size, e=0.16, seg=20, rings=12, mat="Main", tile=1.0):
    """Boîte à arêtes arrondies (superellipsoïde aplatie) : faces quasi planes, bords adoucis."""
    return superellipsoid((size[0] / 2, size[1] / 2, size[2] / 2), e, seg, rings, mat=mat, tile=tile)


def lathe(profile, seg=24, mat="Main", smooth=False, tile=1.0):
    """Surface de révolution (axe Y) d'un profil [(r, y), ...]. smooth=False : arêtes vives entre segments du profil."""
    ps = np.linspace(0, 2 * math.pi, seg + 1)
    rings = lambda pts: np.array([[[r * math.cos(p), y, r * math.sin(p)] for p in ps] for r, y in pts])
    if smooth:
        return _part(rings(profile), mat, tile)
    parts = [_part(rings(profile[k:k + 2]), mat, tile) for k in range(len(profile) - 1) if profile[k] != profile[k + 1]]
    return merge(parts, mat)


def merge(parts, mat):
    V, N, F, UV, off = [], [], [], [], 0
    for p in parts:
        V.append(p.V); N.append(p.N); F.append(p.F + off); UV.append(p.UV); off += len(p.V)
    return Part(np.vstack(V), np.vstack(N), np.vstack(F), mat, np.vstack(UV))


def cylinder(r0, r1, h, seg=14, caps=True, mat="Main", tile=1.0):
    """Cylindre/cône tronqué, axe Y, de y=0 à y=h, avec disques."""
    body = lathe([(r0, 0), (r1, h)], seg, mat, tile=tile)
    parts = [body]
    ps = np.linspace(0, 2 * math.pi, seg + 1)
    for r, y, ny in ((r0, 0, -1), (r1, h, 1)):
        if not caps or r <= 1e-6: continue
        c = np.array([[0, y, 0]] + [[r * math.cos(p), y, r * math.sin(p)] for p in ps[:-1]])
        UV = np.column_stack([c[:, 0], c[:, 2]]) / tile
        F = np.array([[0, 1 + k, 1 + (k + 1) % seg] for k in range(seg)])
        parts.append(Part(c, np.tile([0, ny, 0], (len(c), 1)), F, mat, UV))
    return merge(parts, mat)


def box(s, mat="Main", tile=1.0):
    h = [s[0] / 2, s[1] / 2, s[2] / 2]
    V, N, F, UV = [], [], [], []
    for axis in range(3):
        for sign in (-1, 1):
            n = np.zeros(3); n[axis] = sign
            ua, va = (axis + 1) % 3, (axis + 2) % 3
            base = len(V)
            for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                p = np.zeros(3); p[axis] = sign * h[axis]; p[ua] = a * h[ua]; p[va] = b * h[va]
                V.append(p); N.append(n); UV.append([(a * h[ua] + h[ua]) / tile, (b * h[va] + h[va]) / tile])
            F += [[base, base + 1, base + 2], [base, base + 2, base + 3]]
    return Part(V, N, F, mat, UV)


def quad(w, h, mat="Main", flip=False):
    """Rectangle w x h dans le plan XY, normale +Z (flip : -Z), UV 0..1 (décalcomanie / enseigne)."""
    V = [[-w / 2, -h / 2, 0], [w / 2, -h / 2, 0], [w / 2, h / 2, 0], [-w / 2, h / 2, 0]]
    UV = [[0, 0], [1, 0], [1, 1], [0, 1]]
    if flip: UV = [[1, 0], [0, 0], [0, 1], [1, 1]]
    n = [0, 0, -1 if flip else 1]
    return Part(V, [n] * 4, [[0, 1, 2], [0, 2, 3]], mat, UV)


def torus(R, r, seg=16, tube=8, a0=0, a1=2 * math.pi, mat="Main", tile=1.0):
    us = np.linspace(a0, a1, seg + 1); vs = np.linspace(0, 2 * math.pi, tube + 1)
    P = np.array([[[(R + r * math.cos(v)) * math.cos(u), r * math.sin(v), (R + r * math.cos(v)) * math.sin(u)] for v in vs] for u in us])
    return _part(P, mat, tile)


def tube(points, radius, seg=8, mat="Main", tile=1.0):
    """Tube lisse le long d'une polyligne (câbles, tuyaux, branches courbes). radius : nombre ou liste."""
    pts = np.asarray(points, float); n = len(pts)
    rad = np.full(n, radius, float) if np.isscalar(radius) else np.asarray(radius, float)
    P = np.zeros((n, seg + 1, 3))
    for i in range(n):
        d = pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]
        d = d / (np.linalg.norm(d) + 1e-12)
        a = np.array([0, 0, 1.0]) if abs(d[2]) < 0.9 else np.array([1.0, 0, 0])
        u = _norm(np.cross(d, a)); v = np.cross(d, u)
        for j, ang in enumerate(np.linspace(0, 2 * math.pi, seg + 1)):
            P[i, j] = pts[i] + rad[i] * (math.cos(ang) * u + math.sin(ang) * v)
    return _part(P, mat, tile)


class Model:
    def __init__(self, name, uv=1.0):
        self.name = name; self.parts = []; self.uv = uv   # uv : densité de texture du modèle (multiplie les UV)

    def add(self, part, t=(0, 0, 0), R=None, S=None):
        M = np.eye(3) if R is None else R
        if S is not None: M = M @ np.diag(S)
        p = part.transformed(M, t)
        p.UV = p.UV * self.uv
        self.parts.append(p); return self

    def finish(self, ground=False, keep=False):
        """Recentre sur la boîte englobante (ground : sol centré ; keep : coordonnées d'origine conservées)."""
        V = np.vstack([p.V for p in self.parts])
        lo, hi = V.min(0), V.max(0)
        self.size = hi - lo
        if keep: return self
        c = (lo + hi) / 2
        if ground: c[1] = lo[1]
        for p in self.parts: p.V = p.V - c
        return self

    def triangles(self):
        return sum(len(p.F) for p in self.parts)


COLORS = {"Main": (0.8, 0.8, 0.8), "Dark": (0.1, 0.1, 0.12), "Light": (0.95, 0.95, 0.95), "Accent": (1, 0.8, 0.2),
          "Pot": (0.35, 0.2, 0.12), "Soil": (0.2, 0.12, 0.08)}


def write_fbx(model, path):
    mats = []
    for p in model.parts:
        if p.mat not in mats: mats.append(p.mat)
    V, N, I, M, UV, UI = [], [], [], [], [], []
    off = 0
    for p in model.parts:
        V += p.V.reshape(-1).tolist(); UV += p.UV.reshape(-1).tolist()
        for f in p.F:
            idx = (f + off).tolist()
            I += [idx[0], idx[1], -idx[2] - 1]
            UI += idx
            for k in f: N += p.N[k].tolist()
            M.append(mats.index(p.mat))
        off += len(p.V)
    fmt = lambda a: ",".join(repr(round(float(x), 5)) if isinstance(x, float) else str(x) for x in a)
    gid, mid = 1000001, 1000002
    mat_ids = [2000001 + i for i in range(len(mats))]
    o = ["; FBX 7.4.0 project file\nFBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 7400\n\tCreator: \"ArcadeEmpire modelgen\"\n}"]
    o.append("GlobalSettings:  {\n\tVersion: 1000\n\tProperties70:  {\n"
             "\t\tP: \"UpAxis\", \"int\", \"Integer\", \"\",1\n\t\tP: \"UpAxisSign\", \"int\", \"Integer\", \"\",1\n"
             "\t\tP: \"FrontAxis\", \"int\", \"Integer\", \"\",2\n\t\tP: \"FrontAxisSign\", \"int\", \"Integer\", \"\",1\n"
             "\t\tP: \"CoordAxis\", \"int\", \"Integer\", \"\",0\n\t\tP: \"CoordAxisSign\", \"int\", \"Integer\", \"\",1\n"
             "\t\tP: \"UnitScaleFactor\", \"double\", \"Number\", \"\",100\n\t}\n}")
    o.append("Definitions:  {\n\tVersion: 100\n\tCount: %d\n\tObjectType: \"Model\" {\n\t\tCount: 1\n\t}\n\tObjectType: \"Geometry\" {\n\t\tCount: 1\n\t}\n\tObjectType: \"Material\" {\n\t\tCount: %d\n\t}\n}" % (2 + len(mats) + 1, len(mats)))
    body = ["Objects:  {"]
    body.append("\tGeometry: %d, \"Geometry::%s\", \"Mesh\" {\n\t\tVertices: *%d {\n\t\t\ta: %s\n\t\t}\n\t\tPolygonVertexIndex: *%d {\n\t\t\ta: %s\n\t\t}\n\t\tGeometryVersion: 124\n"
                "\t\tLayerElementNormal: 0 {\n\t\t\tVersion: 101\n\t\t\tName: \"\"\n\t\t\tMappingInformationType: \"ByPolygonVertex\"\n\t\t\tReferenceInformationType: \"Direct\"\n\t\t\tNormals: *%d {\n\t\t\t\ta: %s\n\t\t\t}\n\t\t}\n"
                "\t\tLayerElementUV: 0 {\n\t\t\tVersion: 101\n\t\t\tName: \"UVMap\"\n\t\t\tMappingInformationType: \"ByPolygonVertex\"\n\t\t\tReferenceInformationType: \"IndexToDirect\"\n\t\t\tUV: *%d {\n\t\t\t\ta: %s\n\t\t\t}\n\t\t\tUVIndex: *%d {\n\t\t\t\ta: %s\n\t\t\t}\n\t\t}\n"
                "\t\tLayerElementMaterial: 0 {\n\t\t\tVersion: 101\n\t\t\tName: \"\"\n\t\t\tMappingInformationType: \"ByPolygon\"\n\t\t\tReferenceInformationType: \"IndexToDirect\"\n\t\t\tMaterials: *%d {\n\t\t\t\ta: %s\n\t\t\t}\n\t\t}\n"
                "\t\tLayer: 0 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementNormal\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementUV\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementMaterial\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}\n\t\t}\n\t}"
                % (gid, model.name, len(V), fmt(V), len(I), fmt(I), len(N), fmt(N), len(UV), fmt(UV), len(UI), fmt(UI), len(M), fmt(M)))
    body.append("\tModel: %d, \"Model::%s\", \"Mesh\" {\n\t\tVersion: 232\n\t\tProperties70:  {\n\t\t\tP: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",0,0,0\n\t\t}\n\t\tShading: T\n\t\tCulling: \"CullingOff\"\n\t}" % (mid, model.name))
    for mi, m in zip(mat_ids, mats):
        c = COLORS.get(m, (0.8, 0.8, 0.8))
        body.append("\tMaterial: %d, \"Material::%s\", \"\" {\n\t\tVersion: 102\n\t\tShadingModel: \"lambert\"\n\t\tMultiLayer: 0\n\t\tProperties70:  {\n\t\t\tP: \"DiffuseColor\", \"Color\", \"\", \"A\",%s,%s,%s\n\t\t}\n\t}" % (mi, m, c[0], c[1], c[2]))
    body.append("}")
    o.append("\n".join(body))
    conns = ["Connections:  {", "\tC: \"OO\",%d,0" % mid, "\tC: \"OO\",%d,%d" % (gid, mid)] + ["\tC: \"OO\",%d,%d" % (mi, mid) for mi in mat_ids] + ["}"]
    o.append("\n".join(conns))
    with open(path, "w", newline="\n") as f:
        f.write("\n\n".join(o) + "\n")


def read_fbx_counts(path):
    """Relecture de contrôle : cohérence des tableaux déclarés (tailles, indices, UV, normales)."""
    import re
    t = open(path).read()
    def arr(tag):
        m = re.search(tag + r": \*(\d+) \{\s*a: ([^\}]*)\}", t)
        n = int(m.group(1)); vals = [float(x) for x in m.group(2).replace("\n", "").split(",")]
        assert len(vals) == n, (tag, n, len(vals)); return vals
    v = arr("Vertices"); idx = [int(x) for x in arr("PolygonVertexIndex")]; n = arr("Normals"); mt = [int(x) for x in arr("Materials")]
    uv = arr("UV"); ui = [int(x) for x in arr("UVIndex")]
    nv = len(v) // 3; tris = len(idx) // 3
    assert all(-nv <= i < nv for i in idx), "indice hors limites"
    assert all(idx[k] < 0 for k in range(2, len(idx), 3)) and all(idx[k] >= 0 for k in range(0, len(idx), 3))
    assert len(n) == len(idx) * 3 and len(mt) == tris and len(ui) == len(idx) and len(uv) == nv * 2
    assert all(0 <= i < nv for i in ui)
    assert not any(math.isnan(x) for x in v + n + uv), "NaN"
    return nv, tris, len(set(mt))
