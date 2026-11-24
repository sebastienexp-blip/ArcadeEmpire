using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using UnityEngine;

namespace ArcadeEmpire.Building
{
    /// <summary>
    /// Maison du joueur : chaque meuble apparaît à son emplacement dès qu'il est possédé (gagné/acheté à la vitrine),
    /// et les étagères exposent automatiquement les lots gagnés (les plus rares d'abord, un exemplaire de chaque).
    /// </summary>
    public class HomeDisplay : MonoBehaviour
    {
        [System.Serializable]
        public class Spot { public string rewardId; public GameObject content; }

        public List<Spot> spots = new List<Spot>();
        public List<Transform> shelfSlots = new List<Transform>();
        public float slotSize = 0.34f;

        private readonly List<GameObject> shown = new List<GameObject>();
        private InventoryManager inv;

        public int FurnitureOwned { get; private set; }
        public int FurnitureTotal => spots.Count;

        private void Start()
        {
            inv = InventoryManager.Instance;
            if (inv != null) inv.OnChanged += Refresh;
            Refresh();
        }

        private void OnDestroy() { if (inv != null) inv.OnChanged -= Refresh; }

        public void Refresh()
        {
            inv = InventoryManager.Instance;
            FurnitureOwned = 0;
            foreach (var s in spots)
            {
                bool owned = inv != null && inv.Count(s.rewardId) > 0;
                if (s.content != null) s.content.SetActive(owned);
                if (owned) FurnitureOwned++;
            }

            foreach (var g in shown) if (g != null) Destroy(g);
            shown.Clear();
            if (inv == null || inv.Catalog == null) return;
            var owned2 = new List<RewardData>();
            foreach (var r in inv.Catalog.rewards)
                if (r != null && !r.furniture && r.prefab != null && inv.Count(r.id) > 0) owned2.Add(r);
            owned2.Sort((a, b) => b.rarity != a.rarity ? b.rarity.CompareTo(a.rarity) : string.CompareOrdinal(a.displayName, b.displayName));
            for (int i = 0; i < shelfSlots.Count && i < owned2.Count; i++)
            {
                var slot = shelfSlots[i];
                var g = Instantiate(owned2[i].prefab, slot);
                g.transform.localPosition = Vector3.zero;
                g.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // face à la pièce
                // mise à l'échelle : le lot tient dans un cube de slotSize
                var rs = g.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    float m = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    if (m > 0.0001f) g.transform.localScale *= slotSize / m;
                    rs = g.GetComponentsInChildren<Renderer>();
                    b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    g.transform.position += Vector3.up * (slot.position.y - b.min.y);   // posé sur l'étagère
                }
                shown.Add(g);
            }
        }
    }
}
