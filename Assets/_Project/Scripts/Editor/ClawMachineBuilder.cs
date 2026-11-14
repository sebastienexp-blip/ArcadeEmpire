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
        }

        static readonly PrizeSpec[] Specs =
        {
            new PrizeSpec { id = "plush_pink", model = "prize_bunny", name = "Peluche rose", rarity = Rarity.Common, mass = 0.12f, shape = PrimitiveType.Sphere, color = new Color(1f, 0.45f, 0.7f), scale = new Vector3(0.24f, 0.24f, 0.24f), count = 4, tickets = 1, xp = 5 },
            new PrizeSpec { id = "plush_blue", model = "prize_bear", name = "Peluche bleue", rarity = Rarity.Common, mass = 0.12f, shape = PrimitiveType.Sphere, color = new Color(0.35f, 0.6f, 1f), scale = new Vector3(0.24f, 0.24f, 0.24f), count = 4, tickets = 1, xp = 5 },
            new PrizeSpec { id = "mini_toy_green", model = "prize_robot", name = "Mini-jouet vert", rarity = Rarity.Rare, mass = 0.17f, shape = PrimitiveType.Cube, color = new Color(0.2f, 0.9f, 0.4f), scale = new Vector3(0.22f, 0.22f, 0.22f), count = 4, tickets = 5, xp = 15 },
            new PrizeSpec { id = "capsule_purple", model = "prize_capsule", name = "Capsule surprise", rarity = Rarity.Epic, mass = 0.24f, shape = PrimitiveType.Capsule, color = new Color(0.7f, 0.3f, 1f), scale = new Vector3(0.17f, 0.14f, 0.17f), count = 2, tickets = 15, xp = 40 },
            new PrizeSpec { id = "figurine_gold", model = "prize_cat_gold", name = "Figurine dorée", rarity = Rarity.Legendary, mass = 0.34f, shape = PrimitiveType.Cube, color = new Color(1f, 0.8f, 0.15f), scale = new Vector3(0.2f, 0.26f, 0.2f), count = 1, tickets = 50, xp = 120, glow = true },
        };

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

        static void SpawnPrizes(Transform root, Dictionary<string, RewardData> rewards)
        {
            var group = new GameObject("Prizes").transform;
            group.SetParent(root, false);

            // emplacements en grille hors de la trappe, mélangés avec une graine fixe
            var slots = new List<Vector2>();
            for (float x = -0.45f; x <= 0.5f; x += 0.19f)
                for (float z = -0.3f; z <= 0.4f; z += 0.2f)
                    if (!(x < -0.1f && z < 0.05f)) slots.Add(new Vector2(x, z));
            var rng = new System.Random(7);
            for (int i = slots.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (slots[i], slots[j]) = (slots[j], slots[i]); }

            int k = 0;
            foreach (var s in Specs)
            {
                var mat = RoomBuilder.Mat("Prize_" + s.id, s.color, 0.35f);
                switch (s.id)
                {
                    case "plush_pink": case "plush_blue": ModelLib.Textured(mat, "fabric", null, 1f, 0f, 0.2f); break;
                    case "figurine_gold": ModelLib.Textured(mat, "gold", null, 1f, 0.9f, 0.65f); break;
                    default: ModelLib.Textured(mat, "plastic", null, 1f, 0f, 0.6f); break;
                }
                if (s.glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", s.color * 0.8f); }
                for (int n = 0; n < s.count && k < slots.Count; n++, k++)
                {
                    GameObject go;
                    var visual = ModelLib.Spawn(s.model, null, ModelLib.Palette(mat));
                    if (visual != null)
                    {
                        // modèle FBX : racine physique (scale uniforme) + visuel enfant + collider ajusté au modèle
                        go = new GameObject("Prize_" + s.id + "_" + n);
                        go.transform.SetParent(group, false);
                        visual.transform.SetParent(go.transform, false);
                        var raw = ModelLib.LocalBounds(go.transform);
                        float u = s.scale.y * 1.25f / Mathf.Max(raw.size.x, raw.size.y, raw.size.z);   // plus grande dimension = 1,25 x l'ancienne hauteur
                        go.transform.localScale = new Vector3(u, u, u);
                        var b = ModelLib.LocalBounds(go.transform);
                        if (s.shape == PrimitiveType.Capsule) { var sc = go.AddComponent<SphereCollider>(); sc.center = b.center; sc.radius = Mathf.Min(b.extents.x, b.extents.z); }
                        else { var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size * 0.85f; }
                    }
                    else
                    {
                        go = GameObject.CreatePrimitive(s.shape);
                        go.name = "Prize_" + s.id + "_" + n;
                        go.transform.SetParent(group, false);
                        go.transform.localScale = s.scale;
                        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                    }
                    go.transform.localPosition = new Vector3(slots[k].x, 0.72f + (k % 3) * 0.16f, slots[k].y);
                    go.transform.localRotation = Quaternion.Euler(0, rng.Next(360), 0);
                    var rb = go.AddComponent<Rigidbody>();
                    rb.mass = s.mass;
                    rb.linearDamping = 0.35f;
                    rb.angularDamping = 0.6f;
                    var pcol = go.GetComponent<Collider>();
                    if (pcol != null) pcol.sharedMaterial = (s.rarity == Rarity.Common) ? plushPhys : hardPhys;
                    go.AddComponent<PrizeItem>().reward = rewards[s.id];
                }
            }
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
