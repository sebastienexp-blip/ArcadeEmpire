using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Habillage graphique généré par le code : panneaux arrondis (9-slice), disque, anneau et une trentaine d'icônes dessinées
    /// pixel par pixel à partir de formes mathématiques (aucune image importée). Les icônes sont créées à la demande, puis mises en cache.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Gold = new Color(1f, 0.82f, 0.22f);
        public static readonly Color Cyan = new Color(0.25f, 0.9f, 1f);
        public static readonly Color Pink = new Color(1f, 0.3f, 0.65f);
        public static readonly Color Green = new Color(0.3f, 0.88f, 0.5f);
        public static readonly Color Red = new Color(1f, 0.35f, 0.35f);
        public static readonly Color Panel = new Color(0.07f, 0.06f, 0.16f, 0.9f);
        public static readonly Color PanelLight = new Color(0.14f, 0.13f, 0.3f, 0.95f);
        public static readonly Color Text = new Color(0.93f, 0.96f, 1f);
        public static readonly Color TextDim = new Color(0.66f, 0.7f, 0.88f);

        private static Sprite rounded, circle, ring;
        private static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        public static Sprite Rounded => rounded != null ? rounded : (rounded = MakeRounded(64, 18));
        public static Sprite Circle => circle != null ? circle : (circle = MakeShape(128, (x, y) => Mathf.Sqrt(x * x + y * y) - 0.97f));
        public static Sprite Ring => ring != null ? ring : (ring = MakeShape(128, (x, y) => Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 0.88f) - 0.1f));

        public static Sprite Icon(string name)
        {
            if (icons.TryGetValue(name, out Sprite s) && s != null) return s;
            var p = new Pix(96);
            Draw(name, p);
            var tex = new Texture2D(96, 96, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(p.c); tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 100f);
            icons[name] = s;
            return s;
        }

        // ------------------------------------------------------------------ formes de base
        private static Sprite MakeRounded(int n, int radius)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            float r = radius, h = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - h) - (h - r), qy = Mathf.Abs(y + 0.5f - h) - (h - r);
                    float d = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
                }
            tex.SetPixels(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
        }

        private static Sprite MakeShape(int n, Func<float, float, float> sdf)
        {
            var p = new Pix(n);
            p.Fill(sdf, Color.white);
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(p.c); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        // ------------------------------------------------------------------ rasterisation
        private sealed class Pix
        {
            public readonly int n;
            public readonly Color[] c;
            public Pix(int size) { n = size; c = new Color[size * size]; }

            /// <summary>Remplit les pixels où sdf(x, y) &lt; 0 (coordonnées de -1 à 1, y vers le haut), avec un bord lissé.</summary>
            public void Fill(Func<float, float, float> sdf, Color col)
            {
                float aa = 2f / n * 1.25f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = sdf((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
                        float a = Mathf.Clamp01(0.5f - d / aa);
                        if (a <= 0f) continue;
                        Color s = col; s.a *= a;
                        Color o = c[y * n + x];
                        float oa = s.a + o.a * (1f - s.a);
                        c[y * n + x] = oa <= 0f ? Color.clear : new Color((s.r * s.a + o.r * o.a * (1f - s.a)) / oa, (s.g * s.a + o.g * o.a * (1f - s.a)) / oa, (s.b * s.a + o.b * o.a * (1f - s.a)) / oa, oa);
                    }
            }
        }

        private static float Circ(float x, float y, float cx, float cy, float r) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        private static float Box(float x, float y, float cx, float cy, float hx, float hy, float r = 0f)
        {
            float qx = Mathf.Abs(x - cx) - hx + r, qy = Mathf.Abs(y - cy) - hy + r;
            return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float RotBox(float x, float y, float cx, float cy, float hx, float hy, float r, float deg)
        {
            float a = -deg * Mathf.Deg2Rad, dx = x - cx, dy = y - cy;
            return Box(dx * Mathf.Cos(a) - dy * Mathf.Sin(a), dx * Mathf.Sin(a) + dy * Mathf.Cos(a), 0f, 0f, hx, hy, r);
        }

        private static float Cap(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float px = x - ax, py = y - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((px * bax + py * bay) / (bax * bax + bay * bay));
            float dx = px - bax * h, dy = py - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        /// <summary>Polygone convexe, sommets dans le sens anti-horaire.</summary>
        private static float Poly(float x, float y, params float[] pts)
        {
            float d = -1e9f;
            int k = pts.Length / 2;
            for (int i = 0; i < k; i++)
            {
                float ax = pts[2 * i], ay = pts[2 * i + 1], bx = pts[2 * ((i + 1) % k)], by = pts[2 * ((i + 1) % k) + 1];
                float ex = bx - ax, ey = by - ay, len = Mathf.Sqrt(ex * ex + ey * ey);
                d = Mathf.Max(d, ((x - ax) * ey - (y - ay) * ex) / len);
            }
            return d;
        }

        private static float Ring2(float x, float y, float cx, float cy, float r, float w) => Mathf.Abs(Circ(x, y, cx, cy, r)) - w;
        private static float Sub(float a, float b) => Mathf.Max(a, -b);

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        // ------------------------------------------------------------------ les icônes
        private static void Draw(string name, Pix p)
        {
            Color gold = C(1f, 0.82f, 0.22f), goldD = C(0.78f, 0.5f, 0.08f), white = Color.white, dark = C(0.1f, 0.12f, 0.24f), steel = C(0.72f, 0.76f, 0.86f);
            switch (name)
            {
                case "coin":
                    p.Fill((x, y) => Circ(x, y, 0, 0, 0.9f), goldD);
                    p.Fill((x, y) => Circ(x, y, 0, 0, 0.78f), gold);
                    p.Fill((x, y) => Ring2(x, y, 0, 0, 0.55f, 0.05f), goldD);
                    p.Fill((x, y) => Mathf.Min(RotBox(x, y, 0, 0, 0.2f, 0.2f, 0.03f, 45f), Box(x, y, 0, 0, 0.05f, 0.42f)), goldD);
                    p.Fill((x, y) => Circ(x, y, -0.35f, 0.4f, 0.12f), C(1f, 1f, 0.8f, 0.8f));
                    break;
                case "ticket":
                    p.Fill((x, y) => Sub(RotBox(x, y, 0, 0, 0.88f, 0.52f, 0.1f, -14f), Mathf.Min(Circ(x, y, -0.88f * Mathf.Cos(0.244f), 0.88f * Mathf.Sin(0.244f), 0.17f), Circ(x, y, 0.88f * Mathf.Cos(0.244f), -0.88f * Mathf.Sin(0.244f), 0.17f))), C(1f, 0.5f, 0.3f));
                    for (int i = -2; i <= 2; i++) { float k = i * 0.22f; p.Fill((x, y) => RotBox(x, y, 0.28f + k * 0.24f, -k * 0.97f * 0.24f * 0f - k * 0.2f, 0.035f, 0.07f, 0.02f, -14f), C(0.55f, 0.2f, 0.1f)); }
                    p.Fill((x, y) => RotBox(x, y, -0.3f, 0.05f, 0.3f, 0.07f, 0.03f, -14f), white);
                    p.Fill((x, y) => RotBox(x, y, -0.34f, -0.12f, 0.22f, 0.05f, 0.03f, -14f), C(1f, 0.9f, 0.85f));
                    break;
                case "gem":
                    p.Fill((x, y) => Poly(x, y, 0f, -0.92f, 0.88f, 0.12f, 0.52f, 0.55f, -0.52f, 0.55f, -0.88f, 0.12f), C(0.2f, 0.7f, 1f));
                    p.Fill((x, y) => Poly(x, y, -0.52f, 0.55f, -0.88f, 0.12f, -0.15f, 0.12f), C(0.55f, 0.92f, 1f));
                    p.Fill((x, y) => Poly(x, y, -0.52f, 0.55f, -0.15f, 0.12f, 0.15f, 0.12f, 0.52f, 0.55f), C(0.4f, 0.85f, 1f));
                    p.Fill((x, y) => Poly(x, y, 0f, -0.92f, 0.15f, 0.12f, -0.15f, 0.12f), C(0.1f, 0.5f, 0.9f));
                    break;
                case "star":
                    StarFill(p, gold, goldD);
                    break;
                case "bag":
                    p.Fill((x, y) => Sub(Ring2(x, y, 0, 0.78f, 0.28f, 0.07f), Box(x, y, 0, 0.3f, 2f, 0.5f)), C(0.45f, 0.25f, 0.1f));
                    p.Fill((x, y) => Box(x, y, 0, -0.1f, 0.66f, 0.74f, 0.28f), C(0.95f, 0.58f, 0.2f));
                    p.Fill((x, y) => Box(x, y, 0, 0.3f, 0.66f, 0.32f, 0.24f), C(0.8f, 0.4f, 0.12f));
                    p.Fill((x, y) => Box(x, y, 0, -0.42f, 0.4f, 0.24f, 0.1f), C(1f, 0.75f, 0.4f));
                    p.Fill((x, y) => Box(x, y, 0, -0.2f, 0.07f, 0.07f, 0.03f), goldD);
                    break;
                case "shop":
                    p.Fill((x, y) => Sub(Ring2(x, y, 0, 0.3f, 0.36f, 0.07f), Box(x, y, 0, -0.1f, 2f, 0.4f)), C(0.12f, 0.5f, 0.3f));
                    p.Fill((x, y) => Poly(x, y, -0.76f, -0.88f, 0.76f, -0.88f, 0.64f, 0.32f, -0.64f, 0.32f), C(0.3f, 0.88f, 0.5f));
                    p.Fill((x, y) => Poly(x, y, -0.76f, -0.88f, -0.64f, 0.32f, -0.4f, 0.32f, -0.45f, -0.88f), C(0.2f, 0.7f, 0.4f));
                    p.Fill((x, y) => Circ(x, y, 0.05f, -0.2f, 0.28f), white);
                    p.Fill((x, y) => Circ(x, y, 0.05f, -0.2f, 0.17f), gold);
                    break;
                case "mission":
                    p.Fill((x, y) => Box(x, y, 0, -0.05f, 0.64f, 0.86f, 0.12f), C(0.55f, 0.35f, 0.18f));
                    p.Fill((x, y) => Box(x, y, 0, -0.08f, 0.52f, 0.73f, 0.06f), white);
                    p.Fill((x, y) => Box(x, y, 0, 0.8f, 0.3f, 0.13f, 0.06f), C(0.5f, 0.55f, 0.65f));
                    for (int i = 0; i < 3; i++) { float yy = 0.38f - i * 0.4f; p.Fill((x, y) => Cap(x, y, -0.02f, yy, 0.34f, yy, 0.06f), C(0.6f, 0.65f, 0.8f)); p.Fill((x, y) => Box(x, y, -0.34f, yy, 0.1f, 0.1f, 0.03f), C(0.85f, 0.9f, 1f)); }
                    p.Fill((x, y) => Mathf.Min(Cap(x, y, -0.42f, 0.38f, -0.34f, 0.28f, 0.06f), Cap(x, y, -0.34f, 0.28f, -0.2f, 0.5f, 0.06f)), C(0.2f, 0.75f, 0.4f));
                    break;
                case "trophy":
                    p.Fill((x, y) => Mathf.Min(Ring2(x, y, -0.62f, 0.38f, 0.24f, 0.065f), Ring2(x, y, 0.62f, 0.38f, 0.24f, 0.065f)), goldD);
                    p.Fill((x, y) => Poly(x, y, -0.42f, -0.12f, 0.42f, -0.12f, 0.58f, 0.75f, -0.58f, 0.75f), gold);
                    p.Fill((x, y) => Box(x, y, 0, -0.32f, 0.1f, 0.2f), goldD);
                    p.Fill((x, y) => Box(x, y, 0, -0.72f, 0.46f, 0.14f, 0.05f), goldD);
                    p.Fill((x, y) => Poly(x, y, -0.58f, 0.75f, -0.42f, -0.12f, -0.3f, -0.12f, -0.4f, 0.75f), C(1f, 0.95f, 0.6f, 0.55f));
                    break;
                case "hammer":
                    p.Fill((x, y) => Cap(x, y, -0.62f, -0.75f, 0.2f, 0.12f, 0.11f), C(0.65f, 0.4f, 0.2f));
                    p.Fill((x, y) => RotBox(x, y, 0.32f, 0.35f, 0.5f, 0.24f, 0.07f, -45f), steel);
                    p.Fill((x, y) => RotBox(x, y, 0.32f, 0.35f, 0.5f, 0.07f, 0.03f, -45f), C(0.95f, 0.97f, 1f, 0.7f));
                    break;
                case "gear":
                    p.Fill((x, y) =>
                    {
                        float d = Circ(x, y, 0, 0, 0.62f);
                        for (int k = 0; k < 4; k++) d = Mathf.Min(d, RotBox(x, y, 0, 0, 0.84f, 0.17f, 0.04f, k * 45f));
                        return Sub(d, Circ(x, y, 0, 0, 0.27f));
                    }, steel);
                    p.Fill((x, y) => Ring2(x, y, 0, 0, 0.45f, 0.03f), C(1f, 1f, 1f, 0.5f));
                    break;
                case "play":
                    p.Fill((x, y) => Poly(x, y, -0.5f, -0.75f, 0.75f, 0f, -0.5f, 0.75f), C(0.3f, 0.9f, 0.5f));
                    break;
                case "pause":
                    p.Fill((x, y) => Mathf.Min(Box(x, y, -0.32f, 0, 0.2f, 0.7f, 0.06f), Box(x, y, 0.32f, 0, 0.2f, 0.7f, 0.06f)), white);
                    break;
                case "save":
                    p.Fill((x, y) => Box(x, y, 0, 0, 0.82f, 0.82f, 0.1f), C(0.3f, 0.5f, 0.95f));
                    p.Fill((x, y) => Box(x, y, 0.05f, 0.5f, 0.5f, 0.28f, 0.03f), C(0.85f, 0.9f, 1f));
                    p.Fill((x, y) => Box(x, y, 0.32f, 0.5f, 0.1f, 0.2f, 0.02f), C(0.3f, 0.4f, 0.7f));
                    p.Fill((x, y) => Box(x, y, 0, -0.42f, 0.58f, 0.3f, 0.04f), white);
                    p.Fill((x, y) => Cap(x, y, -0.35f, -0.35f, 0.35f, -0.35f, 0.04f), C(0.65f, 0.7f, 0.85f));
                    break;
                case "door":
                    p.Fill((x, y) => Box(x, y, -0.25f, 0, 0.42f, 0.85f, 0.06f), C(0.6f, 0.38f, 0.2f));
                    p.Fill((x, y) => Box(x, y, -0.25f, 0, 0.32f, 0.75f, 0.03f), C(0.78f, 0.52f, 0.28f));
                    p.Fill((x, y) => Circ(x, y, -0.02f, -0.02f, 0.06f), gold);
                    p.Fill((x, y) => Mathf.Min(Cap(x, y, 0.2f, 0f, 0.62f, 0f, 0.1f), Poly(x, y, 0.55f, -0.34f, 0.95f, 0f, 0.55f, 0.34f)), white);
                    break;
                case "speaker":
                    p.Fill((x, y) => Mathf.Min(Box(x, y, -0.62f, 0f, 0.2f, 0.28f, 0.03f), Poly(x, y, -0.45f, -0.28f, 0.05f, -0.72f, 0.05f, 0.72f, -0.45f, 0.28f)), white);
                    p.Fill((x, y) => Mathf.Max(Mathf.Max(Ring2(x, y, 0.05f, 0f, 0.42f, 0.06f), -(x - 0.25f)), Mathf.Abs(y) - 0.5f), C(0.7f, 0.9f, 1f));
                    p.Fill((x, y) => Mathf.Max(Mathf.Max(Ring2(x, y, 0.05f, 0f, 0.72f, 0.06f), -(x - 0.35f)), Mathf.Abs(y) - 0.7f), C(0.7f, 0.9f, 1f, 0.8f));
                    break;
                case "note":
                    p.Fill((x, y) => Mathf.Min(Circ(x, y, -0.32f, -0.55f, 0.27f), Box(x, y, -0.07f, 0.08f, 0.07f, 0.62f)), white);
                    p.Fill((x, y) => Cap(x, y, -0.02f, 0.62f, 0.5f, 0.2f, 0.1f), white);
                    p.Fill((x, y) => Mathf.Min(Circ(x, y, 0.38f, -0.78f, 0.2f), Box(x, y, 0.55f, -0.22f, 0.05f, 0.55f)), C(0.7f, 0.9f, 1f));
                    break;
                case "monitor":
                    p.Fill((x, y) => Box(x, y, 0, 0.12f, 0.88f, 0.62f, 0.1f), C(0.25f, 0.28f, 0.42f));
                    p.Fill((x, y) => Box(x, y, 0, 0.12f, 0.76f, 0.5f, 0.05f), C(0.2f, 0.75f, 0.95f));
                    p.Fill((x, y) => Poly(x, y, -0.76f, -0.38f, 0.1f, -0.38f, 0.76f, 0.62f, -0.76f, 0.62f), C(1f, 1f, 1f, 0.15f));
                    p.Fill((x, y) => Mathf.Min(Box(x, y, 0, -0.68f, 0.09f, 0.14f), Box(x, y, 0, -0.84f, 0.38f, 0.07f, 0.03f)), steel);
                    break;
                case "gamepad":
                    p.Fill((x, y) => Mathf.Min(Box(x, y, 0, 0, 0.9f, 0.5f, 0.32f), Mathf.Min(Circ(x, y, -0.62f, -0.34f, 0.3f), Circ(x, y, 0.62f, -0.34f, 0.3f))), C(0.42f, 0.46f, 0.64f));
                    p.Fill((x, y) => Mathf.Min(Box(x, y, -0.5f, 0.04f, 0.24f, 0.08f, 0.02f), Box(x, y, -0.5f, 0.04f, 0.08f, 0.24f, 0.02f)), white);
                    p.Fill((x, y) => Circ(x, y, 0.62f, 0.12f, 0.1f), C(1f, 0.4f, 0.4f));
                    p.Fill((x, y) => Circ(x, y, 0.42f, -0.06f, 0.1f), C(0.4f, 0.9f, 0.5f));
                    p.Fill((x, y) => Circ(x, y, 0.62f, -0.24f, 0.1f), C(0.4f, 0.7f, 1f));
                    break;
                case "camera":
                    p.Fill((x, y) => Mathf.Min(Box(x, y, 0, -0.05f, 0.86f, 0.56f, 0.12f), Box(x, y, -0.35f, 0.55f, 0.24f, 0.14f, 0.05f)), C(0.5f, 0.55f, 0.72f));
                    p.Fill((x, y) => Circ(x, y, 0, -0.05f, 0.36f), dark);
                    p.Fill((x, y) => Circ(x, y, 0, -0.05f, 0.25f), C(0.3f, 0.6f, 0.95f));
                    p.Fill((x, y) => Circ(x, y, -0.08f, 0.04f, 0.07f), C(1f, 1f, 1f, 0.8f));
                    p.Fill((x, y) => Circ(x, y, 0.62f, 0.3f, 0.06f), C(1f, 0.4f, 0.4f));
                    break;
                case "close":
                    p.Fill((x, y) => Mathf.Min(Cap(x, y, -0.55f, -0.55f, 0.55f, 0.55f, 0.13f), Cap(x, y, -0.55f, 0.55f, 0.55f, -0.55f, 0.13f)), white);
                    break;
                case "check":
                    p.Fill((x, y) => Mathf.Min(Cap(x, y, -0.6f, 0f, -0.2f, -0.45f, 0.13f), Cap(x, y, -0.2f, -0.45f, 0.65f, 0.5f, 0.13f)), C(0.3f, 0.92f, 0.5f));
                    break;
                case "left": p.Fill((x, y) => Poly(x, y, 0.45f, -0.7f, 0.45f, 0.7f, -0.5f, 0f), white); break;
                case "right": p.Fill((x, y) => Poly(x, y, -0.45f, -0.7f, 0.5f, 0f, -0.45f, 0.7f), white); break;
                case "up": p.Fill((x, y) => Poly(x, y, -0.7f, -0.45f, 0.7f, -0.45f, 0f, 0.5f), white); break;
                case "down": p.Fill((x, y) => Poly(x, y, -0.7f, 0.45f, 0f, -0.5f, 0.7f, 0.45f), white); break;
                case "rotL":
                    p.Fill((x, y) => Sub(Ring2(x, y, 0, 0, 0.55f, 0.11f), Box(x, y, 0.3f, 0.35f, 0.4f, 0.3f)), white);
                    p.Fill((x, y) => Poly(x, y, 0.15f, 0.38f, 0.75f, 0.55f, 0.58f, -0.05f), white);
                    break;
                case "rotR":
                    p.Fill((x, y) => Sub(Ring2(x, y, 0, 0, 0.55f, 0.11f), Box(x, y, -0.3f, 0.35f, 0.4f, 0.3f)), white);
                    p.Fill((x, y) => Poly(x, y, -0.15f, 0.38f, -0.58f, -0.05f, -0.75f, 0.55f), white);
                    break;
                case "refresh":
                    p.Fill((x, y) => Sub(Ring2(x, y, 0, 0, 0.55f, 0.11f), Box(x, y, 0.4f, 0.4f, 0.3f, 0.3f)), white);
                    p.Fill((x, y) => Poly(x, y, 0.3f, 0.3f, 0.85f, 0.35f, 0.62f, 0.85f), white);
                    break;
                case "eye":
                    p.Fill((x, y) => Mathf.Max(Circ(x, y, 0f, -0.9f, 1.4f), Circ(x, y, 0f, 0.9f, 1.4f)), white);
                    p.Fill((x, y) => Circ(x, y, 0, 0, 0.38f), C(0.3f, 0.6f, 0.95f));
                    p.Fill((x, y) => Circ(x, y, 0, 0, 0.16f), dark);
                    break;
                case "touch":
                    p.Fill((x, y) => Ring2(x, y, 0, 0, 0.7f, 0.07f), white);
                    p.Fill((x, y) => Circ(x, y, 0, 0, 0.28f), white);
                    break;
                case "plus":
                    p.Fill((x, y) => Mathf.Min(Box(x, y, 0, 0, 0.7f, 0.13f, 0.05f), Box(x, y, 0, 0, 0.13f, 0.7f, 0.05f)), white);
                    break;
                case "minus":
                    p.Fill((x, y) => Box(x, y, 0, 0, 0.7f, 0.13f, 0.05f), white);
                    break;
                case "bolt":
                    p.Fill((x, y) => Mathf.Min(Poly(x, y, -0.35f, -0.1f, 0.12f, -0.1f, 0.05f, 0.9f, -0.5f, 0.9f), Poly(x, y, -0.05f, -0.95f, 0.5f, -0.1f, 0.12f, -0.1f, -0.2f, -0.1f)), gold);
                    break;
                default:
                    p.Fill((x, y) => Circ(x, y, 0, 0, 0.8f), white);
                    break;
            }
        }

        private static void StarFill(Pix p, Color fill, Color edge)
        {
            float[] px = new float[10], py = new float[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f, r = (i % 2 == 0) ? 0.95f : 0.4f;
                px[i] = Mathf.Cos(a) * r; py[i] = Mathf.Sin(a) * r - 0.05f;
            }
            Func<float, float, float> star = (x, y) =>
            {
                float d = Poly(x, y, px[1], py[1], px[3], py[3], px[5], py[5], px[7], py[7], px[9], py[9]);
                for (int k = 0; k < 5; k++)
                {
                    int tip = 2 * k, l = (2 * k + 9) % 10, r = 2 * k + 1;
                    d = Mathf.Min(d, Poly(x, y, px[l], py[l], px[r], py[r], px[tip], py[tip]));
                }
                return d;
            };
            p.Fill((x, y) => star(x * 1.08f, (y + 0.05f) * 1.08f - 0.05f), edge);
            p.Fill(star, fill);
        }
    }
}
