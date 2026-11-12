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
            AirHockey(); Shooting(); Fishing();
            Race(); Stacker();
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

        // ------------------------------------------------------------ Air hockey
        static void AirHockey()
        {
            var data = Data("air_hockey", "Air hockey", MachineCategory.Skill, 5, 3000, 8);
            data.footprintSize = new Vector2(1.5f, 1.7f); data.footprintCenterZ = 0f;
            var pal = ModelLib.Palette(null);
            var root = NewRoot("AirHockeyMachine");
            ModelLib.Spawn("game_airhockey", root, pal);
            var puck = ModelLib.Spawn("hockey_puck", root, pal).transform;
            var mp = ModelLib.Spawn("hockey_mallet", root, pal).transform;
            var malletAi = ModelLib.Spawn("hockey_mallet", root, new Dictionary<string, Material>(pal) { ["Mallet"] = ModelLib.Lit("Hockey_Mallet_AI", new Color(0.15f, 0.4f, 0.95f), 0.55f, 0.2f, "plastic", 1.5f) }).transform;
            puck.localPosition = new Vector3(0, 0.801f, 0);
            mp.localPosition = new Vector3(0, 0.801f, -0.47f);
            malletAi.localPosition = new Vector3(0, 0.801f, 0.47f);
            Col(root, "Base", new Vector3(0, 0.4f, 0), new Vector3(1.34f, 0.8f, 1.54f));
            Col(root, "Rail", new Vector3(0, 0.9f, 0), new Vector3(1.34f, 0.12f, 1.54f));
            Col(root, "Sign", new Vector3(0, 1.7f, 0.7f), new Vector3(1.4f, 0.9f, 0.2f));
            var machine = root.gameObject.AddComponent<AirHockeyMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.9f, -1.7f), new Vector3(0, 0.8f, 0.1f)), null);
            machine.Setup(puck, mp, malletAi, 0.801f);
            Save(root, data, "Game_AirHockey");
        }

        // ------------------------------------------------------------ Stand de tir
        static void Shooting()
        {
            var data = Data("shooting_gallery", "Stand de tir", MachineCategory.Skill, 4, 1800, 6);
            data.footprintSize = new Vector2(1.6f, 2.5f); data.footprintCenterZ = 0.1f;
            var pal = ModelLib.Palette(null);
            var root = NewRoot("ShootingMachine");
            ModelLib.Spawn("game_shooting", root, pal);

            var faces = new Material[3];
            faces[0] = ModelLib.Lit("Target_Normal", Color.white, 0.4f, 0f, "target_normal", 1f);
            faces[1] = ModelLib.Lit("Target_Gold", Color.white, 0.6f, 0.3f, "target_gold", 1f);
            faces[2] = ModelLib.Lit("Target_Bomb", Color.white, 0.4f, 0f, "target_bomb", 1f);
            var tp = new Dictionary<string, Material>(pal) { ["TargetFace"] = faces[0] };

            var pivots = new Transform[9]; var rends = new Renderer[9];
            float[] ys = { 0.83f, 1.33f, 1.83f };
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                {
                    int i = r * 3 + c;
                    var t = new GameObject("Target_" + i).transform;
                    t.SetParent(root, false);
                    t.localPosition = new Vector3(-0.4f + 0.4f * c, ys[r], 1.0f);
                    t.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var vis = ModelLib.Spawn("shoot_target", t, tp);
                    pivots[i] = t;
                    rends[i] = vis != null ? vis.GetComponentInChildren<Renderer>() : null;
                }
            var cross = ModelLib.Spawn("shoot_crosshair", root, pal).transform;
            cross.localPosition = new Vector3(0, 1.5f, 0.9f);

            Col(root, "Counter", new Vector3(0, 0.45f, -0.75f), new Vector3(1.5f, 0.9f, 0.7f));
            Col(root, "SideL", new Vector3(-0.7f, 1.3f, 0.25f), new Vector3(0.1f, 2.6f, 2.3f));
            Col(root, "SideR", new Vector3(0.7f, 1.3f, 0.25f), new Vector3(0.1f, 2.6f, 2.3f));
            Col(root, "Back", new Vector3(0, 1.3f, 1.35f), new Vector3(1.5f, 2.6f, 0.1f));
            Col(root, "Header", new Vector3(0, 2.85f, -0.5f), new Vector3(1.5f, 0.34f, 0.5f));
            var machine = root.gameObject.AddComponent<ShootingMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.55f, -1.9f), new Vector3(0, 1.4f, 1.0f)), null);
            machine.Setup(cross, pivots, rends, faces);
            Save(root, data, "Game_Shooting");
        }

        // ------------------------------------------------------------ Pêche
        static void Fishing()
        {
            var data = Data("fishing_pond", "Pêche aux poissons", MachineCategory.Skill, 4, 1400, 4);
            data.footprintSize = new Vector2(1.6f, 1.7f); data.footprintCenterZ = 0f;
            var pal = ModelLib.Palette(null);
            var root = NewRoot("FishingMachine");
            ModelLib.Spawn("game_fishing", root, pal);

            var carriage = ModelLib.Spawn("fishing_carriage", root, pal).transform;
            carriage.localPosition = new Vector3(0, 1.95f, 0);
            var hook = ModelLib.Spawn("fishing_hook", root, pal).transform;
            hook.localPosition = new Vector3(0, 1.45f, 0);
            var cableGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cableGo.name = "Cable";
            Object.DestroyImmediate(cableGo.GetComponent<Collider>());
            cableGo.transform.SetParent(root, false);
            cableGo.GetComponent<Renderer>().sharedMaterial = pal["Dark"];
            cableGo.transform.localPosition = new Vector3(0, 1.7f, 0);
            cableGo.transform.localScale = new Vector3(0.008f, 0.25f, 0.008f);

            Color[] fishColors = { new Color(1f, 0.5f, 0.15f), new Color(0.95f, 0.35f, 0.2f), new Color(1f, 0.6f, 0.2f), new Color(0.9f, 0.45f, 0.25f),
                                   new Color(0.2f, 0.55f, 0.95f), new Color(0.15f, 0.7f, 0.85f), new Color(1f, 0.8f, 0.1f), new Color(0.95f, 0.55f, 0.3f) };
            int n = FishingMachine.FishCount;
            var fishes = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                bool big = FishingMachine.ValueOf(i) >= 5;
                var fp = new Dictionary<string, Material>(pal)
                {
                    ["Main"] = ModelLib.Lit("Fish_" + i, fishColors[i % fishColors.Length], 0.6f, big ? 0.6f : 0.1f, "plastic", 2f)
                };
                var f = ModelLib.Spawn(big ? "fish_big" : "fish_small", root, fp).transform;
                f.name = "Fish_" + i;
                f.localPosition = new Vector3(0, 0.76f, 0);
                fishes[i] = f;
            }

            Col(root, "Base", new Vector3(0, 0.4f, 0), new Vector3(1.5f, 0.8f, 1.5f));
            Col(root, "Rim", new Vector3(0, 0.84f, 0), new Vector3(1.5f, 0.12f, 1.5f));
            Col(root, "Roof", new Vector3(0, 2.05f, 0), new Vector3(1.5f, 0.1f, 1.5f));
            Col(root, "Header", new Vector3(0, 2.34f, -0.5f), new Vector3(1.5f, 0.34f, 0.5f));
            var machine = root.gameObject.AddComponent<FishingMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.8f, -1.9f), new Vector3(0, 0.9f, 0f)), null);
            machine.Setup(carriage, hook, cableGo.transform, fishes);
            Save(root, data, "Game_Fishing");
        }

        // ------------------------------------------------------------ Course d'arcade
        static void Race()
        {
            var data = Data("race_arcade", "Course d'arcade", MachineCategory.Skill, 4, 2200, 10);
            data.footprintSize = new Vector2(1.2f, 1.3f); data.footprintCenterZ = -0.1f;
            var pal = ModelLib.Palette(null);
            var root = NewRoot("RaceMachine");
            var cab = ModelLib.Spawn("game_race", root, pal);
            Renderer screen = null;
            foreach (var r in cab.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) if (m == pal["RaceScreen"]) screen = r;

            Color[] cols = { new Color(0.1f, 0.4f, 0.95f), new Color(0.2f, 0.8f, 0.3f), new Color(0.95f, 0.8f, 0.1f), new Color(0.7f, 0.2f, 0.9f), new Color(1f, 0.5f, 0.1f), new Color(0.9f, 0.9f, 0.95f) };
            var player = ModelLib.Spawn("race_car", root, new Dictionary<string, Material>(pal) { ["Main"] = ModelLib.Lit("Race_Player", new Color(0.9f, 0.1f, 0.15f), 0.7f, 0.4f, "paint", 1f) }).transform;
            var rivals = new Transform[6];
            for (int i = 0; i < rivals.Length; i++)
            {
                var p = new Dictionary<string, Material>(pal) { ["Main"] = ModelLib.Lit("Race_Rival_" + i, cols[i], 0.7f, 0.4f, "paint", 1f) };
                rivals[i] = ModelLib.Spawn("race_car", root, p).transform;
                rivals[i].name = "Rival_" + i;
                rivals[i].gameObject.SetActive(false);
            }
            Col(root, "Lower", new Vector3(0, 0.475f, 0), new Vector3(0.95f, 0.95f, 0.9f));
            Col(root, "Upper", new Vector3(0, 1.65f, 0), new Vector3(0.9f, 1.4f, 0.6f));
            Col(root, "Header", new Vector3(0, 2.55f, 0.05f), new Vector3(1f, 0.3f, 0.5f));
            var machine = root.gameObject.AddComponent<RaceMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.6f, -1.8f), new Vector3(0, 1.6f, 0f)), null);
            machine.Setup(screen, player, rivals);
            Save(root, data, "Game_Race");
        }

        // ------------------------------------------------------------ Stacker rétro
        static void Stacker()
        {
            var data = Data("stacker_retro", "Stacker rétro", MachineCategory.Skill, 5, 2600, 12);
            data.footprintSize = new Vector2(1.2f, 1.3f); data.footprintCenterZ = -0.1f;
            var pal = ModelLib.Palette(null);
            var root = NewRoot("StackerMachine");
            ModelLib.Spawn("game_stacker", root, pal);
            Color[] rowCols = { new Color(0.2f, 0.9f, 0.4f), new Color(0.2f, 0.9f, 0.4f), new Color(0.3f, 0.85f, 0.7f), new Color(0.2f, 0.8f, 1f),
                                new Color(0.2f, 0.5f, 1f), new Color(0.5f, 0.35f, 1f), new Color(0.8f, 0.3f, 0.95f), new Color(1f, 0.2f, 0.7f),
                                new Color(1f, 0.3f, 0.35f), new Color(1f, 0.55f, 0.15f), new Color(1f, 0.85f, 0.15f), new Color(1f, 0.95f, 0.5f) };
            var cells = new Transform[StackerMachine.Rows * StackerMachine.Cols];
            for (int r = 0; r < StackerMachine.Rows; r++)
            {
                var p = new Dictionary<string, Material>(pal) { ["Main"] = RoomBuilder.Neon("Stack_Row_" + r, rowCols[r]) };
                for (int c = 0; c < StackerMachine.Cols; c++)
                {
                    var cell = ModelLib.Spawn("stack_block", root, p).transform;
                    cell.name = $"Cell_{r}_{c}";
                    cell.localPosition = new Vector3((c - 3) * 0.1f, 1.75f - 0.55f + r * 0.1f, -0.30f);
                    cells[r * StackerMachine.Cols + c] = cell;
                    cell.gameObject.SetActive(false);
                }
            }
            var button = Button(root, new Vector3(0, 0.975f, -0.35f), pal, 1.0f);
            Col(root, "Lower", new Vector3(0, 0.45f, -0.05f), new Vector3(0.95f, 0.9f, 0.9f));
            Col(root, "Upper", new Vector3(0, 1.75f, 0.05f), new Vector3(0.9f, 1.7f, 0.6f));
            Col(root, "Header", new Vector3(0, 2.75f, 0f), new Vector3(1f, 0.3f, 0.5f));
            var machine = root.gameObject.AddComponent<StackerMachine>();
            machine.Configure(data, Anchor(root, new Vector3(0, 1.8f, -2.0f), new Vector3(0, 1.75f, 0f)), button);
            machine.Setup(cells);
            Save(root, data, "Game_Stacker");
        }
    }
}
#endif
