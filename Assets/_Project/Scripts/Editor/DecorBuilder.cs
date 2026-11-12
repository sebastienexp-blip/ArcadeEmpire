#if UNITY_EDITOR
using ArcadeEmpire.Data;
using UnityEditor;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>
    /// Décorations achetables et plaçables (modèles provisoires en primitives) : plante, pouf, colonne néon, trophée.
    /// Chaque décoration a un collider (le joueur ne la traverse pas) et sa fiche MachineData (catégorie Decoration).
    /// </summary>
    public static class DecorBuilder
    {
        const string DataDir = "Assets/_Project/Data";
        const string PrefabDir = "Assets/_Project/Prefabs";

        public static void EnsureAll()
        {
            ProjectSetup.EnsureFolder(DataDir);
            ProjectSetup.EnsureFolder(PrefabDir);

            var pot = RoomBuilder.Mat("Decor_Pot", new Color(0.35f, 0.2f, 0.12f), 0.3f);
            var leaf = RoomBuilder.Mat("Decor_Leaf", new Color(0.15f, 0.65f, 0.25f), 0.3f);
            var pouf = RoomBuilder.Mat("Decor_Pouf", new Color(0.9f, 0.3f, 0.6f), 0.3f);
            var dark = RoomBuilder.Mat("Decor_Dark", new Color(0.08f, 0.08f, 0.12f), 0.5f);
            var cyan = RoomBuilder.Neon("Neon_Cyan", new Color(0.1f, 0.9f, 1f));
            var gold = RoomBuilder.Mat("Decor_Gold", new Color(1f, 0.8f, 0.2f), 0.85f);
            ModelLib.Textured(pouf, "fabric", null, 1f, 0f, 0.2f);
            ModelLib.Textured(gold, "gold", null, 1f, 0.9f, 0.65f);
            ModelLib.Textured(dark, "paint", null, 1f, 0.1f, 0.5f);
            ModelLib.Textured(leaf, "fabric", null, 1f, 0f, 0.3f);

            // Plante en pot
            Make("decor_plant", "Plante en pot", 60, 1, new Vector2(0.8f, 0.8f), "Decor_Plant", root =>
            {
                if (UseModel("decor_plant", root, leaf)) return;
                Prim(PrimitiveType.Cylinder, "Pot", root, new Vector3(0, 0.25f, 0), new Vector3(0.45f, 0.25f, 0.45f), pot, true);
                Prim(PrimitiveType.Sphere, "Leaves", root, new Vector3(0, 0.8f, 0), new Vector3(0.7f, 0.8f, 0.7f), leaf, false);
            });

            // Pouf
            Make("decor_pouf", "Pouf", 80, 1, new Vector2(1.0f, 1.0f), "Decor_Pouf", root =>
            {
                if (UseModel("decor_pouf", root, pouf)) return;
                Prim(PrimitiveType.Cylinder, "Seat", root, new Vector3(0, 0.22f, 0), new Vector3(0.8f, 0.22f, 0.8f), pouf, true);
            });

            // Colonne néon (émet réellement de la lumière)
            Make("decor_neon_pillar", "Colonne néon", 150, 3, new Vector2(0.6f, 0.6f), "Decor_NeonPillar", root =>
            {
                if (!UseModel("decor_neon_pillar", root, dark, cyan))
                {
                Prim(PrimitiveType.Cube, "Pillar", root, new Vector3(0, 1.0f, 0), new Vector3(0.35f, 2.0f, 0.35f), dark, true);
                Prim(PrimitiveType.Cube, "Strip", root, new Vector3(0, 1.0f, -0.19f), new Vector3(0.08f, 1.7f, 0.03f), cyan, false);
                }
                var l = new GameObject("Light").AddComponent<Light>();
                l.transform.SetParent(root, false);
                l.transform.localPosition = new Vector3(0, 1.2f, -0.5f);
                l.type = LightType.Point;
                l.color = new Color(0.2f, 0.9f, 1f);
                l.range = 4f;
                l.intensity = 3f;
                l.shadows = LightShadows.None;
            });

            // Trophée doré
            Make("decor_trophy", "Trophée doré", 250, 5, new Vector2(0.7f, 0.7f), "Decor_Trophy", root =>
            {
                if (UseModel("decor_trophy", root, gold)) return;
                Prim(PrimitiveType.Cube, "Base", root, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.6f, 0.6f), dark, true);
                Prim(PrimitiveType.Cylinder, "Stem", root, new Vector3(0, 0.75f, 0), new Vector3(0.1f, 0.15f, 0.1f), gold, false);
                Prim(PrimitiveType.Sphere, "Cup", root, new Vector3(0, 1.05f, 0), new Vector3(0.35f, 0.35f, 0.35f), gold, false);
            });
        }

        /// <summary>Utilise le modèle FBX s'il existe (matériau Main = mat) et ajoute un collider ajusté ; sinon false (primitives de repli).</summary>
        static bool UseModel(string modelName, Transform root, Material main, Material accent = null)
        {
            var palette = ModelLib.Palette(main);
            if (accent != null) palette["Accent"] = accent;
            var visual = ModelLib.Spawn(modelName, root, palette);
            if (visual == null) return false;
            var b = ModelLib.LocalBounds(root);
            var bc = root.gameObject.AddComponent<BoxCollider>();
            bc.center = b.center;
            bc.size = new Vector3(b.size.x * 0.9f, b.size.y, b.size.z * 0.9f);
            return true;
        }

        static void Make(string id, string displayName, long price, int level, Vector2 footprint, string prefabName, System.Action<Transform> build)
        {
            var root = new GameObject(prefabName).transform;
            build(root);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{PrefabDir}/{prefabName}.prefab");
            Object.DestroyImmediate(root.gameObject);

            string path = $"{DataDir}/Decor_{id}.asset";
            var d = AssetDatabase.LoadAssetAtPath<MachineData>(path);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<MachineData>();
                AssetDatabase.CreateAsset(d, path);
            }
            d.id = id;
            d.displayName = displayName;
            d.category = MachineCategory.Decoration;
            d.prefab = prefab;
            d.costPerPlay = 0;
            d.purchasePrice = price;
            d.unlockLevel = level;
            d.footprintSize = footprint;
            d.footprintCenterZ = 0f;
            EditorUtility.SetDirty(d);
        }

        static void Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        }
    }
}
#endif
