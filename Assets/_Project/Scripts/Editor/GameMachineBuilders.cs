#if UNITY_EDITOR
using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Machines.Games;
using UnityEditor;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>
    /// Phase 9 : construit les cinq machines de jeux (roue, machine à sous, marteau, basket, réflexes) en prefabs
    /// avec leur fiche MachineData. Modèles FBX texturés ; collisions par colliders invisibles.
    /// </summary>
    public static class GameMachineBuilders
    {
        const string DataDir = "Assets/_Project/Data";
        const string PrefabDir = "Assets/_Project/Prefabs";

        public static void EnsureAll()
        {
            ProjectSetup.EnsureFolder(DataDir);
            ProjectSetup.EnsureFolder(PrefabDir);
            Wheel(); Slots(); Hammer(); Basket(); Reflex();
            AssetDatabase.SaveAssets();
        }

        static MachineData Data(string id, string name, MachineCategory cat, long cost, long price, int level)
        {
            string path = $"{DataDir}/Machine_{id}.asset";
            var d = AssetDatabase.LoadAssetAtPath<MachineData>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<MachineData>(); AssetDatabase.CreateAsset(d, path); }
            d.id = id; d.displayName = name; d.category = cat; d.costPerPlay = cost; d.purchasePrice = price; d.unlockLevel = level;
            d.footprintSize = new Vector2(1.6f, 2.3f); d.footprintCenterZ = -0.55f;
            EditorUtility.SetDirty(d);
            return d;
        }

        static Transform NewRoot(string name)
        {
            var root = new GameObject(name).transform;
            return root;
        }

        static void Col(Transform root, string name, Vector3 center, Vector3 size, bool trigger = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = center; bc.size = size; bc.isTrigger = trigger;
        }

        static Transform Anchor(Transform root, Vector3 pos, Vector3 look)
        {
            var a = new GameObject("CameraAnchor").transform;
            a.SetParent(root, false);
            a.localPosition = pos;
            a.localRotation = Quaternion.LookRotation(look - pos);
            return a;
        }

        static Transform Button(Transform root, Vector3 pos, Dictionary<string, Material> pal, float scale)
        {
            var b = new GameObject("DropButton").transform;
            b.SetParent(root, false);
            b.localPosition = pos;
            b.localScale = Vector3.one * scale;
            ModelLib.Spawn("arcade_button", b, pal);
            return b;
        }

        static void Save(Transform root, MachineData data, string prefabName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            data.prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{PrefabDir}/{prefabName}.prefab");
            Object.DestroyImmediate(root.gameObject);
            EditorUtility.SetDirty(data);
        }

        // ------------------------------------------------------------ Roue de la fortune
        static void Wheel()
        {
            var data = Data("wheel_fortune", "Roue de la fortune", MachineCategory.Other, 5, 1200, 4);
            var pal = ModelLib.Palette(null);
            var root = NewRoot("WheelMachine");
            ModelLib.Spawn("game_wheel", root, pal);
            var pivot = new GameObject("Wheel").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(0f, 1.55f, 0.13f);
            ModelLib.Spawn("wheel_disc", pivot, pal);
            Col(root, "Base", new Vector3(0, 0.375f, 0), new Vector3(1.4f, 0.75f, 1.0f));
            Col(root, "Back", new Vector3(0, 1.55f, 0.35f), new Vector3(1.4f, 2.1f, 0.3f));
            Col(root, "Shelf", new Vector3(0, 0.81f, -0.72f), new Vector3(1.3f, 0.12f, 0.5f));
            var button = Button(root, new Vector3(0, 0.875f, -0.72f), pal, 1.0f);
            var machine = root.gameObject.AddComponent<WheelMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.7f, -2.7f), new Vector3(0, 1.4f, 0.2f)), button);
            machine.SetWheel(pivot);
            Save(root, data, "Game_Wheel");
        }

        // ------------------------------------------------------------ Machine à sous
        static void Slots()
        {
            var data = Data("slots_jackpot", "Machine à sous Jackpot", MachineCategory.Other, 10, 2500, 6);
            var pal = ModelLib.Palette(null);
            var root = NewRoot("SlotMachine");
            ModelLib.Spawn("game_slots", root, pal);
            var reels = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                reels[i] = new GameObject("Reel_" + i).transform;
                reels[i].SetParent(root, false);
                reels[i].localPosition = new Vector3(-0.28f + 0.28f * i, 1.66f, 0.0f);
                ModelLib.Spawn("slot_reel", reels[i], pal);
            }
            Col(root, "Base", new Vector3(0, 0.5f, 0), new Vector3(1.3f, 1.0f, 0.9f));
            Col(root, "Upper", new Vector3(0, 1.9f, 0.05f), new Vector3(1.3f, 1.9f, 0.8f));
            Col(root, "Shelf", new Vector3(0, 1.0f, -0.62f), new Vector3(1.3f, 0.1f, 0.45f));
            var button = Button(root, new Vector3(0, 1.07f, -0.62f), pal, 1.0f);
            var machine = root.gameObject.AddComponent<SlotMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.75f, -2.6f), new Vector3(0, 1.7f, 0f)), button);
            machine.SetReels(reels);
            Save(root, data, "Game_Slots");
        }

        // ------------------------------------------------------------ Testeur de force
        static void Hammer()
        {
            var data = Data("hammer_strength", "Testeur de force", MachineCategory.Skill, 4, 1500, 5);
            var pal = ModelLib.Palette(null);
            var root = NewRoot("HammerMachine");
            ModelLib.Spawn("game_hammer", root, pal);
            var puck = new GameObject("Puck").transform;
            puck.SetParent(root, false);
            puck.localPosition = new Vector3(0, 0.62f, 0.46f);
            ModelLib.Spawn("hammer_puck", puck, pal);

            var on = RoomBuilder.Neon("Hammer_LampOn", new Color(0.2f, 1f, 0.4f));
            var off = RoomBuilder.Mat("Hammer_LampOff", new Color(0.06f, 0.1f, 0.08f), 0.3f);
            var lamps = new Renderer[10];
            for (int i = 0; i < 10; i++)
            {
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lamp.name = "Lamp_" + i;
                Object.DestroyImmediate(lamp.GetComponent<BoxCollider>());
                lamp.transform.SetParent(root, false);
                lamp.transform.localPosition = new Vector3(0.28f, 0.72f + i * 0.16f, 0.495f);
                lamp.transform.localScale = new Vector3(0.07f, 0.07f, 0.02f);
                lamps[i] = lamp.GetComponent<MeshRenderer>();
                lamps[i].sharedMaterial = off;
            }
            Col(root, "Base", new Vector3(0, 0.15f, 0), new Vector3(1.3f, 0.3f, 1.4f));
            Col(root, "Tower", new Vector3(0, 1.45f, 0.55f), new Vector3(0.7f, 2.3f, 0.1f));
            Col(root, "Podium", new Vector3(0, 0.575f, -0.55f), new Vector3(0.7f, 0.55f, 0.4f));
            var button = Button(root, new Vector3(0, 0.87f, -0.55f), pal, 0.9f);
            var machine = root.gameObject.AddComponent<HammerMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.5f, -2.8f), new Vector3(0, 1.4f, 0.4f)), button);
            machine.Setup(puck, lamps, on, off, 0.62f, 2.3f);
            Save(root, data, "Game_Hammer");
        }

        // ------------------------------------------------------------ Basket
        static void Basket()
        {
            var data = Data("basketball", "Basket", MachineCategory.Skill, 5, 2000, 7);
            var pal = ModelLib.Palette(null);
            var root = NewRoot("BasketMachine");
            ModelLib.Spawn("game_basket", root, pal);

            var launch = new GameObject("LaunchPoint").transform;
            launch.SetParent(root, false); launch.localPosition = new Vector3(0, 1.0f, -0.45f);
            var hoop = new GameObject("HoopCenter").transform;
            hoop.SetParent(root, false); hoop.localPosition = new Vector3(0, 1.75f, 0.33f);

            // ballon modèle (inactif) cloné à chaque tir
            string pmPath = DataDir + "/BallBounce.physicMaterial";
            var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
            if (pm == null)
            {
                pm = new PhysicsMaterial("BallBounce");
                AssetDatabase.CreateAsset(pm, pmPath);
            }
            pm.bounciness = 0.55f; pm.dynamicFriction = 0.4f; pm.staticFriction = 0.4f;
            pm.bounceCombine = PhysicsMaterialCombine.Maximum;
            EditorUtility.SetDirty(pm);
            var ball = new GameObject("BallTemplate");
            ball.transform.SetParent(root, false);
            ball.transform.localPosition = launch.localPosition;
            var sc = ball.AddComponent<SphereCollider>(); sc.radius = 0.12f; sc.sharedMaterial = pm;
            var rb = ball.AddComponent<Rigidbody>(); rb.mass = 0.6f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; rb.angularDamping = 0.2f;
            ball.AddComponent<BasketBall>();
            ModelLib.Spawn("basketball", ball.transform, pal);
            ball.SetActive(false);

            Col(root, "Base", new Vector3(0, 0.25f, 0), new Vector3(1.4f, 0.5f, 1.6f));
            Col(root, "SideL", new Vector3(-0.67f, 1.4f, 0), new Vector3(0.06f, 1.8f, 1.5f));
            Col(root, "SideR", new Vector3(0.67f, 1.4f, 0), new Vector3(0.06f, 1.8f, 1.5f));
            Col(root, "Roof", new Vector3(0, 2.35f, 0), new Vector3(1.4f, 0.1f, 1.5f));
            Col(root, "BackWall", new Vector3(0, 1.5f, 0.72f), new Vector3(1.4f, 2.0f, 0.1f));
            Col(root, "Backboard", new Vector3(0, 1.95f, 0.6f), new Vector3(1.0f, 0.7f, 0.06f));
            Col(root, "Podium", new Vector3(0, 0.62f, -0.9f), new Vector3(1.3f, 0.12f, 0.5f));
            var button = Button(root, new Vector3(0, 0.7f, -0.9f), pal, 0.9f);

            var machine = root.gameObject.AddComponent<BasketballMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.6f, -2.9f), new Vector3(0, 1.5f, 0.1f)), button);
            machine.Setup(launch, hoop, ball);

            var zone = new GameObject("HoopZone");
            zone.transform.SetParent(root, false);
            zone.transform.localPosition = new Vector3(0, 1.7f, 0.33f);
            var bc = zone.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.3f, 0.12f, 0.3f);
            zone.AddComponent<BasketHoopZone>().Configure(machine);
            Save(root, data, "Game_Basket");
        }

        // ------------------------------------------------------------ Réflexes
        static void Reflex()
        {
            var data = Data("reflex_pads", "Réflexes", MachineCategory.Skill, 3, 1000, 3);
            var pal = ModelLib.Palette(null);
            var root = NewRoot("ReflexMachine");
            ModelLib.Spawn("game_reflex", root, pal);

            var rim = pal["Metal"];
            var on = new Material[6]; var off = new Material[6];
            var renderers = new Renderer[6];
            Quaternion tilt = Quaternion.Euler(-20f, 0f, 0f);
            Vector3 center = new Vector3(0f, 0.98f, -0.06f);
            for (int i = 0; i < 6; i++)
            {
                string tex = "pad_" + (i + 1);
                off[i] = ModelLib.Textured(RoomBuilder.Mat("Pad_Off_" + (i + 1), new Color(0.4f, 0.4f, 0.4f), 0.5f), tex, null, 1f, 0f, 0.5f);
                on[i] = ModelLib.Glowing("Pad_On_" + (i + 1), tex, 1.5f);
                var p = new Dictionary<string, Material>(pal) { ["Pad"] = off[i] };
                var pad = new GameObject("Pad_" + (i + 1)).transform;
                pad.SetParent(root, false);
                int col = i % 3, row = i / 3;
                pad.localPosition = center + tilt * new Vector3(-0.38f + 0.38f * col, 0.05f, -0.16f + 0.32f * row);
                pad.localRotation = tilt;
                ModelLib.Spawn("reflex_pad", pad, p);
                renderers[i] = pad.GetComponentInChildren<Renderer>();
            }
            Col(root, "Base", new Vector3(0, 0.4f, 0), new Vector3(1.3f, 0.8f, 0.9f));
            Col(root, "Back", new Vector3(0, 1.55f, 0.3f), new Vector3(1.3f, 1.5f, 0.3f));
            Col(root, "Panel", new Vector3(0, 0.95f, -0.06f), new Vector3(1.3f, 0.3f, 0.7f));
            var machine = root.gameObject.AddComponent<ReflexMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.8f, -2.4f), new Vector3(0, 1.0f, -0.06f)), null);
            machine.Setup(renderers, on, off, rim);
            Save(root, data, "Game_Reflex");
        }
    }
}
#endif
