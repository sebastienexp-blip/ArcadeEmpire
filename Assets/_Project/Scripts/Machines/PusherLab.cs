using System.Collections;
using System.IO;
using System.Text;
using ArcadeEmpire.Economy;
using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Labo automatique du coin pusher (lancé seulement avec l'argument -pusherlab, par l'intégration continue) :
    /// le jeu insère des pièces tout seul, mesure la physique (empilement, débit, blocages) et prend des captures d'écran.
    /// Arguments : -labout dossier, -labseconds durée de la phase d'insertion.
    /// </summary>
    public class PusherLab : MonoBehaviour
    {
        private string outDir;
        private float seconds = 240f;
        private CoinPusherMachine machine;
        private Camera cam;
        private RenderTexture rt;
        private readonly StringBuilder csv = new StringBuilder();
        private readonly StringBuilder log = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-pusherlab") < 0) return;
            var go = new GameObject("PusherLab");
            DontDestroyOnLoad(go);
            var lab = go.AddComponent<PusherLab>();
            lab.outDir = Arg(args, "-labout") ?? Path.Combine(Application.persistentDataPath, "lab");
            if (float.TryParse(Arg(args, "-labseconds") ?? "", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s)) lab.seconds = s;
        }

        private static string Arg(string[] a, string key)
        {
            int i = System.Array.IndexOf(a, key);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
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
                machine = FindAnyObjectByType<CoinPusherMachine>();
                yield return new WaitForSecondsRealtime(0.5f); wait += 0.5f;
            }
            if (machine == null) { Note("ERREUR : aucun coin pusher dans la scène"); Finish(); yield break; }
            yield return new WaitForSeconds(1f);

            foreach (var c in Camera.allCameras) c.enabled = false;   // rendu seulement pour les captures : la simulation va plus vite
            var cg = new GameObject("LabCamera");
            cam = cg.AddComponent<Camera>();
            cam.enabled = false; cam.fieldOfView = 42f; cam.nearClipPlane = 0.02f; cam.farClipPlane = 60f;
            rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;

            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Add(CurrencyType.Coins, 100000);
            Note("Entrée dans la machine : " + machine.Enter());
            csv.AppendLine("t,inserees,tombees,sur_plateau,c0,c1,c2,c3,c4,c5,c6plus,couche_max,debout,couloir,eveillees");
            CaptureAll("00_debut");
            Sample(0f);

            float t0 = Time.time, nextIns = 0f, nextSample = 5f;
            bool seqDone = false, wheelDone = false, midDone = false;
            int slot = 0;
            while (Time.time - t0 < seconds)
            {
                float t = Time.time - t0;
                if (t >= nextIns) { machine.InsertAt(slot); slot = (slot + 1) % CoinPusherMachine.SlotX.Length; nextIns = t + 0.6f; }
                if (t >= nextSample) { Sample(t); nextSample += 5f; }
                if (!midDone && t > seconds * 0.4f) { midDone = true; CaptureAll("01_milieu"); }
                if (!seqDone && t > seconds * 0.5f)
                {
                    seqDone = true;
                    for (int i = 0; i < 16; i++) { Capture("seq_" + i.ToString("00"), 0); yield return new WaitForSeconds(0.2f); }
                }
                if (!wheelDone && t > seconds * 0.65f)
                {
                    wheelDone = true;
                    machine.LabSpinWheel();
                    yield return new WaitForSeconds(1.2f); Capture("roue_1", 0);
                    yield return new WaitForSeconds(1.5f); Capture("roue_2", 0);
                    yield return new WaitForSeconds(2.6f); Capture("roue_3_resultat", 0);
                    Note("Roue : " + machine.StatusText);
                    yield return new WaitForSeconds(1.5f); Capture("roue_4_effet", 1);
                }
                yield return null;
            }
            CaptureAll("02_fin_insertion");
            float tEnd = Time.time;
            while (Time.time - tEnd < 30f) { yield return new WaitForSeconds(5f); Sample(Time.time - t0); }
            CaptureAll("03_fin");
            Note($"Bilan : {machine.InsertedCount} insérées, {machine.TotalCollected} tombées dans le bac, {machine.CoinsOnTray} sur le plateau");
            Finish();
        }

        private void Sample(float t)
        {
            var hist = new int[7];
            int maxLayer = 0, standing = 0, chute = 0, awake = 0;
            float plateFront = machine.PlateFrontZ;
            foreach (var c in machine.ActiveCoins)
            {
                if (c == null || !c.gameObject.activeInHierarchy || c.Body == null || c.Body.isKinematic) continue;
                if (!c.Body.IsSleeping()) awake++;
                Vector3 lp = machine.transform.InverseTransformPoint(c.transform.position);
                if (lp.y > 1.13f && lp.z > 0.23f && lp.z < 0.31f) { chute++; continue; }
                bool onPlate = lp.z > plateFront && lp.y > 1.0f;
                float surface = onPlate ? 1.025f : 0.9f;
                int layer = Mathf.FloorToInt((lp.y - surface) / 0.0068f);
                if (layer < 0 || layer > 40) continue;
                hist[Mathf.Min(layer, 6)]++;
                maxLayer = Mathf.Max(maxLayer, layer);
                if (Mathf.Abs(Vector3.Dot(c.transform.up, machine.transform.forward)) > 0.6f) standing++;
            }
            csv.AppendLine(string.Join(",", t.ToString("0"), machine.InsertedCount, machine.TotalCollected, machine.CoinsOnTray,
                hist[0], hist[1], hist[2], hist[3], hist[4], hist[5], hist[6], maxLayer, standing, chute, awake));
            Note($"t={t:0}s inserees={machine.InsertedCount} tombees={machine.TotalCollected} plateau={machine.CoinsOnTray} couches=[{string.Join(" ", hist)}] max={maxLayer} debout={standing} couloir={chute} eveillees={awake}");
        }

        // vues : 0 face (joueur), 1 trois-quarts, 2 dessus, 3 profil
        private void Capture(string name, int view)
        {
            Transform m = machine.transform;
            Vector3 pos, look;
            switch (view)
            {
                case 1: pos = new Vector3(-1.15f, 1.75f, -1.25f); look = new Vector3(0f, 1.0f, -0.05f); break;
                case 2: pos = new Vector3(0f, 2.35f, -0.3f); look = new Vector3(0f, 0.95f, -0.12f); break;
                case 3: pos = new Vector3(1.55f, 1.3f, -0.05f); look = new Vector3(0f, 1.05f, 0.0f); break;
                default: pos = new Vector3(0f, 1.62f, -1.45f); look = new Vector3(0f, 1.02f, -0.05f); break;
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
            Capture(prefix + "_face", 0); Capture(prefix + "_34", 1); Capture(prefix + "_dessus", 2); Capture(prefix + "_profil", 3);
        }

        private void Finish()
        {
            File.WriteAllText(Path.Combine(outDir, "mesures.csv"), csv.ToString());
            File.WriteAllText(Path.Combine(outDir, "journal.txt"), log.ToString());
            Application.Quit();
        }
    }
}
