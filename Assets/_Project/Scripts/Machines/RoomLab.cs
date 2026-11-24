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

            // inventaire de démonstration (machine du labo, jetable) : quelques lots et meubles, des tickets et des clés
            var inv = ArcadeEmpire.Economy.InventoryManager.Instance;
            if (inv != null && inv.Catalog != null)
            {
                int lots = 0, furn = 0;
                foreach (var r in inv.Catalog.rewards)
                {
                    if (r == null) continue;
                    if (r.furniture && furn < 9) { inv.Add(r, 1); furn++; }
                    else if (!r.furniture && lots < 26) { inv.Add(r, 1 + lots % 3); lots++; }
                }
                var cm = ArcadeEmpire.Economy.CurrencyManager.Instance;
                if (cm != null) { cm.Add(ArcadeEmpire.Economy.CurrencyType.Tickets, 900); cm.Add(ArcadeEmpire.Economy.CurrencyType.Keys, 3); }
                log.AppendLine($"inventaire démo : {lots} lots, {furn} meubles ; catalogue {inv.Catalog.rewards.Count}");
            }

            yield return new WaitForSeconds(0.5f);

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
            // le personnage, de face et de trois quarts
            var pl = FindFirstObjectByType<ArcadeEmpire.Player.PlayerController>();
            if (pl != null)
            {
                Transform p = pl.transform;
                foreach (var (name, off) in new[] { ("joueur_face", new Vector3(0f, 1.25f, 2.3f)), ("joueur_34", new Vector3(1.6f, 1.4f, 1.7f)), ("joueur_dos", new Vector3(-0.8f, 1.6f, -2.2f)) })
                {
                    Vector3 pos = p.TransformPoint(off), look = p.TransformPoint(0f, 0.95f, 0f);
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
            }

            // interface : écran de jeu (avec commandes tactiles), puis chaque menu
            if (FindFirstObjectByType<ArcadeEmpire.UI.TouchControls>() == null) new GameObject("TouchControls").AddComponent<ArcadeEmpire.UI.TouchControls>();
            yield return new WaitForSeconds(1f);
            yield return Shot("ui_hud", log);
            if (pl != null)
            {
                pl.TeleportTo(new Vector3(7.7f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));   // devant le comptoir de la vitrine
                yield return new WaitForSeconds(0.6f);
                yield return Shot("ui_vitrine_invite", log);
            }
            foreach (var panel in new[] { "shop", "sell", "inventory" })
            {
                ArcadeEmpire.UI.UIState.Request(panel);
                yield return new WaitForSeconds(1.2f);
                yield return Shot("ui_" + panel, log);
                ArcadeEmpire.UI.UIState.RequestClose();
                yield return new WaitForSeconds(0.4f);
            }
            if (pl != null)
            {
                pl.TeleportTo(new Vector3(60f, 0f, -4.4f), Quaternion.identity);   // maison
                yield return new WaitForSeconds(0.8f);
                yield return Shot("ui_maison", log);
            }
            File.WriteAllText(Path.Combine(outDir, "journal.txt"), log.ToString());
            Application.Quit();
        }

        private IEnumerator Shot(string name, System.Text.StringBuilder log)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            log.AppendLine($"capture {name} ({tex.width}x{tex.height})");
            Destroy(tex);
        }
    }
}