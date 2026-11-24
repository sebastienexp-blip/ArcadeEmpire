#if UNITY_EDITOR
using System.IO;
using ArcadeEmpire.Building;
using ArcadeEmpire.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>
    /// Génère la première salle d'arcade (rétro années 80) avec des PRIMITIVES Unity (modèles provisoires :
    /// aucun modèle 3D final n'est importé à ce stade). Dimensions : 22 x 16 m, hauteur 4,2 m.
    /// Scène produite : Assets/_Project/Scenes/ArcadeRoom.unity.
    /// </summary>
    public static class RoomBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/ArcadeRoom.unity";
        const string MatDir = "Assets/_Project/Materials";

        const float W = 22f, D = 16f, H = 4.2f;

        [MenuItem("ArcadeEmpire/Générer la salle d'arcade")]
        public static void Build()
        {
            ProjectSetup.EnsureFolder(MatDir);
            ProjectSetup.EnsureFolder("Assets/_Project/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var floorA = Mat("Floor_A", new Color(0.10f, 0.10f, 0.14f), 0.7f);
            var floorB = Mat("Floor_B", new Color(0.16f, 0.12f, 0.22f), 0.7f);
            var wall = Mat("Wall", new Color(0.12f, 0.08f, 0.20f), 0.2f);
            var ceiling = Mat("Ceiling", new Color(0.05f, 0.05f, 0.08f), 0.1f);
            ModelLib.Textured(floorA, "tile", null, 1f, 0f, 0.7f);
            ModelLib.Textured(floorB, "tile", null, 1f, 0f, 0.7f);
            ModelLib.Textured(wall, "wall", null, 3f, 0f, 0.2f);
            ModelLib.Textured(ceiling, "paint", null, 4f, 0f, 0.1f);
            var cabinet = Mat("Cabinet", new Color(0.08f, 0.08f, 0.10f), 0.5f);
            var neonPink = Neon("Neon_Pink", new Color(1f, 0.1f, 0.6f));
            var neonCyan = Neon("Neon_Cyan", new Color(0.1f, 0.9f, 1f));
            var neonYellow = Neon("Neon_Yellow", new Color(1f, 0.85f, 0.1f));

            var root = new GameObject("Room").transform;

            // Sol en damier (1 m x 1 m)
            var floor = Group("Floor", root);
            for (int x = 0; x < (int)W; x++)
                for (int z = 0; z < (int)D; z++)
                    Tag(Box("Tile", floor, new Vector3(x + 0.5f - W / 2, -0.05f, z + 0.5f - D / 2), new Vector3(1f, 0.1f, 1f), (x + z) % 2 == 0 ? floorA : floorB, false),
                        (x + z) % 2 == 0 ? RoomThemeTarget.Role.FloorA : RoomThemeTarget.Role.FloorB);

            var floorCol = floor.gameObject.AddComponent<BoxCollider>();
            floorCol.center = new Vector3(0f, -0.05f, 0f);
            floorCol.size = new Vector3(W, 0.1f, D);

            // Murs et plafond
            var walls = Group("Walls", root);
            Tag(Box("Wall_Back", walls, new Vector3(0, H / 2, D / 2), new Vector3(W, H, 0.2f), wall, true), RoomThemeTarget.Role.Wall);
            Tag(Box("Wall_Front", walls, new Vector3(0, H / 2, -D / 2), new Vector3(W, H, 0.2f), wall, true), RoomThemeTarget.Role.Wall);
            Tag(Box("Wall_Left", walls, new Vector3(-W / 2, H / 2, 0), new Vector3(0.2f, H, D), wall, true), RoomThemeTarget.Role.Wall);
            Tag(Box("Wall_Right", walls, new Vector3(W / 2, H / 2, 0), new Vector3(0.2f, H, D), wall, true), RoomThemeTarget.Role.Wall);
            Tag(Box("Ceiling", walls, new Vector3(0, H + 0.1f, 0), new Vector3(W + 0.4f, 0.2f, D + 0.4f), ceiling, false), RoomThemeTarget.Role.Ceiling);

            // Néons : bandes le long du plafond et enseigne
            var neons = Group("Neons", root);
            Tag(Box("Neon_Back", neons, new Vector3(0, H - 0.2f, D / 2 - 0.15f), new Vector3(W - 0.4f, 0.08f, 0.08f), neonPink, false), RoomThemeTarget.Role.NeonA);
            Tag(Box("Neon_Front", neons, new Vector3(0, H - 0.2f, -D / 2 + 0.15f), new Vector3(W - 0.4f, 0.08f, 0.08f), neonCyan, false), RoomThemeTarget.Role.NeonB);
            Tag(Box("Neon_Left", neons, new Vector3(-W / 2 + 0.15f, H - 0.2f, 0), new Vector3(0.08f, 0.08f, D - 0.4f), neonCyan, false), RoomThemeTarget.Role.NeonB);
            Tag(Box("Neon_Right", neons, new Vector3(W / 2 - 0.15f, H - 0.2f, 0), new Vector3(0.08f, 0.08f, D - 0.4f), neonPink, false), RoomThemeTarget.Role.NeonA);
            Tag(Box("Sign_Arcade", neons, new Vector3(0, 3.4f, D / 2 - 0.15f), new Vector3(5f, 0.5f, 0.1f), neonYellow, false), RoomThemeTarget.Role.NeonAccent);

            // Éclairage réaliste : dalles lumineuses blanches au plafond (grille) + quelques lumières colorées d'ambiance
            var lights = Group("Lights", root);
            var panel = Neon("Ceiling_Panel", new Color(1f, 0.97f, 0.9f));
            int li = 0;
            for (float lx = -7.5f; lx <= 7.6f; lx += 5f)
                for (float lz = -5f; lz <= 5.1f; lz += 5f)
                {
                    Box("Panel", lights, new Vector3(lx, H - 0.02f, lz), new Vector3(1.2f, 0.03f, 1.2f), panel, false);
                    PointLight("Light_White_" + li++, lights, new Vector3(lx, H - 0.35f, lz), new Color(1f, 0.95f, 0.88f), 7.5f).intensity = 2.6f;   // blanc fixe (non recoloré par les thèmes)
                }
            Tag(PointLight("Light_Pink", lights, new Vector3(-9f, 2.6f, 5.5f), new Color(1f, 0.25f, 0.7f), 6f), RoomThemeTarget.Role.LightA);
            Tag(PointLight("Light_Cyan", lights, new Vector3(9f, 2.6f, 5.5f), new Color(0.25f, 0.9f, 1f), 6f), RoomThemeTarget.Role.LightB);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.19f, 0.25f);

            // Machines et décorations : prefabs placés à l'exécution par BuildingManager (disposition sauvegardée)
            ClawMachineBuilder.EnsurePrefab();
            CoinPusherBuilder.EnsurePrefab();
            DecorBuilder.EnsureAll();
            GameMachineBuilders.EnsureAll();
            AssetDatabase.SaveAssets();
            ModelLib.LogBounds("claw_shell");
            ModelLib.LogBounds("orientation_probe");   // attendu si X inversé à l'import : x négatif ; z et y inchangés   // diagnostic d'orientation après import FBX (attendu : centre z négatif)
            var machineCatalog = ProjectSetup.EnsureMachineCatalog();

            var building = new GameObject("Building");
            building.AddComponent<BuildingManager>().Configure(machineCatalog);
            building.AddComponent<RoomThemeManager>();
            building.AddComponent<BuildMode>().Configure(GhostMaterial());

            // Point d'apparition du joueur
            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(root, false);
            spawn.transform.position = new Vector3(0, 0, -5f);

            // Joueur provisoire (capsule) + caméra 3e personne + post-traitement (bloom des néons)
            var player = BuildPlayer(spawn.transform.position);
            player.gameObject.AddComponent<MachineInteractor>();
            var hud = new GameObject("HUD");
            hud.AddComponent<ArcadeEmpire.UI.GameHud>();
            hud.AddComponent<ArcadeEmpire.UI.InventoryPanel>();
            hud.AddComponent<ArcadeEmpire.UI.OptionsMenu>();
            hud.AddComponent<ArcadeEmpire.UI.ShopPanel>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = spawn.transform.position + new Vector3(0, 3f, -4f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            var tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.SetTarget(player.transform);
            player.SetCamera(camGo.transform);
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var volGo = new GameObject("Global Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = EnsureProfile();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Matériau transparent (asset, donc inclus dans le build) pour l'aperçu de placement.</summary>
        static Material GhostMaterial()
        {
            string path = MatDir + "/BuildGhost.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", new Color(0.2f, 1f, 0.4f, 0.45f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", 5f);
            m.SetFloat("_DstBlend", 10f);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Personnage provisoire : capsule + tête + "nez" indiquant la direction (aucun modèle FBX à ce stade).</summary>
        static PlayerController BuildPlayer(Vector3 pos)
        {
            var go = new GameObject("Player") { tag = "Player" };
            go.transform.position = pos + new Vector3(0f, 0.05f, 0f);

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;

            // Personnage détaillé : torse, tête, bras et jambes (FBX texturés) ; les membres pivotent à l'épaule / à la hanche
            var pal = ModelLib.Palette(null);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            bool ok = ModelLib.Spawn("player_torso", visual, pal) != null;
            Transform Part(string model, string name, Vector3 pos, float scaleX = 1f)
            {
                var pivot = new GameObject(name).transform;
                pivot.SetParent(visual, false);
                pivot.localPosition = pos;
                pivot.localScale = new Vector3(scaleX, 1f, 1f);
                ModelLib.Spawn(model, pivot, pal);
                return pivot;
            }
            if (ok)
            {
                Part("player_head", "Head", new Vector3(0f, 1.64f, 0f));
                var armL = Part("player_arm", "ArmL", new Vector3(-0.29f, 1.45f, 0f), -1f);
                var armR = Part("player_arm", "ArmR", new Vector3(0.29f, 1.45f, 0f));
                var legL = Part("player_leg", "LegL", new Vector3(-0.11f, 0.92f, 0f), -1f);
                var legR = Part("player_leg", "LegR", new Vector3(0.11f, 0.92f, 0f));
                go.AddComponent<PlayerVisualAnimator>().Configure(armL, armR, legL, legR, visual);
            }
            else
            {
                // repli : capsule provisoire si les modèles sont absents
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
                body.transform.SetParent(go.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                body.GetComponent<MeshRenderer>().sharedMaterial = Mat("Player_Body", new Color(0.95f, 0.55f, 0.15f), 0.3f);
            }

            return go.AddComponent<PlayerController>();
        }

        static VolumeProfile EnsureProfile()
        {
            string path = MatDir + "/ArcadeVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.4f);
            bloom.scatter.Override(0.7f);
            AssetDatabase.AddObjectToAsset(bloom, profile);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            AssetDatabase.AddObjectToAsset(tone, profile);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void Tag(GameObject go, RoomThemeTarget.Role role)
        {
            GameObjectUtility.SetStaticEditorFlags(go, 0);   // un objet recoloré à l'exécution ne doit pas être en batching statique
            go.AddComponent<RoomThemeTarget>().role = role;
        }

        static void Tag(Light l, RoomThemeTarget.Role role) => l.gameObject.AddComponent<RoomThemeTarget>().role = role;

        public static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (local) go.transform.localPosition = pos; else go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static Light PointLight(string name, Transform parent, Vector3 pos, Color color, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = 6f;
            l.shadows = LightShadows.None;
            return l;
        }

        public static Material Mat(string name, Color color, float smoothness)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.enableInstancing = true;   // regroupe les objets identiques (pièces, cubes, poissons...)
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Neon(string name, Color color)
        {
            var m = Mat(name, color, 0.5f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * 3f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
#endif
