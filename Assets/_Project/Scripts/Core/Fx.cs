using UnityEngine;

namespace ArcadeEmpire.Core
{
    /// <summary>
    /// Effets visuels créés par le code : confettis de victoire, étincelles dorées (pièces, clés), anneau d'énergie (téléporteur).
    /// Matériaux dans Resources/FX (générés par RoomBuilder) pour que leurs shaders soient inclus dans le build.
    /// </summary>
    public static class Fx
    {
        private static Material glow, flat;
        private static Texture2D dot;

        private static Material Glow()
        {
            if (glow != null) return glow;
            var src = Resources.Load<Material>("FX/ParticleUnlit");
            if (src == null) return null;
            glow = new Material(src);
            glow.SetTexture("_BaseMap", Dot());
            return glow;
        }

        private static Material Flat()
        {
            if (flat != null) return flat;
            var src = Resources.Load<Material>("FX/ParticleLit");
            if (src == null) return null;
            flat = new Material(src);
            return flat;
        }

        /// <summary>Point lumineux doux (dégradé radial) pour les étincelles.</summary>
        private static Texture2D Dot()
        {
            if (dot != null) return dot;
            const int n = 64;
            dot = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r); a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            dot.SetPixels(px); dot.Apply();
            return dot;
        }

        private static ParticleSystem Make(string name, Vector3 pos, Material m, float life)
        {
            var go = new GameObject("Fx_" + name);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = false; main.duration = 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.startLifetime = life;
            var em = ps.emission; em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = m;
            return ps;
        }

        /// <summary>Pluie de confettis multicolores (lot gagné, achat).</summary>
        public static void Confetti(Vector3 pos, int count = 70)
        {
            var m = Flat(); if (m == null) return;
            var ps = Make("Confetti", pos, m, 2.4f);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 6.28f); main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 6.28f); main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.gravityModifier = 0.55f;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.3f, 0.6f), 0f), new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0.33f), new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.66f), new GradientColorKey(new Color(0.4f, 1f, 0.5f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 35f; shape.radius = 0.05f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-8f, 8f); rot.y = new ParticleSystem.MinMaxCurve(-8f, 8f); rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            var vel = ps.limitVelocityOverLifetime; vel.enabled = true; vel.limit = 2.2f; vel.dampen = 0.12f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;
            ps.Emit(count);
            ps.Play();
        }

        /// <summary>Étincelles lumineuses (pièces gagnées, clé dorée, jackpot).</summary>
        public static void Sparkle(Vector3 pos, Color color, int count = 30, float speed = 1.4f, float size = 0.06f)
        {
            var m = Glow(); if (m == null) return;
            var ps = Make("Sparkle", pos, m, 0.9f);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color * 1.6f;
            main.gravityModifier = 0.25f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.05f;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            ps.Emit(count);
            ps.Play();
        }

        /// <summary>Colonne d'énergie montante (téléporteur).</summary>
        public static void TeleportBurst(Vector3 pos, Color color)
        {
            var m = Glow(); if (m == null) return;
            var ps = Make("Teleport", pos, m, 1.1f);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = color * 1.8f;
            main.gravityModifier = -0.15f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.5f; shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.radiusThickness = 0.1f;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            ps.Emit(90);
            ps.Play();
        }
    }
}
