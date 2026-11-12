#if UNITY_EDITOR
using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Machines;
using UnityEditor;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>
    /// Construit le coin pusher avec des PRIMITIVES Unity (modèle provisoire). Repère local : façade du côté -Z,
    /// plateau y = 0,9 (z de -0,55 à 0,95), poussoir cinématique, zone de récupération sous le bord avant.
    /// </summary>
    public static class CoinPusherBuilder
    {
        const string DataDir = "Assets/_Project/Data";
        const string MatDir = "Assets/_Project/Materials";

        public static CoinPusherMachine Build(Transform parent, Vector3 pos, float rotY)
        {
            ProjectSetup.EnsureFolder(DataDir + "/Rewards");
            ProjectSetup.EnsureFolder(MatDir);

            var data = EnsureMachineData();
            var tokenSmall = EnsureReward("token_tickets", "Jeton tickets", Rarity.Rare, 10, 15);
            var tokenJackpot = EnsureReward("token_jackpot", "Jeton Jackpot", Rarity.Epic, 40, 50);

            var body = RoomBuilder.Mat("Pusher_Body", new Color(0.09f, 0.08f, 0.13f), 0.5f);
            var tray = RoomBuilder.Mat("Pusher_Tray", new Color(0.30f, 0.14f, 0.45f), 0.3f);
            var plateMat = RoomBuilder.Mat("Pusher_Plate", new Color(0.8f, 0.82f, 0.9f), 0.7f);
            var pink = RoomBuilder.Neon("Neon_Pink", new Color(1f, 0.1f, 0.6f));
            var cyan = RoomBuilder.Neon("Neon_Cyan", new Color(0.1f, 0.9f, 1f));
            var yellow = RoomBuilder.Neon("Neon_Yellow", new Color(1f, 0.85f, 0.1f));
            var glass = ClawGlass();
            var coinMat = RoomBuilder.Mat("Pusher_Coin", new Color(1f, 0.78f, 0.1f), 0.75f);
            var tokenMat = RoomBuilder.Mat("Pusher_Token", new Color(0.2f, 0.9f, 0.5f), 0.6f);
            var jackpotMat = RoomBuilder.Mat("Pusher_Jackpot", new Color(0.8f, 0.3f, 1f), 0.6f);

            var root = new GameObject("CoinPusher_Classic").transform;
            root.SetParent(parent, false);
            root.position = pos;
            root.rotation = Quaternion.Euler(0f, rotY, 0f);

            // --- Caisson et plateau
            B("BaseLower", root, new Vector3(0, 0.25f, 0.025f), new Vector3(1.5f, 0.5f, 1.85f), body, true);
            B("Tray", root, new Vector3(0, 0.7f, 0.2f), new Vector3(1.3f, 0.4f, 1.5f), tray, true);
            B("Bin_L", root, new Vector3(-0.7f, 0.7f, -0.725f), new Vector3(0.1f, 0.4f, 0.35f), body, true);
            B("Bin_R", root, new Vector3(0.7f, 0.7f, -0.725f), new Vector3(0.1f, 0.4f, 0.35f), body, true);
            B("Bin_Front", root, new Vector3(0, 0.7f, -0.925f), new Vector3(1.5f, 0.4f, 0.05f), body, true);
            B("BinGlow", root, new Vector3(0, 0.505f, -0.725f), new Vector3(1.2f, 0.01f, 0.3f), yellow, false);

            // --- Parois et plafond
            B("Wall_L", root, new Vector3(-0.675f, 1.2f, 0.2f), new Vector3(0.05f, 0.6f, 1.5f), glass, true);
            B("Wall_R", root, new Vector3(0.675f, 1.2f, 0.2f), new Vector3(0.05f, 0.6f, 1.5f), glass, true);
            B("Wall_Back", root, new Vector3(0, 1.2f, 0.975f), new Vector3(1.4f, 0.6f, 0.05f), body, true);
            B("Top", root, new Vector3(0, 1.62f, 0.2f), new Vector3(1.5f, 0.25f, 1.6f), body, true);
            B("Marquee", root, new Vector3(0, 1.95f, 0.45f), new Vector3(1.5f, 0.4f, 1.0f), pink, false);
            B("FrontPillar_L", root, new Vector3(-0.7f, 1.2f, -0.55f), new Vector3(0.1f, 0.6f, 0.1f), body, true);
            B("FrontPillar_R", root, new Vector3(0.7f, 1.2f, -0.55f), new Vector3(0.1f, 0.6f, 0.1f), body, true);

            // --- Poussoir (Rigidbody cinématique, déplacé par CoinPusherMachine)
            var pusherGo = new GameObject("Pusher");
            pusherGo.transform.SetParent(root, false);
            pusherGo.transform.localPosition = new Vector3(0, 0.96f, 0.70f);
            var prb = pusherGo.AddComponent<Rigidbody>();
            prb.isKinematic = true;
            prb.interpolation = RigidbodyInterpolation.Interpolate;
            prb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var pcol = pusherGo.AddComponent<BoxCollider>();
            pcol.size = new Vector3(1.28f, 0.12f, 0.5f);
            var pvis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pvis.name = "PlateVisual";
            Object.DestroyImmediate(pvis.GetComponent<BoxCollider>());
            pvis.transform.SetParent(pusherGo.transform, false);
            pvis.transform.localScale = new Vector3(1.28f, 0.12f, 0.5f);
            pvis.GetComponent<MeshRenderer>().sharedMaterial = plateMat;

            // --- Commandes : indicateur de visée (se déplace réellement) et bouton (s'enfonce à chaque pièce)
            var aim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aim.name = "AimIndicator";
            Object.DestroyImmediate(aim.GetComponent<BoxCollider>());
            aim.transform.SetParent(root, false);
            aim.transform.localPosition = new Vector3(0, 1.48f, 0.25f);
            aim.transform.localScale = new Vector3(0.14f, 0.03f, 0.14f);
            aim.GetComponent<MeshRenderer>().sharedMaterial = yellow;

            B("Podium", root, new Vector3(0, 0.3f, -1.15f), new Vector3(1.0f, 0.6f, 0.4f), body, true);
            B("Deck", root, new Vector3(0, 0.625f, -1.15f), new Vector3(1.0f, 0.05f, 0.4f), body, true);
            var button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            button.name = "DropButton";
            Object.DestroyImmediate(button.GetComponent<Collider>());
            button.transform.SetParent(root, false);
            button.transform.localPosition = new Vector3(0, 0.665f, -1.15f);
            button.transform.localScale = new Vector3(0.16f, 0.015f, 0.16f);
            button.GetComponent<MeshRenderer>().sharedMaterial = cyan;

            // --- Modèle de pièce (désactivé, copié par le pool) + pièces et lots posés sur le plateau
            var template = BuildCoin("CoinTemplate", root, new Vector3(0, 2.5f, 0), 0f, coinMat, 0.1f, 0.02f, null);
            template.SetActive(false);

            var coins = new GameObject("Coins").transform;
            coins.SetParent(root, false);
            int index = 0;
            for (float z = -0.45f; z <= -0.1f; z += 0.13f)
                for (float x = -0.52f; x <= 0.53f; x += 0.13f)
                {
                    index++;
                    Vector3 p = new Vector3(x, 0.92f, z);
                    float yaw = (index * 47) % 360;
                    if (index == 6 || index == 15 || index == 22)
                        BuildCoin("Token_" + index, coins, p, yaw, tokenMat, 0.14f, 0.03f, tokenSmall);
                    else if (index == 12)
                        BuildCoin("Jackpot_" + index, coins, p, yaw, jackpotMat, 0.16f, 0.035f, tokenJackpot);
                    else
                        BuildCoin("Coin_" + index, coins, p, yaw, coinMat, 0.1f, 0.02f, null);
                }

            // --- Habillage détaillé : coque FBX texturée ; les primitives restent comme volumes physiques invisibles
            var pal = ModelLib.Palette(null);
            ModelLib.Textured(tray, "felt", null, 3f, 0f, 0.15f);
            var shell = ModelLib.Spawn("pusher_shell", root, pal);
            if (shell != null)
            {
                var hide = new HashSet<string> { "BaseLower", "Bin_L", "Bin_R", "Bin_Front", "Wall_Back", "Top", "Marquee", "FrontPillar_L", "FrontPillar_R", "Podium", "Deck" };
                var toHide = new List<Transform>();
                foreach (Transform c in root) if (hide.Contains(c.name)) toHide.Add(c);
                foreach (var c in toHide)
                {
                    if (c.GetComponent<Collider>() == null) Object.DestroyImmediate(c.gameObject);
                    else c.GetComponent<Renderer>().enabled = false;
                }
            }
            if (ModelLib.Dress(pusherGo.transform, "pusher_plate", pal, 1f, false)) pvis.GetComponent<Renderer>().enabled = false;
            ModelLib.Dress(aim.transform, "pusher_aim", pal);
            ModelLib.Dress(button.transform, "arcade_button", ModelLib.WithButtonColor(pal, new Color(0.1f, 0.9f, 1f)), 0.65f);

            // --- Ancrage caméra
            var anchor = new GameObject("CameraAnchor").transform;
            anchor.SetParent(root, false);
            anchor.localPosition = new Vector3(0, 1.75f, -2.4f);
            anchor.localRotation = Quaternion.LookRotation(new Vector3(0, 0.95f, 0.2f) - anchor.localPosition);

            var machine = root.gameObject.AddComponent<CoinPusherMachine>();
            machine.Configure(data, anchor, prb, aim.transform, button.transform, template.transform, coins);

            // --- Zone de récupération sous le bord avant
            var zone = new GameObject("Collector");
            zone.transform.SetParent(root, false);
            zone.transform.localPosition = new Vector3(0, 0.68f, -0.72f);
            var bc = zone.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(1.3f, 0.34f, 0.32f);
            zone.AddComponent<PusherCollector>().Configure(machine);

            return machine;
        }

        /// <summary>Crée le prefab de la machine (placée dynamiquement par BuildingManager) et le relie à sa MachineData.</summary>
        public static MachineData EnsurePrefab()
        {
            ProjectSetup.EnsureFolder("Assets/_Project/Prefabs");
            MachineData data = EnsureMachineData();
            var go = Build(null, Vector3.zero, 0f).gameObject;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            data.prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/_Project/Prefabs/CoinPusher_Classic.prefab");
            Object.DestroyImmediate(go);
            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>Pièce ou jeton : cylindre plat, collider convexe, Rigidbody à détection continue.</summary>
        static GameObject BuildCoin(string name, Transform parent, Vector3 localPos, float yaw, Material mat, float diameter, float thickness, RewardData reward)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(diameter, thickness * 0.5f, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
            var meshCol = go.AddComponent<MeshCollider>();
            meshCol.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;   // collider simple (cylindre), le visuel détaillé est séparé
            meshCol.convex = true;
            var detail = ModelLib.Spawn("coin_unit", null, ModelLib.CoinPalette(mat.name, mat.GetColor("_BaseColor")));
            if (detail != null)
            {
                var dmf = detail.GetComponentInChildren<MeshFilter>(); var dr = detail.GetComponentInChildren<MeshRenderer>();
                if (dmf != null && dr != null)
                {
                    go.GetComponent<MeshFilter>().sharedMesh = dmf.sharedMesh;
                    go.GetComponent<MeshRenderer>().sharedMaterials = dr.sharedMaterials;
                }
                Object.DestroyImmediate(detail);
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = reward == null ? 0.03f : 0.06f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.angularDamping = 0.3f;

            if (reward == null) go.AddComponent<PusherCoin>();
            else go.AddComponent<PrizeItem>().reward = reward;
            return go;
        }

        static void B(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider)
            => RoomBuilder.Box(name, parent, pos, size, mat, collider, true);

        static Material ClawGlass()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Claw_Glass.mat");
            return m != null ? m : RoomBuilder.Mat("Pusher_Glass", new Color(0.6f, 0.85f, 1f), 0.9f);
        }

        static MachineData EnsureMachineData()
        {
            ProjectSetup.EnsureFolder(DataDir);
            string path = DataDir + "/Machine_CoinPusher.asset";
            var d = AssetDatabase.LoadAssetAtPath<MachineData>(path);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<MachineData>();
                AssetDatabase.CreateAsset(d, path);
            }
            d.id = "coin_pusher";
            d.displayName = "Coin Pusher";
            d.category = MachineCategory.CoinPusher;
            d.costPerPlay = 1;
            d.purchasePrice = 800;
            d.unlockLevel = 1;
            d.footprintSize = new Vector2(1.5f, 2.8f);
            d.footprintCenterZ = -0.45f;
            EditorUtility.SetDirty(d);
            return d;
        }

        static RewardData EnsureReward(string id, string name, Rarity rarity, int tickets, long xp)
        {
            string path = DataDir + "/Rewards/Reward_" + id + ".asset";
            var r = AssetDatabase.LoadAssetAtPath<RewardData>(path);
            if (r == null)
            {
                r = ScriptableObject.CreateInstance<RewardData>();
                AssetDatabase.CreateAsset(r, path);
            }
            r.id = id;
            r.displayName = name;
            r.rarity = rarity;
            r.ticketValue = tickets;
            r.experienceValue = xp;
            EditorUtility.SetDirty(r);
            return r;
        }
    }
}
#endif
