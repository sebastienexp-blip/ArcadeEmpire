"""Textures procédurales tileables (albedo + normal map) -> Assets/_Project/Art/Textures. Usage : python3 make_textures.py"""
import math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Art", "Textures")
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(1234)


def fft_noise(n, beta=2.0, seed=None):
    """Bruit en 1/f^beta tileable, normalisé 0..1."""
    r = np.random.default_rng(seed) if seed is not None else rng
    f = np.fft.fftfreq(n)
    fx, fy = np.meshgrid(f, f)
    amp = 1.0 / np.maximum(np.sqrt(fx ** 2 + fy ** 2), 1.0 / n) ** (beta / 2)
    amp[0, 0] = 0
    z = np.fft.ifft2(amp * np.exp(2j * math.pi * r.random((n, n)))).real
    z -= z.min(); z /= z.max() or 1
    return z


def normal_from_height(h, strength=2.0):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5 * strength * h.shape[0] / 64
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5 * strength * h.shape[0] / 64
    n = np.stack([-dx, dy, np.ones_like(h)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n * 0.5 + 0.5


def save(name, rgb, height=None, strength=2.0):
    a = (np.clip(rgb, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(a).save(os.path.join(OUT, name + ".png"))
    if height is not None:
        Image.fromarray((normal_from_height(height, strength) * 255).astype(np.uint8)).save(os.path.join(OUT, name + "_n.png"))


def gray(v, tint=(1, 1, 1)):
    return np.stack([v * tint[0], v * tint[1], v * tint[2]], -1)


N = 512
x = np.arange(N) / N
X, Y = np.meshgrid(x, x)

# --- Tissu / peluche : tissage + fibres (gris clair, teinté par la couleur du matériau)
weave = 0.5 + 0.25 * np.sin(2 * math.pi * X * 64) * np.sin(2 * math.pi * Y * 64)
fibres = fft_noise(N, 1.2, 1)
h = 0.6 * weave + 0.4 * fibres
save("fabric", gray(0.62 + 0.38 * h), h, 3.0)

# --- Métal brossé
streak = fft_noise(N, 2.0, 2)
brushed = np.mean([np.roll(streak, k, 1) for k in range(0, 40, 4)], axis=0)
h = 0.7 * brushed + 0.3 * fft_noise(N, 1.0, 3)
save("metal", gray(0.62 + 0.3 * h, (0.95, 0.97, 1.0)), h, 0.8)

# --- Peinture sombre de carrosserie (grain fin + rayures légères)
grain = fft_noise(N, 0.6, 4)
sc = np.zeros((N, N))
for _ in range(60):
    y0 = rng.integers(0, N); x0 = rng.integers(0, N); l = rng.integers(20, 120); a = rng.random() * math.pi
    for t in range(l):
        sc[int(y0 + t * math.sin(a)) % N, int(x0 + t * math.cos(a)) % N] = rng.random() * 0.5 + 0.5
sc = np.array(Image.fromarray((sc * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))) / 255.0
h = 0.5 * grain + 0.5 * sc
save("paint", gray(0.55 + 0.25 * grain + 0.35 * sc, (0.95, 0.95, 1.05)), h, 1.0)

# --- Plastique (peau d'orange)
h = fft_noise(N, 0.8, 5)
save("plastic", gray(0.85 + 0.12 * h), h, 0.6)

# --- Caoutchouc / grip
h = 0.5 * fft_noise(N, 0.4, 6) + 0.5 * (0.5 + 0.5 * np.sin(2 * math.pi * X * 48) * np.sin(2 * math.pi * Y * 48) > 0.62)
save("rubber", gray(0.35 + 0.25 * h), h, 2.0)

# --- Or martelé (couleur propre)
cells = np.zeros((N, N))
pts = rng.random((260, 2))
for px, py in pts:
    d = np.minimum(np.abs(X - px), 1 - np.abs(X - px)) ** 2 + np.minimum(np.abs(Y - py), 1 - np.abs(Y - py)) ** 2
    cells = np.maximum(cells, np.clip(1 - np.sqrt(d) * 22, 0, 1))
h = 0.8 * cells + 0.2 * fft_noise(N, 1.5, 7)
v = 0.75 + 0.25 * h
save("gold", np.stack([v * 1.0, v * 0.8, v * 0.28], -1), h, 2.0)

# --- Feutre / moquette du sol de jeu (gris, teinté)
h = 0.6 * fft_noise(N, 0.9, 8) + 0.4 * fft_noise(N, 0.3, 9)
save("felt", gray(0.55 + 0.4 * h), h, 2.2)

# --- Carrelage de salle : joints + usure (gris, teinté par l'ambiance)
tile = np.ones((N, N)); g = 6
tile[:g, :] = 0.35; tile[:, :g] = 0.35
bevel = np.minimum.reduce([np.arange(N)[None, :].repeat(N, 0), np.arange(N)[:, None].repeat(N, 1)]) 
wear = fft_noise(N, 2.2, 10)
h = np.clip(bevel / 18.0, 0, 1) * 0.6 + wear * 0.15
save("tile", gray(0.62 * tile + 0.38 * (0.5 + 0.5 * wear) * tile), h, 2.5)

# --- Mur : plâtre + panneaux
pl = fft_noise(N, 2.5, 11)
panel = np.zeros((N, N)); panel[N // 2 - 3:N // 2 + 3, :] = 1; panel[:, N // 2 - 3:N // 2 + 3] = 1
h = 0.7 * pl + 0.3 * (1 - panel)
save("wall", gray(0.78 + 0.22 * pl - 0.2 * panel), h, 1.2)

# --- Denim / tricot (vêtements)
weave = 0.5 + 0.5 * np.sin(2 * math.pi * (X + Y) * 90) * np.sin(2 * math.pi * (X - Y) * 90)
h = 0.6 * weave + 0.4 * fft_noise(N, 1.0, 12)
save("denim", gray(0.58 + 0.42 * h), h, 2.5)

# --- Peau (légère variation)
h = fft_noise(N, 1.0, 13)
save("skin", gray(0.9 + 0.1 * h), h, 0.4)

# --- Pièce : face (anneau + étoile), carré 256 en vue de dessus pour les UV planaires
S = 256
img = Image.new("RGB", (S, S), (190, 190, 190)); d = ImageDraw.Draw(img)
d.ellipse([6, 6, S - 6, S - 6], outline=(255, 255, 255), width=10)
d.ellipse([34, 34, S - 34, S - 34], outline=(120, 120, 120), width=4)
c = S / 2; star = [(c + (84 if k % 2 == 0 else 36) * math.sin(k * math.pi / 5), c - (84 if k % 2 == 0 else 36) * math.cos(k * math.pi / 5)) for k in range(10)]
d.polygon(star, fill=(255, 255, 255), outline=(110, 110, 110))
img.save(os.path.join(OUT, "coin_face.png"))
hh = np.array(img.convert("L"), float) / 255.0
Image.fromarray((normal_from_height(np.array(Image.fromarray((hh * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))) / 255.0, 6) * 255).astype(np.uint8)).save(os.path.join(OUT, "coin_face_n.png"))


# --- Enseignes lumineuses (albedo = texte sombre sur fond coloré ; émission = même image)
def font(sz):
    for p in ("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", "/usr/share/fonts/dejavu/DejaVuSans-Bold.ttf"):
        if os.path.exists(p): return ImageFont.truetype(p, sz)
    return ImageFont.load_default()


def sign(name, text, sub, c1, c2):
    W, H = 1024, 256
    img = Image.new("RGB", (W, H), (10, 8, 22)); d = ImageDraw.Draw(img)
    for i in range(0, H, 4):
        t = i / H; d.line([(0, i), (W, i)], fill=tuple(int(a * (1 - t) + b * t) * 0 + int(14 + (a * (1 - t) + b * t) * 0.18) for a, b in zip(c1, c2)))
    d.rounded_rectangle([10, 10, W - 10, H - 10], radius=28, outline=c1, width=8)
    d.rounded_rectangle([26, 26, W - 26, H - 26], radius=20, outline=c2, width=3)
    f = font(120); w = d.textlength(text, font=f)
    for off, col in ((6, tuple(int(v * 0.4) for v in c2)), (0, (255, 255, 255))):
        d.text(((W - w) / 2 + off, 46 + off), text, font=f, fill=col)
    f2 = font(44); w2 = d.textlength(sub, font=f2)
    d.text(((W - w2) / 2, 176), sub, font=f2, fill=c1)
    for sx in (70, W - 70):
        sy = 128; pts = [(sx + (34 if k % 2 == 0 else 14) * math.sin(k * math.pi / 5), sy - (34 if k % 2 == 0 else 14) * math.cos(k * math.pi / 5)) for k in range(10)]
        d.polygon(pts, fill=c2)
    img = img.filter(ImageFilter.GaussianBlur(0.6))
    img.save(os.path.join(OUT, name + ".png"))


sign("sign_claw", "CLAW", "GRAB & WIN", (30, 230, 255), (255, 60, 170))
sign("sign_pusher", "COIN PUSHER", "JACKPOT", (255, 70, 170), (255, 220, 40))


# --- Décalcomanie du pupitre
def decal(name, lines):
    W, H = 512, 256
    img = Image.new("RGB", (W, H), (18, 16, 30)); d = ImageDraw.Draw(img)
    d.rounded_rectangle([4, 4, W - 4, H - 4], radius=16, outline=(30, 230, 255), width=4)
    for i, (t, col, sz) in enumerate(lines):
        f = font(sz); w = d.textlength(t, font=f); d.text(((W - w) / 2, 18 + i * 62), t, font=f, fill=col)
    img.save(os.path.join(OUT, name + ".png"))


decal("decal_claw", [("INSERT COIN", (255, 220, 40), 54), ("5 COINS / PLAY", (255, 255, 255), 40), ("JOYSTICK + BUTTON", (30, 230, 255), 34)])
decal("decal_pusher", [("INSERT COIN", (255, 220, 40), 54), ("AIM  < >", (255, 255, 255), 44), ("PUSH THE JACKPOT", (255, 70, 170), 34)])
print(len(os.listdir(OUT)), "fichiers dans", OUT)


# ================= Phase 9 : machines de jeux =================
for nm, text, sub, a, b in (("sign_wheel", "FORTUNE", "SPIN & WIN", (255, 220, 40), (255, 70, 170)),
                            ("sign_slots", "JACKPOT", "777", (255, 70, 90), (255, 220, 40)),
                            ("sign_hammer", "STRENGTH", "HIT THE BELL", (60, 255, 140), (30, 230, 255)),
                            ("sign_basket", "HOOPS", "30 SECONDS", (255, 150, 40), (255, 70, 170)),
                            ("sign_reflex", "REFLEX", "BE QUICK", (30, 230, 255), (180, 90, 255))):
    sign(nm, text, sub, a, b)

# --- Roue de la fortune : 12 secteurs, le secteur 0 commence en haut et suit le sens horaire
WHEEL_VALUES = [2, 5, 2, 10, 2, 5, 25, 2, 5, 2, 10, 100]
WHEEL_COLORS = [(230, 60, 90), (40, 120, 230), (240, 190, 40), (60, 200, 120), (170, 80, 220), (240, 120, 40)]
S = 1024
img = Image.new("RGB", (S, S), (14, 12, 24)); d = ImageDraw.Draw(img)
c = S / 2; R = S / 2 - 6
for i, val in enumerate(WHEEL_VALUES):
    a0 = -90 + i * 30; a1 = a0 + 30
    col = (255, 215, 40) if val >= 100 else WHEEL_COLORS[i % 6]
    d.pieslice([c - R, c - R, c + R, c + R], a0, a1, fill=col, outline=(255, 255, 255), width=5)
    am = math.radians(-90 + (i + 0.5) * 30)
    tx, ty = c + math.cos(am) * R * 0.72, c + math.sin(am) * R * 0.72
    f = font(110 if val < 100 else 90); t = str(val); w = d.textlength(t, font=f)
    tmp = Image.new("RGBA", (260, 160), (0, 0, 0, 0)); td = ImageDraw.Draw(tmp)
    td.text((130 - w / 2, 22), t, font=f, fill=(255, 255, 255)); td.text((130 - w / 2 + 4, 26), t, font=f, fill=(0, 0, 0, 110))
    tmp = tmp.rotate(-(math.degrees(am) + 90), expand=True, resample=Image.BICUBIC)
    img.paste(tmp, (int(tx - tmp.width / 2), int(ty - tmp.height / 2)), tmp)
d.ellipse([c - 70, c - 70, c + 70, c + 70], fill=(30, 28, 50), outline=(255, 215, 40), width=8)
img.save(os.path.join(OUT, "wheel_face.png"))

# --- Rouleau de machine à sous : 6 symboles empilés (v = (j+0.5)/6 pour le symbole j, image v vers le haut)
W, H = 256, 1536; strip = Image.new("RGB", (W, H), (236, 232, 222)); d = ImageDraw.Draw(strip)
def sym(j, cx, cy):
    r = 80
    if j == 0:   # cerises
        d.ellipse([cx - 62, cy - 10, cx - 4, cy + 48], fill=(220, 30, 50)); d.ellipse([cx + 4, cy - 2, cx + 62, cy + 56], fill=(220, 30, 50))
        d.line([(cx - 33, cy - 10), (cx, cy - 60)], fill=(40, 120, 40), width=6); d.line([(cx + 33, cy - 2), (cx, cy - 60)], fill=(40, 120, 40), width=6)
    elif j == 1: # citron
        d.ellipse([cx - 70, cy - 46, cx + 70, cy + 46], fill=(250, 225, 50), outline=(190, 160, 20), width=5)
    elif j == 2: # cloche
        d.pieslice([cx - 60, cy - 66, cx + 60, cy + 54], 180, 360, fill=(250, 200, 40)); d.rectangle([cx - 60, cy - 6, cx + 60, cy + 40], fill=(250, 200, 40)); d.ellipse([cx - 14, cy + 36, cx + 14, cy + 64], fill=(180, 120, 20))
    elif j == 3: # BAR
        d.rounded_rectangle([cx - 84, cy - 36, cx + 84, cy + 36], radius=10, fill=(30, 30, 40)); f = font(60); w = d.textlength("BAR", font=f); d.text((cx - w / 2, cy - 38), "BAR", font=f, fill=(255, 255, 255))
    elif j == 4: # 7
        f = font(150); w = d.textlength("7", font=f); d.text((cx - w / 2 + 4, cy - 86), "7", font=f, fill=(120, 0, 0)); d.text((cx - w / 2, cy - 90), "7", font=f, fill=(235, 30, 40))
    else:        # étoile
        pts = [(cx + (70 if k % 2 == 0 else 30) * math.sin(k * math.pi / 5), cy - (70 if k % 2 == 0 else 30) * math.cos(k * math.pi / 5)) for k in range(10)]
        d.polygon(pts, fill=(255, 190, 30), outline=(170, 110, 10))
cell = H / 6
for j in range(6):
    cy = H - (j + 0.5) * cell      # v=(j+0.5)/6 mesure depuis le bas de l'image
    sym(j, W / 2, cy)
    d.line([(10, cy + cell / 2), (W - 10, cy + cell / 2)], fill=(150, 145, 135), width=3)
arr = np.array(strip, float) / 255.0
strip.save(os.path.join(OUT, "reel_strip.png"))
Image.fromarray((normal_from_height(arr.mean(2), 1.5) * 255).astype(np.uint8)).save(os.path.join(OUT, "reel_strip_n.png"))

# --- Ballon de basket
S = 512; img = Image.new("RGB", (S, S), (225, 110, 30)); d = ImageDraw.Draw(img)
hh = np.array(img.convert("L"), float) / 255.0
for k in range(0, S, 4): d.line([(0, k), (S, k)], fill=(215 + (k % 8), 100 + (k % 12), 25), width=1)
for x0 in (0, S // 2, S): d.line([(x0, 0), (x0, S)], fill=(30, 20, 15), width=9)
d.line([(0, S // 2), (S, S // 2)], fill=(30, 20, 15), width=9)
img.save(os.path.join(OUT, "ball.png"))
Image.fromarray((normal_from_height(fft_noise(512, 0.5, 21), 1.2) * 255).astype(np.uint8)).save(os.path.join(OUT, "ball_n.png"))

# --- Pavés de réflexes 1..6 (chiffre sur fond coloré)
PAD_COLORS = [(230, 60, 90), (40, 140, 255), (255, 200, 40), (60, 220, 120), (180, 90, 255), (255, 130, 40)]
for k, col in enumerate(PAD_COLORS, 1):
    S = 256; img = Image.new("RGB", (S, S), tuple(int(v * 0.55) for v in col)); d = ImageDraw.Draw(img)
    d.ellipse([10, 10, S - 10, S - 10], outline=(255, 255, 255), width=10)
    f = font(170); w = d.textlength(str(k), font=f); d.text((S / 2 - w / 2, 22), str(k), font=f, fill=(255, 255, 255))
    img.save(os.path.join(OUT, "pad_%d.png" % k))
print("phase 9 :", len(os.listdir(OUT)), "fichiers")
