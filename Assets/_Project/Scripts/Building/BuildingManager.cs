using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Player;
using ArcadeEmpire.Save;
using UnityEngine;

namespace ArcadeEmpire.Building
{
    /// <summary>
    /// Disposition de la salle : recrée au démarrage les machines et décorations de la sauvegarde, et gère
    /// achat/placement, déplacement, rotation et revente. Toute la logique de validité (limites de la salle,
    /// chevauchements d'emprises, joueur) est ici ; BuildMode ne gère que l'interface et les entrées.
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        public enum PlaceResult { Ok, Invalid, NotEnoughCoins, LevelTooLow, NoPrefab }

        public const float Grid = 0.5f;
        public const int LayoutVersion = 2;

        /// <summary>Machines installées d'office : 4 pinces au fond, 3 coin pushers à gauche (face vers le centre).</summary>
        public static readonly (string id, Vector3 pos, float rotY)[] DefaultLayout =
        {
            ("claw_classic", new Vector3(-4.5f, 0f, 6.6f), 0f),
            ("claw_kpop", new Vector3(-1.5f, 0f, 6.6f), 0f),
            ("claw_sweets", new Vector3(1.5f, 0f, 6.6f), 0f),
            ("claw_toys", new Vector3(4.5f, 0f, 6.6f), 0f),
            ("coin_pusher", new Vector3(-9.8f, 0f, 3.6f), 270f),
            ("coin_pusher_ocean", new Vector3(-9.8f, 0f, 0f), 270f),
            ("coin_pusher_royal", new Vector3(-9.8f, 0f, -3.6f), 270f),
        };

        [SerializeField] private MachineCatalog catalog;
        [Tooltip("Demi-dimensions utilisables de la salle (x, z), murs exclus.")]
        [SerializeField] private Vector2 roomHalfSize = new Vector2(10.8f, 7.8f);

        private readonly List<PlacedObject> placed = new List<PlacedObject>();
        private SaveData fallback;
        private PlayerController player;

        public MachineCatalog Catalog => catalog;
        public IReadOnlyList<PlacedObject> Placed => placed;

        public void Configure(MachineCatalog machineCatalog) => catalog = machineCatalog;

        private SaveData Data
        {
            get
            {
                if (SaveManager.Instance != null) return SaveManager.Instance.Data;
                return fallback ?? (fallback = new SaveData());
            }
        }

        private void Awake() => Instance = this;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            SaveData data = Data;

            // Disposition de départ : la grande salle est livrée complète (toutes les machines installées, rien à acheter).
            // Une ancienne sauvegarde (petite salle) est migrée vers cette disposition.
            if (!data.layoutInitialized || data.layoutVersion < LayoutVersion)
            {
                data.layoutInitialized = true;
                data.layoutVersion = LayoutVersion;
                data.placedMachines.Clear();
                foreach (var (id, pos, rot) in DefaultLayout) AddEntry(data, id, pos, rot);
            }

            foreach (PlacedMachineData entry in data.placedMachines.ToArray())
                Spawn(entry);
        }

        private static void AddEntry(SaveData data, string id, Vector3 pos, float rotY)
        {
            data.placedMachines.Add(new PlacedMachineData { machineId = id, position = pos, rotationY = rotY });
            if (!data.ownedMachineIds.Contains(id)) data.ownedMachineIds.Add(id);
        }

        private PlacedObject Spawn(PlacedMachineData entry)
        {
            MachineData md = catalog != null ? catalog.Find(entry.machineId) : null;
            if (md == null || md.prefab == null)
            {
                Debug.LogWarning($"[Building] Objet inconnu ou sans prefab : {entry.machineId}");
                return null;
            }
            GameObject go = Instantiate(md.prefab, entry.position, Quaternion.Euler(0f, entry.rotationY, 0f));
            var po = go.AddComponent<PlacedObject>();
            po.Init(md, entry);
            placed.Add(po);
            return po;
        }

        // --- Géométrie ---------------------------------------------------------------------------

        public static Vector3 Snap(Vector3 p) =>
            new Vector3(Mathf.Round(p.x / Grid) * Grid, 0f, Mathf.Round(p.z / Grid) * Grid);

        /// <summary>Rectangle au sol (centre, taille) occupé par un objet à une position et une rotation données.</summary>
        public static void GetRect(MachineData md, Vector3 pos, float rotY, out Vector2 center, out Vector2 size)
        {
            float a = rotY * Mathf.Deg2Rad;
            center = new Vector2(pos.x + Mathf.Sin(a) * md.footprintCenterZ, pos.z + Mathf.Cos(a) * md.footprintCenterZ);
            bool quarterTurn = Mathf.Abs(Mathf.RoundToInt(rotY / 90f)) % 2 == 1;
            size = quarterTurn ? new Vector2(md.footprintSize.y, md.footprintSize.x) : md.footprintSize;
        }

        private static bool Overlap(Vector2 ca, Vector2 sa, Vector2 cb, Vector2 sb)
        {
            const float eps = 0.02f;
            return Mathf.Abs(ca.x - cb.x) < (sa.x + sb.x) * 0.5f - eps && Mathf.Abs(ca.y - cb.y) < (sa.y + sb.y) * 0.5f - eps;
        }

        /// <summary>Position valide : dans la salle, sans chevaucher un autre objet ni le joueur.</summary>
        public bool IsValid(MachineData md, Vector3 pos, float rotY, PlacedObject ignore)
        {
            GetRect(md, pos, rotY, out Vector2 c, out Vector2 s);
            if (Mathf.Abs(c.x) + s.x * 0.5f > roomHalfSize.x || Mathf.Abs(c.y) + s.y * 0.5f > roomHalfSize.y) return false;

            foreach (PlacedObject other in placed)
            {
                if (other == ignore || other == null) continue;
                GetRect(other.Data, other.Entry.position, other.Entry.rotationY, out Vector2 oc, out Vector2 os);
                if (Overlap(c, s, oc, os)) return false;
            }

            if (player != null)
            {
                Vector2 p = new Vector2(player.transform.position.x, player.transform.position.z);
                if (Mathf.Abs(p.x - c.x) < s.x * 0.5f + 0.5f && Mathf.Abs(p.y - c.y) < s.y * 0.5f + 0.5f) return false;
            }
            return true;
        }

        // --- Actions -------------------------------------------------------------------------------

        public bool MeetsLevel(MachineData md) =>
            ProgressionManager.Instance == null || ProgressionManager.Instance.Level >= md.unlockLevel;

        public bool CanAfford(MachineData md) =>
            CurrencyManager.Instance != null && CurrencyManager.Instance.CanAfford(CurrencyType.Coins, md.purchasePrice);

        public PlaceResult Place(MachineData md, Vector3 pos, float rotY)
        {
            if (md.prefab == null) return PlaceResult.NoPrefab;
            if (!MeetsLevel(md)) return PlaceResult.LevelTooLow;
            if (!IsValid(md, pos, rotY, null)) return PlaceResult.Invalid;
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.TrySpend(CurrencyType.Coins, md.purchasePrice))
                return PlaceResult.NotEnoughCoins;

            var entry = new PlacedMachineData { machineId = md.id, position = pos, rotationY = rotY };
            Data.placedMachines.Add(entry);
            if (!Data.ownedMachineIds.Contains(md.id)) Data.ownedMachineIds.Add(md.id);
            Spawn(entry);
            return PlaceResult.Ok;
        }

        public bool Move(PlacedObject po, Vector3 pos, float rotY)
        {
            if (po == null || !IsValid(po.Data, pos, rotY, po)) return false;
            po.Entry.position = pos;
            po.Entry.rotationY = rotY;
            po.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            return true;
        }

        public long Sell(PlacedObject po)
        {
            if (po == null) return 0;
            long refund = po.Data.SellPrice;
            if (CurrencyManager.Instance != null && refund > 0) CurrencyManager.Instance.Add(CurrencyType.Coins, refund);
            Data.placedMachines.Remove(po.Entry);
            placed.Remove(po);
            Destroy(po.gameObject);
            return refund;
        }
    }
}
