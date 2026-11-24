using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Labo « visite » (argument -roomlab) : photographie la salle, chaque machine et les autres lieux (points de vue nommés
    /// « LabView_* » dans la scène), puis quitte. Sert à vérifier le décor sans jouer.
    /// </summary>
    public class RoomLab : MonoBehaviour
    {
        private string outDir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-roomlab") < 0) return;
            var go = new GameObject("RoomLab");
            DontDestroyOnLoad(go);
            var lab = go.AddComponent<RoomLab>();
            int i = System.Array.IndexOf(args, "-labout");
            lab.outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "lab");
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(outDir);
            Application.runInBackground = true;
            yield return new WaitForSeconds(4f);
            var cam = new GameObject("LabCamera").AddComponent<Camera>();
            cam.enabled = false; cam.fieldOfView = 55f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var log = new System.Text.StringBuilder();

            // vues générales de la salle
            var views = new List<(string, Vector3, Vector3)>
            {
                ("salle_entree", new Vector3(0f, 2.6f, -7.4f), new Vector3(0f, 0.8f, 2f)),
                ("salle_fond", new Vector3(0f, 2.4f, 0f), new Vector3(0f, 1f, 7f)),
                ("salle_gauche", new Vector3(-4f, 2.2f, 0f), new Vector3(-10f, 1f, 0f)),
                ("salle_droite", new Vector3(4f, 2.2f, 0f), new Vector3(10f, 1f, 0f)),
                ("salle_haut", new Vector3(0f, 9f, -9f), new Vector3(0f, 0f, 0.5f)),
            };
            // points de vue placés dans la scène (vitrine, boutique, maison…)
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name.StartsWith("LabView_")) views.Add((t.name.Substring(8), t.position, t.position + t.forward * 3f));
            // chaque machine, vue de face
            int k = 0;
            foreach (var m in FindObjectsByType<MachineBase>(FindObjectsSortMode.None))
            {
                Transform a = m.transform;
                views.Add(("machine_" + (k++) + "_" + m.name.Replace("(Clone)", ""), a.TransformPoint(0f, 1.7f, -2.3f), a.TransformPoint(0f, 1.05f, 0.1f)));
            }
            foreach (var (name, pos, look) in views)
            {
                cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
                cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
                Destroy(tex);
                log.AppendLine("capture " + name);
                yield return null;
            }
            File.WriteAllText(Path.Combine(outDir, "journal.txt"), log.ToString());
            Application.Quit();
        }
    }
}
