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

        public static Dictionary<string, Material> Palette(Material main)
        {
            return new Dictionary<string, Material>
            {
                ["Main"] = main,
                ["Dark"] = RoomBuilder.Mat("Model_Dark", new Color(0.08f, 0.08f, 0.11f), 0.45f),
                ["Light"] = RoomBuilder.Mat("Model_Light", new Color(0.95f, 0.95f, 0.95f), 0.3f),
                ["Accent"] = RoomBuilder.Neon("Model_Accent", new Color(1f, 0.8f, 0.2f)),
                ["Pot"] = RoomBuilder.Mat("Model_Pot", new Color(0.55f, 0.32f, 0.2f), 0.3f),
                ["Soil"] = RoomBuilder.Mat("Model_Soil", new Color(0.18f, 0.11f, 0.07f), 0.1f),
            };
        }
    }
}
#endif
