#if UNITY_EDITOR
using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Machines;
using UnityEditor;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>
    /// Construit la machine à pinces classique (3 branches) avec des PRIMITIVES Unity (modèle provisoire),
    /// ses données (MachineData, RewardData) et ses lots physiques. Repère local : façade du côté -Z,
    /// zone de jeu x[-0.6;0.6] z[-0.5;0.5], sol de jeu à y = 0,55, trappe dans le coin avant-gauche.
    /// </summary>
    public static class ClawMachineBuilder
    {
        const string DataDir = "Assets/_Project/Data";
        const string MatDir = "Assets/_Project/Materials";

        sealed class PrizeSpec
        {
            public string id, name; public Rarity rarity; public PrimitiveType shape;
            public string model; public float mass; public Color color; public Vector3 scale; public int count, tickets; public long xp; public bool glow;
            // peluches réalistes : couleurs des zones (ventre/museau, 2e couleur, bec/corne) et taille (plus grande dimension, m)
            public Color light = new Color(0.97f, 0.94f, 0.9f), second = new Color(0.35f, 0.22f, 0.15f), accent = new Color(1f, 0.6f, 0.15f);
            public float size; public bool plush;
        }

        static PrizeSpec P(string id, string name, string model, Rarity r, Color main, int count, int tickets, long xp, float size, float mass,
            Color? light = null, Color? second = null, Color? accent = null)
        {
            var s = new PrizeSpec { id = id, name = name, model = model, rarity = r, color = main, count = count, tickets = tickets, xp = xp, size = size, mass = mass,
                plush = model.StartsWith("plush_"), shape = PrimitiveType.Sphere, scale = new Vector3(size, size, size) / 1.25f };
            if (light.HasValue) s.light = light.Value;
            if (second.HasValue) s.second = second.Value;
            if (accent.HasValue) s.accent = accent.Value;
            return s;
        }

        static readonly PrizeSpec[] Specs =
        {
            // --- peluches communes (les ids historiques restent valables pour le pousse-pièces)
            P("plush_pink", "Lapin rose", "plush_bunny", Rarity.Common, new Color(1f, 0.62f, 0.78f), 3, 1, 5, 0.27f, 0.11f, new Color(1f, 0.93f, 0.95f)),
            P("plush_blue", "Ourson bleu", "plush_bear", Rarity.Common, new Color(0.45f, 0.62f, 0.95f), 3, 1, 5, 0.26f, 0.12f, new Color(0.95f, 0.96f, 1f)),
            P("plush_brown", "Ourson brun", "plush_bear", Rarity.Common, new Color(0.55f, 0.36f, 0.22f), 3, 1, 5, 0.26f, 0.12f, new Color(0.93f, 0.84f, 0.7f)),
            P("plush_cat", "Chat roux", "plush_cat", Rarity.Common, new Color(0.95f, 0.62f, 0.3f), 3, 1, 5, 0.26f, 0.12f, new Color(1f, 0.95f, 0.88f), new Color(0.78f, 0.42f, 0.16f)),
            P("plush_dog", "Chiot", "plush_dog", Rarity.Common, new Color(0.96f, 0.9f, 0.78f), 3, 1, 5, 0.26f, 0.12f, Color.white, new Color(0.55f, 0.38f, 0.25f)),
            P("plush_yellow", "Canard", "plush_duck", Rarity.Common, new Color(1f, 0.86f, 0.25f), 3, 1, 5, 0.25f, 0.11f, null, null, new Color(1f, 0.55f, 0.1f)),
            P("plush_green", "Grenouille", "plush_frog", Rarity.Common, new Color(0.4f, 0.78f, 0.3f), 3, 1, 5, 0.27f, 0.12f, new Color(0.88f, 0.96f, 0.68f)),
            // --- rares
            P("plush_penguin", "Pingouin", "plush_penguin", Rarity.Rare, new Color(0.15f, 0.17f, 0.24f), 2, 5, 15, 0.28f, 0.13f, Color.white, null, new Color(1f, 0.6f, 0.12f)),
            P("plush_panda", "Panda", "plush_panda", Rarity.Rare, new Color(0.97f, 0.97f, 0.95f), 2, 5, 15, 0.28f, 0.13f, Color.white, new Color(0.1f, 0.1f, 0.12f)),
            P("plush_fox", "Renard", "plush_fox", Rarity.Rare, new Color(0.95f, 0.45f, 0.15f), 2, 5, 15, 0.28f, 0.13f, Color.white),
            P("plush_octopus", "Pieuvre", "plush_octopus", Rarity.Rare, new Color(0.75f, 0.45f, 0.9f), 2, 5, 15, 0.28f, 0.13f, null, new Color(1f, 0.5f, 0.6f)),
            new PrizeSpec { id = "mini_toy_green", model = "prize_robot", name = "Mini-robot vert", rarity = Rarity.Rare, mass = 0.17f, shape = PrimitiveType.Cube, color = new Color(0.2f, 0.9f, 0.4f), scale = new Vector3(0.2f, 0.2f, 0.2f), count = 1, tickets = 5, xp = 15 },
            new PrizeSpec { id = "mini_toy_pink", model = "prize_robot", name = "Mini-robot rose", rarity = Rarity.Rare, mass = 0.17f, shape = PrimitiveType.Cube, color = new Color(1f, 0.4f, 0.75f), scale = new Vector3(0.2f, 0.2f, 0.2f), count = 1, tickets = 5, xp = 15 },
            // --- épiques
            P("plush_unicorn", "Licorne", "plush_unicorn", Rarity.Epic, new Color(0.98f, 0.96f, 1f), 1, 15, 40, 0.3f, 0.14f, new Color(1f, 0.9f, 0.95f), new Color(0.78f, 0.62f, 1f), new Color(1f, 0.82f, 0.3f)),
            P("plush_dino", "Dino", "plush_dino", Rarity.Epic, new Color(0.35f, 0.75f, 0.45f), 1, 15, 40, 0.32f, 0.14f, new Color(0.9f, 0.95f, 0.7f), new Color(1f, 0.6f, 0.2f)),
            P("prize_box", "Figurine en boîte", "prize_box", Rarity.Epic, new Color(0.2f, 0.45f, 0.95f), 1, 15, 40, 0.24f, 0.2f, new Color(0.85f, 0.95f, 1f), new Color(1f, 0.85f, 0.2f), new Color(1f, 0.35f, 0.55f)),
            new PrizeSpec { id = "capsule_purple", model = "prize_capsule", name = "Capsule surprise", rarity = Rarity.Epic, mass = 0.2f, shape = PrimitiveType.Capsule, color = new Color(0.7f, 0.3f, 1f), scale = new Vector3(0.14f, 0.12f, 0.14f), count = 1, tickets = 15, xp = 40 },
            new PrizeSpec { id = "capsule_cyan", model = "prize_capsule", name = "Capsule turquoise", rarity = Rarity.Epic, mass = 0.2f, shape = PrimitiveType.Capsule, color = new Color(0.2f, 0.9f, 1f), scale = new Vector3(0.14f, 0.12f, 0.14f), count = 1, tickets = 15, xp = 40, glow = true },
            new PrizeSpec { id = "figurine_silver", model = "prize_cat_gold", name = "Figurine argentée", rarity = Rarity.Epic, mass = 0.3f, shape = PrimitiveType.Cube, color = new Color(0.8f, 0.85f, 0.95f), scale = new Vector3(0.18f, 0.22f, 0.18f), count = 0, tickets = 30, xp = 80, glow = true },
            // --- légendaire
            new PrizeSpec { id = "figurine_gold", model = "prize_cat_gold", name = "Figurine dorée", rarity = Rarity.Legendary, mass = 0.3f, shape = PrimitiveType.Cube, color = new Color(1f, 0.8f, 0.15f), scale = new Vector3(0.18f, 0.22f, 0.18f), count = 1, tickets = 50, xp = 120, glow = true },
        };

        /// <summary>Données d'un lot de la pince (réutilisées par le pousse-pièces) : id = plush_pink, plush_blue, mini_toy_green, capsule_purple, figurine_gold.</summary>
        public static RewardData GetReward(string id)
        {
            ProjectSetup.EnsureFolder(DataDir + "/Rewards");
            foreach (var s in Specs) if (s.id == id) return EnsureReward(s);
            return null;
        }

        /// <summary>Modèle FBX et couleur d'un lot de la pince.</summary>
        public static bool GetPrizeLook(string id, out string model, out Color color, out Rarity rarity)
        {
            foreach (var s in Specs)
                if (s.id == id) { model = s.model; color = s.color; rarity = s.rarity; return true; }
            model = null; color = Color.white; rarity = Rarity.Common;
            return false;
        }

        public static ClawMachine Build(Transform parent, Vector3 pos, float rotY)
        {
            ProjectSetup.EnsureFolder(DataDir + "/Rewards");
            ProjectSetup.EnsureFolder(MatDir);

            plushPhys = EnsurePhysicsMaterial("Plush_Physics", 0.85f, 0.7f, 0.08f);
            hardPhys = EnsurePhysicsMaterial("Hard_Physics", 0.5f, 0.4f, 0.2f);
            var feltPhys = EnsurePhysicsMaterial("Felt_Physics", 0.7f, 0.6f, 0.05f);
            MachineData data = EnsureMachineData();
            var rewards = new Dictionary<string, RewardData>();
            foreach (var s in Specs) rewards[s.id] = EnsureReward(s);

            var body = RoomBuilder.Mat("Claw_Body", new Color(0.09f, 0.09f, 0.13f), 0.5f);
            var floorMat = RoomBuilder.Mat("Claw_Floor", new Color(0.22f, 0.18f, 0.3f), 0.3f);
            var cyan = RoomBuilder.Neon("Neon_Cyan", new Color(0.1f, 0.9f, 1f));
            var pink = RoomBuilder.Neon("Neon_Pink", new Color(1f, 0.1f, 0.6f));
            var yellow = RoomBuilder.Neon("Neon_Yellow", new Color(1f, 0.85f, 0.1f));
            var metal = RoomBuilder.Mat("Claw_Metal", new Color(0.7f, 0.72f, 0.78f), 0.8f);
            var glass = Glass();

            var root = new GameObject("ClawMachine_Classic").transform;
            root.SetParent(parent, false);
            root.position = pos;
            root.rotation = Quaternion.Euler(0f, rotY, 0f);

            // --- Caisson
            B("BaseLower", root, new Vector3(0, 0.15f, 0), new Vector3(1.4f, 0.3f, 1.2f), body, true);
            B("Shell_L", root, new Vector3(-0.65f, 0.425f, 0), new Vector3(0.1f, 0.25f, 1.2f), body, true);
            B("Shell_R", root, new Vector3(0.65f, 0.425f, 0), new Vector3(0.1f, 0.25f, 1.2f), body, true);
            B("Shell_B", root, new Vector3(0, 0.425f, 0.55f), new Vector3(1.2f, 0.25f, 0.1f), body, true);
            B("Shell_F", root, new Vector3(0, 0.425f, -0.55f), new Vector3(1.2f, 0.25f, 0.1f), body, true);
            // sol de jeu avec trappe (x[-0.6;-0.2] z[-0.5;-0.1] ouvert)
            B("PlayFloor_A", root, new Vector3(0.2f, 0.525f, 0), new Vector3(0.8f, 0.05f, 1.0f), floorMat, true);
            B("PlayFloor_B", root, new Vector3(-0.4f, 0.525f, 0.2f), new Vector3(0.4f, 0.05f, 0.6f), floorMat, true);
            B("ChuteGlow", root, new Vector3(-0.4f, 0.305f, -0.3f), new Vector3(0.38f, 0.01f, 0.38f), yellow, false);
            // garde en plexiglas autour de la trappe (comme sur les vraies machines : un lot ne tombe pas dedans par hasard)
            B("ChuteGuard_X", root, new Vector3(-0.2f, 0.62f, -0.3f), new Vector3(0.012f, 0.14f, 0.4f), glass, true);
            B("ChuteGuard_Z", root, new Vector3(-0.4f, 0.62f, -0.1f), new Vector3(0.4f, 0.14f, 0.012f), glass, true);
            B("ChuteGuardTrim_X", root, new Vector3(-0.2f, 0.693f, -0.3f), new Vector3(0.016f, 0.008f, 0.4f), yellow, false);
            B("ChuteGuardTrim_Z", root, new Vector3(-0.4f, 0.693f, -0.1f), new Vector3(0.4f, 0.008f, 0.016f), yellow, false);

            // --- Vitres, montants, plafond
            foreach (var p in new[] { new Vector2(-0.62f, -0.52f), new Vector2(0.62f, -0.52f), new Vector2(-0.62f, 0.52f), new Vector2(0.62f, 0.52f) })
                B("Post", root, new Vector3(p.x, 1.15f, p.y), new Vector3(0.06f, 1.2f, 0.06f), body, true);
            B("Glass_F", root, new Vector3(0, 1.15f, -0.53f), new Vector3(1.2f, 1.2f, 0.02f), glass, true);
            B("Glass_B", root, new Vector3(0, 1.15f, 0.53f), new Vector3(1.2f, 1.2f, 0.02f), glass, true);
            B("Glass_L", root, new Vector3(-0.63f, 1.15f, 0), new Vector3(0.02f, 1.2f, 1.0f), glass, true);
            B("Glass_R", root, new Vector3(0.63f, 1.15f, 0), new Vector3(0.02f, 1.2f, 1.0f), glass, true);
            B("Top", root, new Vector3(0, 1.8f, 0), new Vector3(1.4f, 0.1f, 1.2f), body, true);
            B("Marquee", root, new Vector3(0, 2.0f, -0.1f), new Vector3(1.4f, 0.3f, 0.8f), cyan, false);
            B("Rail", root, new Vector3(0, 1.72f, 0), new Vector3(1.2f, 0.03f, 0.03f), metal, false);

            // --- Pupitre de commande (joystick + bouton réellement animés par ClawMachine)
            B("Podium", root, new Vector3(0, 0.3f, -0.85f), new Vector3(1.4f, 0.6f, 0.5f), body, true);
            B("Deck", root, new Vector3(0, 0.625f, -0.85f), new Vector3(1.4f, 0.05f, 0.5f), body, true);
            var stickPivot = new GameObject("JoystickPivot").transform;
            stickPivot.SetParent(root, false);
            stickPivot.localPosition = new Vector3(-0.3f, 0.65f, -0.85f);
            var stick = Prim(PrimitiveType.Cylinder, "Stick", stickPivot, new Vector3(0, 0.1f, 0), new Vector3(0.03f, 0.1f, 0.03f), metal);
            Prim(PrimitiveType.Sphere, "StickBall", stickPivot, new Vector3(0, 0.22f, 0), new Vector3(0.07f, 0.07f, 0.07f), pink);
            var button = Prim(PrimitiveType.Cylinder, "DropButton", root, new Vector3(0.3f, 0.665f, -0.85f), new Vector3(0.13f, 0.015f, 0.13f), pink);

            // --- Tête de pince (Rigidbody cinématique) + câble + 3 branches
            var headGo = new GameObject("ClawHead");
            headGo.transform.SetParent(root, false);
            headGo.transform.localPosition = new Vector3(-0.4f, 1.55f, -0.3f);
            var rb = headGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Prim(PrimitiveType.Cylinder, "Housing", headGo.transform, new Vector3(0, 0.02f, 0), new Vector3(0.14f, 0.025f, 0.14f), metal, false);

            var pivots = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                var pivot = new GameObject("ProngPivot_" + i).transform;
                pivot.SetParent(headGo.transform, false);
                pivot.localPosition = new Vector3(Mathf.Cos(a) * 0.06f, -0.03f, Mathf.Sin(a) * 0.06f);
                var prong = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prong.name = "Prong";
                prong.transform.SetParent(pivot, false);
                prong.transform.localPosition = new Vector3(0, -0.1f, 0);
                prong.transform.localScale = new Vector3(0.028f, 0.2f, 0.028f);
                prong.GetComponent<MeshRenderer>().sharedMaterial = metal;
                pivots[i] = pivot;
            }
            var hold = new GameObject("HoldPoint").transform;
            hold.SetParent(headGo.transform, false);
            hold.localPosition = new Vector3(0, -0.16f, 0);

            var cable = Prim(PrimitiveType.Cylinder, "Cable", root, new Vector3(-0.4f, 1.64f, -0.3f), new Vector3(0.012f, 0.08f, 0.012f), metal, false);

            // --- Habillage détaillé : coque FBX texturée ; les primitives de la coque restent comme volumes physiques invisibles
            var pal = ModelLib.Palette(null);
            ModelLib.Textured(floorMat, "felt", null, 2f, 0f, 0.15f);
            ModelLib.Textured(metal, "metal", null, 1.5f, 0.9f, 0.7f);
            var shell = ModelLib.Spawn("claw_shell", root, pal);
            if (shell != null)
            {
                var hide = new HashSet<string> { "BaseLower", "Shell_L", "Shell_R", "Shell_B", "Shell_F", "Post", "Top", "Marquee", "Rail", "Podium", "Deck" };
                var toHide = new List<Transform>();
                foreach (Transform c in root) if (hide.Contains(c.name)) toHide.Add(c);
                foreach (var c in toHide)
                {
                    if (c.GetComponent<Collider>() == null) Object.DestroyImmediate(c.gameObject);
                    else c.GetComponent<Renderer>().enabled = false;
                }
            }
            var stickModel = ModelLib.Spawn("arcade_stick", stickPivot, pal);
            if (stickModel != null)
            {
                stick.GetComponent<Renderer>().enabled = false;
                var ball = stickPivot.Find("StickBall");
                if (ball != null) ball.GetComponent<Renderer>().enabled = false;
            }
            ModelLib.Dress(button.transform, "arcade_button", pal, 0.65f);
            if (ModelLib.Dress(headGo.transform, "claw_head", pal, 1f, false)) headGo.transform.Find("Housing").GetComponent<Renderer>().enabled = false;
            foreach (var pv in pivots)
                if (ModelLib.Dress(pv, "claw_prong", pal, 1f, false)) pv.Find("Prong").GetComponent<Renderer>().enabled = false;

            // --- Ancrage caméra devant la machine
            var anchor = new GameObject("CameraAnchor").transform;
            anchor.SetParent(root, false);
            anchor.localPosition = new Vector3(0, 1.35f, -2.2f);
            anchor.localRotation = Quaternion.LookRotation(new Vector3(0, 1.0f, 0) - anchor.localPosition);

            // --- Machine + zone de récupération
            foreach (string fl in new[] { "PlayFloor_A", "PlayFloor_B" })
            {
                var ft = root.Find(fl);
                if (ft != null && ft.GetComponent<Collider>() != null) ft.GetComponent<Collider>().sharedMaterial = feltPhys;
            }

            // --- Portique : pont (axe X) et chariot qui se déplacent réellement
            var bridgeT = new GameObject("Bridge").transform;
            bridgeT.SetParent(root, false);
            bridgeT.localPosition = new Vector3(0f, 1.72f, -0.3f);
            ModelLib.Spawn("claw_bridge", bridgeT, pal);
            var carriageT = new GameObject("Carriage").transform;
            carriageT.SetParent(root, false);
            carriageT.localPosition = new Vector3(-0.4f, 1.665f, -0.3f);
            ModelLib.Spawn("claw_carriage", carriageT, pal);

            // --- Monnayeur : pièce animée à l'insertion
            var coinMat = RoomBuilder.Mat("Claw_Coin", new Color(1f, 0.8f, 0.15f), 0.8f);
            ModelLib.Textured(coinMat, "metal", null, 1f, 0.9f, 0.8f);
            var coinVis = Prim(PrimitiveType.Cylinder, "InsertCoin", root, new Vector3(0f, 0.4f, -1.14f), new Vector3(0.06f, 0.004f, 0.06f), coinMat, false);
            coinVis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            coinVis.SetActive(false);

            // --- Afficheur numérique sous le plafond, face au joueur (derrière la vitre)
            var frame = Prim(PrimitiveType.Cube, "DisplayFrame", root, new Vector3(0f, 1.665f, -0.507f), new Vector3(1.0f, 0.17f, 0.012f), body, false);
            var display = new GameObject("Display").transform;
            display.SetParent(root, false);
            display.localPosition = new Vector3(0f, 1.665f, -0.516f);

            // --- LED du plafond animées par le script
            var leds = new Light[4];
            var lpos = new[] { new Vector3(-0.5f, 1.68f, -0.35f), new Vector3(0.5f, 1.68f, -0.35f), new Vector3(-0.5f, 1.68f, 0.35f), new Vector3(0.5f, 1.68f, 0.35f) };
            for (int i = 0; i < 4; i++)
            {
                var lg = new GameObject("Light_" + i);
                lg.transform.SetParent(root, false);
                lg.transform.localPosition = lpos[i];
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point; l.range = 1.4f; l.intensity = 1f; l.shadows = LightShadows.None; l.color = new Color(1f, 0.3f, 0.7f);
                leds[i] = l;
            }

            var machine = root.gameObject.AddComponent<ClawMachine>();
            machine.Configure(data, anchor, rb, cable.transform, pivots, hold, stickPivot, button.transform);
            machine.SetupExtras(bridgeT, carriageT, coinVis.transform, display, leds);

            var chute = new GameObject("ChuteZone");
            chute.transform.SetParent(root, false);
            chute.transform.localPosition = new Vector3(-0.4f, 0.43f, -0.3f);
            var bc = chute.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(0.4f, 0.25f, 0.4f);
            chute.AddComponent<ChuteZone>().Configure(machine);

            // --- Lots physiques
            SpawnPrizes(root, rewards);
            return machine;
        }

        /// <summary>Crée le prefab de la machine (placée dynamiquement par BuildingManager) et le relie à sa MachineData.</summary>
        public static MachineData EnsurePrefab()
        {
            ProjectSetup.EnsureFolder("Assets/_Project/Prefabs");
            MachineData data = EnsureMachineData();
            var go = Build(null, Vector3.zero, 0f).gameObject;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            data.prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/_Project/Prefabs/ClawMachine_Classic.prefab");
            Object.DestroyImmediate(go);
            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>Palette de matériaux d'un lot : tissu de peluche texturé (fourrure), yeux brillants, fil brodé, étiquette, plastique.</summary>
        public static Dictionary<string, Material> PrizePalette(string id)
        {
            PrizeSpec s = null;
            foreach (var sp in Specs) if (sp.id == id) s = sp;
            if (s == null) return ModelLib.Palette(null);
            var main = RoomBuilder.Mat("Prize_" + s.id, s.color, 0.12f);
            var pal = ModelLib.Palette(main);
            if (s.plush)
            {
                ModelLib.Textured(main, "plush", null, 1f, 0f, 0.12f);
                pal["Light"] = ModelLib.Textured(RoomBuilder.Mat("Prize_" + s.id + "_Light", s.light, 0.12f), "plush", null, 1f, 0f, 0.12f);
                pal["Second"] = ModelLib.Textured(RoomBuilder.Mat("Prize_" + s.id + "_Second", s.second, 0.12f), "plush", null, 1f, 0f, 0.12f);
                pal["Accent"] = ModelLib.Textured(RoomBuilder.Mat("Prize_" + s.id + "_Accent", s.accent, 0.25f), "fabric", null, 2f, 0f, 0.25f);
            }
            else
            {
                switch (s.id)
                {
                    case "figurine_gold": ModelLib.Textured(main, "gold", null, 1f, 0.9f, 0.65f); break;
                    case "figurine_silver": ModelLib.Textured(main, "metal", null, 1f, 0.9f, 0.75f); break;
                    case "prize_box":
                        ModelLib.Textured(main, "plastic", null, 1f, 0f, 0.35f);
                        pal["Light"] = RoomBuilder.Mat("Prize_Box_Window", s.light, 0.95f);
                        pal["Second"] = RoomBuilder.Mat("Prize_" + s.id + "_Second", s.second, 0.4f);
                        pal["Accent"] = RoomBuilder.Mat("Prize_" + s.id + "_Accent", s.accent, 0.6f);
                        break;
                    default: ModelLib.Textured(main, "plastic", null, 1f, 0f, 0.6f); break;
                }
                if (s.glow) { main.EnableKeyword("_EMISSION"); main.SetColor("_EmissionColor", s.color * 0.6f); }
            }
            return pal;
        }

        /// <summary>Crée un lot physique (modèle FBX + colliders adaptés à sa forme). Utilisé par la pince et le pousse-pièces.</summary>
        public static GameObject CreatePrize(string id, Transform parent, float sizeOverride = -1f)
        {
            PrizeSpec s = null;
            foreach (var sp in Specs) if (sp.id == id) s = sp;
            if (s == null) return null;
            var visual = ModelLib.Spawn(s.model, null, PrizePalette(id));
            if (visual == null) return null;
            var go = new GameObject("Prize_" + s.id);
            go.transform.SetParent(parent, false);
            visual.transform.SetParent(go.transform, false);
            var raw = ModelLib.LocalBounds(go.transform);
            float target = sizeOverride > 0f ? sizeOverride : (s.size > 0f ? s.size : s.scale.y * 1.25f);
            float u = target / Mathf.Max(raw.size.x, raw.size.y, raw.size.z);
            go.transform.localScale = new Vector3(u, u, u);
            var b = ModelLib.LocalBounds(go.transform);   // dans le repère du lot (non mis à l'échelle)
            if (s.plush)
            {
                // peluche : deux sphères (corps + tête) — elle roule et s'emboîte dans le tas comme un objet mou
                Vector3 c = b.center, e = b.extents;
                var body = go.AddComponent<SphereCollider>();
                body.center = c - new Vector3(0f, e.y * 0.38f, 0f);
                body.radius = Mathf.Min(Mathf.Max(e.x, e.z) * 0.92f, e.y * 0.62f);
                var headC = go.AddComponent<SphereCollider>();
                headC.center = c + new Vector3(0f, e.y * 0.36f, e.z * 0.05f);
                headC.radius = Mathf.Min(e.x, e.z, e.y * 0.55f) * 0.9f;
            }
            else if (s.shape == PrimitiveType.Capsule) { var sc = go.AddComponent<SphereCollider>(); sc.center = b.center; sc.radius = Mathf.Min(b.extents.x, b.extents.z); }
            else { var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size * 0.9f; }
            return go;
        }

        public static bool IsPlush(string id) { foreach (var sp in Specs) if (sp.id == id) return sp.plush; return false; }

        static void SpawnPrizes(Transform root, Dictionary<string, RewardData> rewards)
        {
            var group = new GameObject("Prizes").transform;
            group.SetParent(root, false);

            // liste des lots (mélangée, graine fixe), puis empilés en couches au-dessus du sol de jeu, hors de la trappe
            var rng = new System.Random(11);
            var list = new List<PrizeSpec>();
            foreach (var s in Specs) for (int n = 0; n < s.count; n++) list.Add(s);
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (list[i], list[j]) = (list[j], list[i]); }

            var slots = new List<Vector2>();
            for (float x = -0.5f; x <= 0.52f; x += 0.17f)
                for (float z = -0.42f; z <= 0.44f; z += 0.17f)
                    if (!(x < -0.12f && z < 0.0f)) slots.Add(new Vector2(x, z));   // hors trappe + garde

            var bodies = new List<Rigidbody>();
            for (int k = 0; k < list.Count; k++)
            {
                var s = list[k];
                var go = CreatePrize(s.id, group);
                if (go == null) continue;
                go.name = "Prize_" + s.id + "_" + k;
                Vector2 sl = slots[k % slots.Count];
                int layer = k / slots.Count;
                float jx = (float)(rng.NextDouble() - 0.5) * 0.05f, jz = (float)(rng.NextDouble() - 0.5) * 0.05f;
                go.transform.localPosition = new Vector3(sl.x + jx, 0.75f + layer * 0.26f, sl.y + jz);
                // la plupart assises, quelques-unes couchées sur le côté ou sur le dos (comme dans un vrai bac)
                float tilt = rng.NextDouble() < 0.35 ? (float)(rng.NextDouble() * 90.0) : (float)(rng.NextDouble() * 15.0);
                go.transform.localRotation = Quaternion.Euler(tilt, rng.Next(360), (float)(rng.NextDouble() - 0.5) * 30f);
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = s.mass;
                rb.linearDamping = 0.35f;
                rb.angularDamping = 0.8f;
                foreach (var col in go.GetComponents<Collider>()) col.sharedMaterial = s.plush ? plushPhys : hardPhys;
                go.AddComponent<PrizeItem>().reward = rewards[s.id];
                bodies.Add(rb);
            }

            // le tas se tasse réellement (simulation physique pendant la construction) : le prefab contient un tas stable
            var prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (int i = 0; i < 360; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = prevMode;
            int fell = 0;
            foreach (var rb in bodies)
            {
                rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
                var lp = root.InverseTransformPoint(rb.position);
                if (lp.y < 0.5f || lp.x < -0.66f || lp.x > 0.66f || lp.z < -0.56f || lp.z > 0.56f) { Object.DestroyImmediate(rb.gameObject); fell++; }
            }
            Debug.Log($"[ClawBuilder] {bodies.Count} lots empilés, {fell} retirés (hors bac)");
        }

        static PhysicsMaterial plushPhys, hardPhys;

        static PhysicsMaterial EnsurePhysicsMaterial(string name, float staticF, float dynamicF, float bounce)
        {
            ProjectSetup.EnsureFolder("Assets/_Project/Physics");
            string path = "Assets/_Project/Physics/" + name + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (m == null)
            {
                m = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(m, path);
            }
            m.staticFriction = staticF; m.dynamicFriction = dynamicF; m.bounciness = bounce;
            m.frictionCombine = PhysicsMaterialCombine.Average;
            m.bounceCombine = PhysicsMaterialCombine.Average;
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void B(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider)
            => RoomBuilder.Box(name, parent, pos, size, mat, collider, true);

        static Material Glass()
        {
            string path = MatDir + "/Claw_Glass.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", new Color(0.6f, 0.85f, 1f, 0.12f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", 5f);
            m.SetFloat("_DstBlend", 10f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Smoothness", 0.95f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        static MachineData EnsureMachineData()
        {
            ProjectSetup.EnsureFolder(DataDir);
            string path = DataDir + "/Machine_Claw_Classic.asset";
            var d = AssetDatabase.LoadAssetAtPath<MachineData>(path);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<MachineData>();
                AssetDatabase.CreateAsset(d, path);
            }
            d.id = "claw_classic";
            d.displayName = "Pince classique";
            d.category = MachineCategory.Claw;
            d.costPerPlay = 5;
            d.purchasePrice = 500;
            d.unlockLevel = 1;
            d.footprintSize = new Vector2(1.5f, 2.2f);
            d.footprintCenterZ = -0.5f;
            EditorUtility.SetDirty(d);
            return d;
        }

        static RewardData EnsureReward(PrizeSpec s)
        {
            string path = DataDir + "/Rewards/Reward_" + s.id + ".asset";
            var r = AssetDatabase.LoadAssetAtPath<RewardData>(path);
            if (r == null)
            {
                r = ScriptableObject.CreateInstance<RewardData>();
                AssetDatabase.CreateAsset(r, path);
            }
            r.id = s.id;
            r.displayName = s.name;
            r.rarity = s.rarity;
            r.ticketValue = s.tickets;
            r.experienceValue = s.xp;
            r.shopTicketPrice = s.tickets * 4;
            EditorUtility.SetDirty(r);
            return r;
        }
    }
}
#endif
