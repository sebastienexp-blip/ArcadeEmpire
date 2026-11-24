#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>
    /// Charge les modèles FBX générés (Tools/modelgen, dossier Assets/_Project/Art/Models) et leur applique
    /// les matériaux du jeu par nom de slot (Main, Dark, Light, Accent...), pour rester compatible URP.
    /// Si un modèle est absent, l'appelant retombe sur la primitive provisoire.
    /// </summary>
    public static class ModelLib
    {
        public const string Dir = "Assets/_Project/Art/Models";

        public static GameObject Load(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.fbx");
            if (asset == null) Debug.LogWarning($"[ModelLib] Modèle introuvable : {name}");
            return asset;
        }

        /// <summary>Instancie le modèle sous parent ; materials : nom de slot -> matériau. Retourne null si absent.</summary>
        public static GameObject Spawn(string name, Transform parent, Dictionary<string, Material> materials)
        {
            var asset = Load(name);
            if (asset == null) return null;
            var go = Object.Instantiate(asset);
            go.name = name;
            go.transform.SetParent(parent, false);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string slot = mats[i] != null ? mats[i].name.Replace(" (Instance)", "") : "";
                    if (materials.TryGetValue(slot, out var m)) mats[i] = m;
                    else if (materials.TryGetValue("Main", out var fallback)) mats[i] = fallback;
                }
                r.sharedMaterials = mats;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, 0);
            }
            return go;
        }

        /// <summary>Boîte englobante de tous les rendus, en coordonnées locales de root.</summary>
        public static Bounds LocalBounds(Transform root)
        {
            Bounds b = default; bool first = true;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = root.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        /// <summary>
        /// Habille un objet existant : masque son rendu (le collider reste le volume physique) et ajoute le modèle FBX
        /// en enfant, à la taille réelle worldScale (compense l'échelle non uniforme de la primitive). Retourne false si le modèle manque.
        /// </summary>
        public static bool Dress(Transform host, string model, Dictionary<string, Material> palette, float worldScale = 1f, bool hideHost = true)
        {
            var visual = Spawn(model, host, palette);
            if (visual == null) return false;
            var ls = host.lossyScale;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(worldScale / ls.x, worldScale / ls.y, worldScale / ls.z);
            if (hideHost)
            {
                var r = host.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
            return true;
        }

        public const string TexDir = "Assets/_Project/Art/Textures";

        public static Texture2D Tex(string name) => name == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{name}.png");

        /// <summary>Applique texture de base (+ normales) à un matériau URP Lit. tiling : répétitions par unité d'UV.</summary>
        public static Material Textured(Material m, string baseTex, string normalTex = null, float tiling = 1f, float metallic = 0f, float smoothness = -1f)
        {
            var bt = Tex(baseTex);
            if (bt != null) { m.SetTexture("_BaseMap", bt); m.SetTextureScale("_BaseMap", new Vector2(tiling, tiling)); }
            var nt = Tex(normalTex ?? (baseTex != null ? baseTex + "_n" : null));
            if (nt != null)
            {
                m.SetTexture("_BumpMap", nt); m.SetTextureScale("_BumpMap", new Vector2(tiling, tiling));
                m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Metallic", metallic);
            if (smoothness >= 0f) m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Matériau lumineux dont l'image sert aussi d'émission (enseignes).</summary>
        public static Material Glowing(string name, string tex, float intensity)
        {
            var m = RoomBuilder.Mat(name, Color.white, 0.3f);
            var t = Tex(tex);
            m.SetTexture("_BaseMap", t);
            m.SetTexture("_EmissionMap", t);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", Color.white * intensity);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Lit(string name, Color color, float smooth, float metallic, string tex, float tiling)
            => Textured(RoomBuilder.Mat(name, color, smooth), tex, null, tiling, metallic, smooth);

        /// <summary>Tous les matériaux des modèles, par nom de slot. Main est fourni par l'appelant (couleur de l'objet).</summary>
        public static Dictionary<string, Material> Palette(Material main)
        {
            var paint = Lit("Model_Paint", new Color(0.55f, 0.55f, 0.62f), 0.55f, 0.15f, "paint", 1.5f);
            var d = new Dictionary<string, Material>
            {
                ["Main"] = main ?? paint,
                ["Paint"] = paint,
                ["Metal"] = Lit("Model_Metal", new Color(0.85f, 0.87f, 0.92f), 0.75f, 0.9f, "metal", 1.2f),
                ["Rubber"] = Lit("Model_Rubber", new Color(0.35f, 0.35f, 0.38f), 0.25f, 0f, "rubber", 2f),
                ["Plastic"] = Lit("Model_Plastic", new Color(0.92f, 0.92f, 0.95f), 0.6f, 0f, "plastic", 1.5f),
                ["Dark"] = RoomBuilder.Mat("Model_Dark", new Color(0.08f, 0.08f, 0.11f), 0.45f),
                ["Light"] = RoomBuilder.Mat("Model_Light", new Color(0.95f, 0.95f, 0.95f), 0.3f),
                ["Eye"] = RoomBuilder.Mat("Model_Eye", new Color(0.02f, 0.02f, 0.025f), 0.97f),
                ["Tag"] = RoomBuilder.Mat("Model_Tag", new Color(0.97f, 0.97f, 0.95f), 0.45f),
                ["Second"] = RoomBuilder.Mat("Model_Second", new Color(0.3f, 0.2f, 0.14f), 0.15f),
                ["Accent"] = RoomBuilder.Neon("Model_Accent", new Color(1f, 0.8f, 0.2f)),
                ["Pot"] = Lit("Model_Pot", new Color(0.62f, 0.36f, 0.22f), 0.3f, 0f, "plastic", 1.5f),
                ["Soil"] = Lit("Model_Soil", new Color(0.2f, 0.12f, 0.08f), 0.1f, 0f, "felt", 2f),
                ["NeonPink"] = RoomBuilder.Neon("Neon_Pink", new Color(1f, 0.1f, 0.6f)),
                ["NeonCyan"] = RoomBuilder.Neon("Neon_Cyan", new Color(0.1f, 0.9f, 1f)),
                ["NeonYellow"] = RoomBuilder.Neon("Neon_Yellow", new Color(1f, 0.85f, 0.1f)),
                ["SignClaw"] = Glowing("Sign_Claw", "sign_claw", 1.6f),
                ["SignPusher"] = Glowing("Sign_Pusher", "sign_pusher", 1.6f),
                ["SignWheel"] = Glowing("Sign_Wheel", "sign_wheel", 1.6f),
                ["SignSlots"] = Glowing("Sign_Slots", "sign_slots", 1.6f),
                ["SignHammer"] = Glowing("Sign_Hammer", "sign_hammer", 1.6f),
                ["SignBasket"] = Glowing("Sign_Basket", "sign_basket", 1.6f),
                ["SignReflex"] = Glowing("Sign_Reflex", "sign_reflex", 1.6f),
                ["SignRace"] = Glowing("Sign_Race", "sign_race", 1.6f),
                ["SignStacker"] = Glowing("Sign_Stacker", "sign_stacker", 1.6f),
                ["RaceScreen"] = Glowing("Race_Screen", "road_screen", 0.8f),
                ["SignHockey"] = Glowing("Sign_Hockey", "sign_hockey", 1.6f),
                ["SignShoot"] = Glowing("Sign_Shoot", "sign_shoot", 1.6f),
                ["SignFishing"] = Glowing("Sign_Fishing", "sign_fishing", 1.6f),
                ["HockeySurface"] = Lit("Hockey_Surface", Color.white, 0.8f, 0f, "hockey_surface", 1f),
                ["Water"] = Lit("Pond_Water", new Color(0.75f, 0.9f, 1f), 0.95f, 0f, "water", 1f),
                ["Mallet"] = Lit("Hockey_Mallet", new Color(0.9f, 0.15f, 0.15f), 0.55f, 0.2f, "plastic", 1.5f),
                ["WheelFace"] = Textured(Glowing("Wheel_Face", "wheel_face", 0.35f), "wheel_face", "paint", 1f, 0.1f, 0.5f),
                ["Reel"] = Textured(Glowing("Slot_Reel", "reel_strip", 0.3f), "reel_strip", "reel_strip_n", 1f, 0f, 0.55f),
                ["BallSkin"] = Lit("Ball_Skin", Color.white, 0.35f, 0f, "ball", 1f),
                ["Ring"] = RoomBuilder.Neon("Hoop_Ring", new Color(1f, 0.4f, 0.1f)),
                ["DecalClaw"] = Glowing("Decal_Claw", "decal_claw", 0.7f),
                ["DecalPusher"] = Glowing("Decal_Pusher", "decal_pusher", 0.7f),
                ["StickBall"] = RoomBuilder.Neon("Model_StickBall", new Color(1f, 0.15f, 0.6f) * 0.5f),
                ["ButtonColor"] = RoomBuilder.Neon("Model_ButtonPink", new Color(1f, 0.1f, 0.6f)),
                ["Rim"] = Lit("Model_CoinRim", new Color(1f, 0.78f, 0.1f), 0.75f, 0.9f, "metal", 2f),
                ["Face"] = Lit("Model_CoinFace", new Color(1f, 0.78f, 0.1f), 0.7f, 0.9f, "coin_face", 1f),
                ["Shirt"] = Lit("Player_Shirt", new Color(0.95f, 0.5f, 0.15f), 0.2f, 0f, "fabric", 3f),
                ["Jacket"] = Lit("Player_Jacket", new Color(0.22f, 0.27f, 0.42f), 0.25f, 0f, "denim", 2f),
                ["Pants"] = Lit("Player_Pants", new Color(0.2f, 0.34f, 0.62f), 0.2f, 0f, "denim", 2.5f),
                ["Sneaker"] = Lit("Player_Sneaker", new Color(0.9f, 0.22f, 0.3f), 0.45f, 0f, "plastic", 2f),
                ["Skin"] = Lit("Player_Skin", new Color(0.93f, 0.72f, 0.56f), 0.35f, 0f, "skin", 1f),
                ["Hair"] = Lit("Player_Hair", new Color(0.27f, 0.17f, 0.1f), 0.4f, 0f, "fabric", 3f),
                ["Mouth"] = RoomBuilder.Mat("Player_Mouth", new Color(0.55f, 0.2f, 0.2f), 0.3f),
            };
            return d;
        }

        /// <summary>Copie de la palette avec une autre couleur lumineuse pour le slot ButtonColor.</summary>
        public static Dictionary<string, Material> WithButtonColor(Dictionary<string, Material> pal, Color c)
        {
            var d = new Dictionary<string, Material>(pal);
            d["ButtonColor"] = RoomBuilder.Neon("Model_Button_" + ColorUtility.ToHtmlStringRGB(c), c);
            return d;
        }

        /// <summary>Palette d'une pièce ou d'un jeton : Rim (tranche) et Face (motif) teintés de la couleur donnée.</summary>
        public static Dictionary<string, Material> CoinPalette(string id, Color color)
        {
            var d = Palette(null);
            d["Rim"] = Lit("Coin_Rim_" + id, color, 0.75f, 0.9f, "metal", 2f);
            d["Face"] = Lit("Coin_Face_" + id, color, 0.7f, 0.9f, "coin_face", 1f);
            return d;
        }

        /// <summary>Bornes locales (par rapport à root) du maillage d'un modèle, pour diagnostic d'import.</summary>
        public static void LogBounds(string name)
        {
            var go = Load(name);
            if (go == null) return;
            var b = LocalBounds(go.transform);
            Debug.Log($"[ModelLib] {name} : centre {b.center.ToString("F3")} taille {b.size.ToString("F3")}");
        }
    }
}
#endif
