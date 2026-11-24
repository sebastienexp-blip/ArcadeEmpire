using System.Collections.Generic;
using ArcadeEmpire.Data;
using UnityEngine;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Vignettes 3D des lots pour l'interface : le prefab du lot est photographié une fois (caméra cachée, loin sous la scène),
    /// puis l'image est gardée en mémoire. Les menus affichent ainsi la vraie peluche / figurine / console.
    /// </summary>
    public static class PrizeThumbs
    {
        private const int Size = 160;
        private const int Layer = 31;
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        private static Camera cam;
        private static Light key, fill;
        private static Transform stage;
        private static RenderTexture rt;

        public static Texture2D Get(RewardData r)
        {
            if (r == null || r.prefab == null) return null;
            if (cache.TryGetValue(r.id, out var t) && t != null) return t;
            t = Render(r.prefab);
            cache[r.id] = t;
            return t;
        }

        private static void Ensure()
        {
            if (cam != null) return;
            var root = new GameObject("PrizeThumbStudio");
            Object.DontDestroyOnLoad(root);
            root.transform.position = new Vector3(0f, -500f, 0f);
            stage = root.transform;
            var cgo = new GameObject("Cam");
            cgo.transform.SetParent(stage, false);
            cam = cgo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = 1 << Layer;
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 20f;
            var lgo = new GameObject("Key"); lgo.transform.SetParent(stage, false);
            key = lgo.AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.5f; key.color = new Color(1f, 0.97f, 0.92f);
            key.cullingMask = 1 << Layer; key.shadows = LightShadows.None; key.enabled = false;
            lgo.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
            var fgo = new GameObject("Fill"); fgo.transform.SetParent(stage, false);
            fill = fgo.AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = 0.7f; fill.color = new Color(0.7f, 0.8f, 1f);
            fill.cullingMask = 1 << Layer; fill.shadows = LightShadows.None; fill.enabled = false;
            fgo.transform.rotation = Quaternion.Euler(10f, 230f, 0f);
            rt = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
        }

        private static Texture2D Render(GameObject prefab)
        {
            Ensure();
            var go = Object.Instantiate(prefab, stage);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, 200f, 0f);   // de trois quarts, face vers la caméra
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
            var lods = go.GetComponentsInChildren<LODGroup>();
            foreach (var l in lods) l.ForceLOD(0);
            foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = Layer;

            var rs = go.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(stage.position, Vector3.one * 0.3f);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float radius = Mathf.Max(0.02f, b.extents.magnitude);
            float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f;
            Vector3 dir = Quaternion.Euler(14f, 0f, 0f) * Vector3.back;
            cam.transform.position = b.center + dir * dist;
            cam.transform.LookAt(b.center);

            key.enabled = fill.enabled = true;
            var prevAmb = RenderSettings.ambientLight;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
            cam.Render();
            RenderSettings.ambientLight = prevAmb;
            key.enabled = fill.enabled = false;

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply(false, true);
            RenderTexture.active = prev;
            go.SetActive(false);
            Object.Destroy(go);
            return tex;
        }
    }
}
