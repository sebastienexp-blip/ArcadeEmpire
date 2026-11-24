using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ArcadeEmpire.Economy;
using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Labo automatique de la machine à pince (argument -clawlab) : un robot joue des parties (vise un lot, descend, remonte),
    /// mesure les prises et les gains, et prend des captures. Arguments : -labout dossier, -labrounds nombre de parties.
    /// </summary>
    public class ClawLab : MonoBehaviour
    {
        private string outDir;
        private int rounds = 20;
        private ClawMachine machine;
        private Camera cam;
        private RenderTexture rt;
        private readonly StringBuilder log = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-clawlab") < 0) return;
            var go = new GameObject("ClawLab");
            DontDestroyOnLoad(go);
            var lab = go.AddComponent<ClawLab>();
            int i = System.Array.IndexOf(args, "-labout");
            lab.outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "lab");
            int j = System.Array.IndexOf(args, "-labrounds");
            if (j >= 0 && j + 1 < args.Length && int.TryParse(args[j + 1], out int r)) lab.rounds = r;
        }

        private void Note(string s) { Debug.Log("[LAB] " + s); log.AppendLine(s); }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(outDir);
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.1f;
            float wait = 0f;
            while (machine == null && wait < 60f)
            {
                machine = FindAnyObjectByType<ClawMachine>();
                yield return new WaitForSecondsRealtime(0.5f); wait += 0.5f;
            }
            if (machine == null) { Note("ERREUR : aucune machine à pince"); Finish(); yield break; }
            yield return new WaitForSeconds(1.5f);
            foreach (var c in Camera.allCameras) c.enabled = false;
            cam = new GameObject("LabCamera").AddComponent<Camera>();
            cam.enabled = false; cam.fieldOfView = 42f; cam.nearClipPlane = 0.02f; cam.farClipPlane = 60f;
            rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Add(CurrencyType.Coins, 100000);
            Note("Entrée : " + machine.Enter());
            int prizes0 = FreePrizes().Count;
            Note("Lots dans la machine : " + prizes0);
            CaptureAll("00_debut");

            int okRounds = 0;
            for (int r = 0; r < rounds; r++)
            {
                float t0 = Time.time;
                while (!machine.LabIdle && Time.time - t0 < 20f) yield return null;
                int grabs = machine.LabGrabs, wins = machine.LabWins;
                machine.RequestDrop();   // insère la pièce
                t0 = Time.time;
                while (!machine.IsMoving && Time.time - t0 < 5f) yield return null;
                if (!machine.IsMoving) { Note($"partie {r} : la partie ne démarre pas"); continue; }

                PrizeItem target = PickTarget();
                if (target == null) { Note("Plus de lots"); break; }
                Vector3 tl = machine.transform.InverseTransformPoint(target.transform.position);
                Vector2 goal = new Vector2(Mathf.Clamp(tl.x, machine.PlayMin.x, machine.PlayMax.x), Mathf.Clamp(tl.z, machine.PlayMin.y, machine.PlayMax.y));
                t0 = Time.time;
                while (machine.IsMoving && Time.time - t0 < 15f)
                {
                    Vector3 h = machine.HeadLocalPos;
                    Vector2 d = goal - new Vector2(h.x, h.z);
                    if (d.magnitude < 0.012f) break;
                    machine.SetExternalMove(Vector2.ClampMagnitude(d / 0.12f, 1f));
                    yield return null;
                }
                machine.SetExternalMove(Vector2.zero);
                yield return new WaitForSeconds(0.6f);
                if (r == 0) Capture("01_visee", 0);
                machine.RequestDrop();
                // séquence : captures sur les premières parties
                float ts = Time.time;
                bool cap1 = false, cap2 = false, cap3 = false;
                while (!machine.LabIdle && Time.time - ts < 25f)
                {
                    float e = Time.time - ts;
                    if (r < 3)
                    {
                        if (!cap1 && e > 1.6f) { cap1 = true; Capture($"p{r}_a_descente", 3); }
                        if (!cap2 && e > 3.4f) { cap2 = true; Capture($"p{r}_b_remontee", 3); }
                        if (!cap3 && e > 5.6f) { cap3 = true; Capture($"p{r}_c_trappe", 0); }
                    }
                    yield return null;
                }
                bool grabbed = machine.LabGrabs > grabs, won = machine.LabWins > wins;
                if (won) okRounds++;
                Note($"partie {r} : cible {target.name} ({target.reward?.displayName}) prise={(grabbed ? "oui" : "non")} gagnée={(won ? "OUI" : "non")}  « {machine.StatusText} »");
            }
            CaptureAll("02_fin");
            Note($"Bilan : {rounds} parties, {machine.LabGrabs} prises, {machine.LabWins} gains ({100f * machine.LabWins / Mathf.Max(1, rounds):0}%), lots restants {FreePrizes().Count}/{prizes0}");
            Finish();
        }

        private List<PrizeItem> FreePrizes()
        {
            var l = new List<PrizeItem>();
            foreach (var p in FindObjectsByType<PrizeItem>(FindObjectsSortMode.None))
                if (p.Machine == machine && p.IsFree) l.Add(p);
            return l;
        }

        /// <summary>Comme un joueur : vise un lot bien accessible (le plus haut, pas collé aux vitres ni à la trappe).</summary>
        private PrizeItem PickTarget()
        {
            PrizeItem best = null; float bestScore = float.MinValue;
            foreach (var p in FreePrizes())
            {
                Vector3 l = machine.transform.InverseTransformPoint(p.transform.position);
                if (l.x < machine.PlayMin.x - 0.02f || l.x > machine.PlayMax.x + 0.02f || l.z < machine.PlayMin.y - 0.02f || l.z > machine.PlayMax.y + 0.02f) continue;
                float score = l.y * 3f - Vector2.Distance(new Vector2(l.x, l.z), machine.ChuteXZ) * 0.5f + Random.Range(0f, 0.15f);
                if (score > bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        // vues : 0 face (joueur), 1 trois-quarts, 2 dessus, 3 gros plan pince
        private void Capture(string name, int view)
        {
            Transform m = machine.transform;
            Vector3 pos, look;
            Vector3 h = machine.HeadLocalPos;
            switch (view)
            {
                case 1: pos = new Vector3(-1.3f, 1.9f, -1.5f); look = new Vector3(0f, 1.0f, 0f); break;
                case 2: pos = new Vector3(0f, 1.6f, -0.35f); look = new Vector3(0f, 0.6f, 0.05f); break;
                case 3: pos = new Vector3(h.x + 0.35f, h.y - 0.05f, h.z - 0.75f); look = new Vector3(h.x, h.y - 0.25f, h.z); break;
                default: pos = new Vector3(0f, 1.55f, -1.75f); look = new Vector3(0f, 1.0f, 0f); break;
            }
            Vector3 wp = m.TransformPoint(pos);
            cam.transform.SetPositionAndRotation(wp, Quaternion.LookRotation(m.TransformPoint(look) - wp, Vector3.up));
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        private void CaptureAll(string prefix)
        {
            Capture(prefix + "_face", 0); Capture(prefix + "_34", 1); Capture(prefix + "_dessus", 2);
        }

        private void Finish()
        {
            File.WriteAllText(Path.Combine(outDir, "journal.txt"), log.ToString());
            Application.Quit();
        }
    }
}
