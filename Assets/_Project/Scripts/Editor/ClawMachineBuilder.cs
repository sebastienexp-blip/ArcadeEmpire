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
            public Color color; public Vector3 scale; public int count, tickets; public long xp; public bool glow;
        }

        static readonly PrizeSpec[] Specs =
        {
            new PrizeSpec { id = "plush_pink", name = "Peluche rose", rarity = Rarity.Common, shape = PrimitiveType.Sphere, color = new Color(1f, 0.45f, 0.7f), scale = new Vector3(0.24f, 0.24f, 0.24f), count = 4, tickets = 1, xp = 5 },
            new PrizeSpec { id = "plush_blue", name = "Peluche bleue", rarity = Rarity.Common, shape = PrimitiveType.Sphere, color = new Color(0.35f, 0.6f, 1f), scale = new Vector3(0.24f, 0.24f, 0.24f), count = 4, tickets = 1, xp = 5 },
            new PrizeSpec { id = "mini_toy_green", name = "Mini-jouet vert", rarity = Rarity.Rare, shape = PrimitiveType.Cube, color = new Color(0.2f, 0.9f, 0.4f), scale = new Vector3(0.22f, 0.22f, 0.22f), count = 4, tickets = 5, xp = 15 },
            new PrizeSpec { id = "capsule_purple", name = "Capsule surprise", rarity = Rarity.Epic, shape = PrimitiveType.Capsule, color = new Color(0.7f, 0.3f, 1f), scale = new Vector3(0.17f, 0.14f, 0.17f), count = 2, tickets = 15, xp = 40 },
            new PrizeSpec { id = "figurine_gold", name = "Figurine dorée", rarity = Rarity.Legendary, shape = PrimitiveType.Cube, color = new Color(1f, 0.8f, 0.15f), scale = new Vector3(0.2f, 0.26f, 0.2f), count = 1, tickets = 50, xp = 120, glow = true },
        };

        public static ClawMachine Build(Transform parent, Vector3 pos, float rotY)
        {
            ProjectSetup.EnsureFolder(DataDir + "/Rewards");
            ProjectSetup.EnsureFolder(MatDir);

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

            // --- Ancrage caméra devant la machine
            var anchor = new GameObject("CameraAnchor").transform;
            anchor.SetParent(root, false);
            anchor.localPosition = new Vector3(0, 1.35f, -2.2f);
            anchor.localRotation = Quaternion.LookRotation(new Vector3(0, 1.0f, 0) - anchor.localPosition);

            // --- Machine + zone de récupération
            var machine = root.gameObject.AddComponent<ClawMachine>();
            machine.Configure(data, anchor, rb, cable.transform, pivots, hold, stickPivot, button.transform);

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
            MachineData data = EnsureMachineData();
            ProjectSetup.EnsureFolder("Assets/_Project/Prefabs");
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
                if (s.glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", s.color * 0.8f); }
                for (int n = 0; n < s.count && k < slots.Count; n++, k++)
                {
                    var go = GameObject.CreatePrimitive(s.shape);
                    go.name = "Prize_" + s.id + "_" + n;
                    go.transform.SetParent(group, false);
                    go.transform.localPosition = new Vector3(slots[k].x, 0.72f + (k % 3) * 0.16f, slots[k].y);
                    go.transform.localRotation = Quaternion.Euler(0, rng.Next(360), 0);
                    go.transform.localScale = s.scale;
                    go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                    var rb = go.AddComponent<Rigidbody>();
                    rb.mass = 0.15f;
                    rb.linearDamping = 0.4f;
                    rb.angularDamping = 0.6f;
                    go.AddComponent<PrizeItem>().reward = rewards[s.id];
                }
            }
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
