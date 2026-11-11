using System.Collections.Generic;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Save;
using UnityEngine;

namespace ArcadeEmpire.Building
{
    /// <summary>
    /// Applique l'ambiance de la salle (couleurs des sols, murs, néons, lumières) et gère son déblocage.
    /// Une instance de matériau par rôle est créée au démarrage : les assets d'origine ne sont jamais modifiés.
    /// </summary>
    public class RoomThemeManager : MonoBehaviour
    {
        public static RoomThemeManager Instance { get; private set; }

        public enum Result { Ok, Locked, LevelTooLow, NotEnoughCoins }

        private readonly Dictionary<RoomThemeTarget.Role, Material> materials = new Dictionary<RoomThemeTarget.Role, Material>();
        private readonly List<RoomThemeTarget> lightTargets = new List<RoomThemeTarget>();
        private readonly List<Renderer> ceilings = new List<Renderer>();
        private SaveData fallback;

        private SaveData Data
        {
            get
            {
                if (SaveManager.Instance != null) return SaveManager.Instance.Data;
                return fallback ?? (fallback = new SaveData());
            }
        }

        public string CurrentId => Data.roomThemeId;

        private void Awake()
        {
            Instance = this;
            foreach (RoomThemeTarget t in FindObjectsByType<RoomThemeTarget>(FindObjectsSortMode.None))
            {
                var r = t.GetComponent<Renderer>();
                if (r != null)
                {
                    if (!materials.TryGetValue(t.role, out Material inst))
                    {
                        inst = new Material(r.sharedMaterial);
                        materials[t.role] = inst;
                    }
                    r.sharedMaterial = inst;
                    if (t.role == RoomThemeTarget.Role.Ceiling) ceilings.Add(r);
                }
                else if (t.GetComponent<Light>() != null) lightTargets.Add(t);
            }
        }

        private void Start() => Apply(Data.roomThemeId);

        /// <summary>Affiche ou masque le plafond (masqué en mode construction pour voir la salle de dessus).</summary>
        public void SetCeilingVisible(bool visible)
        {
            foreach (Renderer r in ceilings) if (r != null) r.enabled = visible;
        }

        public bool IsUnlocked(string id) => id == "retro80" || Data.unlockedRoomIds.Contains(id);

        public Result Unlock(string id)
        {
            RoomTheme t = RoomThemes.Find(id);
            if (IsUnlocked(id)) return Result.Ok;
            int level = ProgressionManager.Instance != null ? ProgressionManager.Instance.Level : 1;
            if (level < t.minLevel) return Result.LevelTooLow;
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.TrySpend(CurrencyType.Coins, t.price)) return Result.NotEnoughCoins;
            Data.unlockedRoomIds.Add(id);
            return Result.Ok;
        }

        public bool Apply(string id)
        {
            if (!IsUnlocked(id)) return false;
            RoomTheme t = RoomThemes.Find(id);
            Data.roomThemeId = t.id;

            Set(RoomThemeTarget.Role.FloorA, t.floorA, false);
            Set(RoomThemeTarget.Role.FloorB, t.floorB, false);
            Set(RoomThemeTarget.Role.Wall, t.wall, false);
            Set(RoomThemeTarget.Role.Ceiling, t.ceiling, false);
            Set(RoomThemeTarget.Role.NeonA, t.neonA, true);
            Set(RoomThemeTarget.Role.NeonB, t.neonB, true);
            Set(RoomThemeTarget.Role.NeonAccent, t.neonAccent, true);

            foreach (RoomThemeTarget lt in lightTargets)
            {
                var l = lt.GetComponent<Light>();
                if (l != null) l.color = lt.role == RoomThemeTarget.Role.LightA ? t.lightA : t.lightB;
            }
            RenderSettings.ambientLight = t.ambient;
            return true;
        }

        private void Set(RoomThemeTarget.Role role, Color color, bool emissive)
        {
            if (!materials.TryGetValue(role, out Material m)) return;
            m.SetColor("_BaseColor", color);
            if (emissive) m.SetColor("_EmissionColor", color * 3f);
        }
    }
}
