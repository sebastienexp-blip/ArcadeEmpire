using ArcadeEmpire.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>Éléments d'interface communs aux menus refaits : fenêtre plein écran à onglets, grilles de cartes de lots.</summary>
    public static class UiCards
    {
        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return new Color(0.55f, 0.62f, 0.75f);
                case Rarity.Rare: return new Color(0.25f, 0.6f, 1f);
                case Rarity.Epic: return new Color(0.72f, 0.35f, 1f);
                default: return new Color(1f, 0.72f, 0.18f);
            }
        }

        public static string RarityName(Rarity r) => ArcadeEmpire.Machines.ClawMachine.RarityName(r);

        /// <summary>Fenêtre de menu (presque plein écran) : bandeau titre avec icône, monnaies à droite, bouton fermer.</summary>
        public static RectTransform Screen(Transform canvas, string icon, string title, Color accent, UnityAction onClose, out Text currencies)
        {
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(canvas, false);
            var dr = dim.GetComponent<RectTransform>(); dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one; dr.offsetMin = dr.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0.01f, 0.0f, 0.04f, 0.7f);

            var win = new GameObject("Window", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            win.transform.SetParent(canvas, false);
            UiKit.Round(win, new Color(0.05f, 0.04f, 0.13f, 0.98f));
            var ol = win.AddComponent<Outline>(); ol.effectColor = new Color(accent.r, accent.g, accent.b, 0.7f); ol.effectDistance = new Vector2(2f, -2f);
            var r = win.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.04f, 0.05f); r.anchorMax = new Vector2(0.96f, 0.95f); r.offsetMin = r.offsetMax = Vector2.zero;
            win.AddComponent<UiPopIn>();
            var v = win.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(22, 22, 14, 16); v.spacing = 10f;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            // bandeau
            var head = UiKit.AddRow(r, 58f);
            UiKit.Round(head.gameObject, new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 1f));
            var hl = head.GetComponent<HorizontalLayoutGroup>(); hl.padding = new RectOffset(14, 8, 6, 6); hl.spacing = 12f;
            UiKit.AddIcon(head, icon, 44f);
            var t = UiKit.AddText(head, title, 30, Color.white, -1f, 46f, TextAnchor.MiddleLeft, true);
            t.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.6f);
            currencies = UiKit.AddText(head, "", 20, UiKit.Gold, 460f, 46f, TextAnchor.MiddleRight, true);
            currencies.supportRichText = true; currencies.resizeTextForBestFit = true; currencies.resizeTextMinSize = 12; currencies.resizeTextMaxSize = 20;
            var close = UiKit.AddButton(head, "", 48f, 46f, onClose);
            UiKit.SetButtonIcon(close, "close");
            var cc = close.colors; cc.normalColor = new Color(0.75f, 0.2f, 0.3f); cc.highlightedColor = new Color(0.9f, 0.3f, 0.4f); cc.selectedColor = cc.normalColor; close.colors = cc;
            return r;
        }

        public static string CurrencyLine()
        {
            var cm = ArcadeEmpire.Economy.CurrencyManager.Instance;
            if (cm == null) return "";
            return $"<color=#FFD24A>{cm.Get(ArcadeEmpire.Economy.CurrencyType.Coins):N0} pièces</color>   <color=#FF9E66>{cm.Get(ArcadeEmpire.Economy.CurrencyType.Tickets):N0} tickets</color>   <color=#FFE27A>{cm.Get(ArcadeEmpire.Economy.CurrencyType.Keys):N0} clés</color>";
        }

        /// <summary>Barre d'onglets (boutons larges, l'onglet actif est coloré).</summary>
        public static Button[] Tabs(Transform parent, string[] names, string[] icons, Color accent, UnityAction<int> onSelect)
        {
            var row = UiKit.AddRow(parent, 50f);
            var bs = new Button[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int k = i;
                bs[i] = icons != null ? UiKit.AddIconButton(row, icons[i], names[i], 190f, 48f, () => onSelect(k)) : UiKit.AddButton(row, names[i], 190f, 48f, () => onSelect(k));
            }
            return bs;
        }

        public static void HighlightTab(Button[] tabs, int index, Color accent)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                var c = tabs[i].colors;
                c.normalColor = i == index ? accent * 0.85f + new Color(0, 0, 0, 0.15f) : UiKit.ButtonBg;
                c.selectedColor = c.normalColor;
                tabs[i].colors = c;
            }
        }

        /// <summary>Grille de cartes dans une zone défilante.</summary>
        public static RectTransform Grid(RectTransform scrollContent, float cellW = 186f, float cellH = 250f)
        {
            var go = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(scrollContent, false);
            var g = go.GetComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(cellW, cellH); g.spacing = new Vector2(12f, 12f);
            g.padding = new RectOffset(6, 6, 6, 6);
            g.startCorner = GridLayoutGroup.Corner.UpperLeft; g.startAxis = GridLayoutGroup.Axis.Horizontal;
            g.childAlignment = TextAnchor.UpperLeft;
            return go.GetComponent<RectTransform>();
        }

        /// <summary>Carte de lot : vignette 3D sur fond de la couleur de rareté, nom, ligne d'info, ligne de prix, bouton.</summary>
        public static RectTransform PrizeCard(Transform grid, RewardData r, bool known, string info, string price, string button, bool canClick, UnityAction onClick, string badge = null)
        {
            Color rc = RarityColor(r.rarity);
            var card = new GameObject("Card_" + r.id, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(grid, false);
            UiKit.Round(card, new Color(0.1f, 0.09f, 0.22f, 1f));
            var ol = card.AddComponent<Outline>(); ol.effectColor = new Color(rc.r, rc.g, rc.b, 0.8f); ol.effectDistance = new Vector2(2f, -2f);
            var v = card.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(8, 8, 8, 8); v.spacing = 3f; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            // vignette
            var pic = new GameObject("Pic", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            pic.transform.SetParent(card.transform, false);
            UiKit.Round(pic, new Color(rc.r * 0.45f, rc.g * 0.45f, rc.b * 0.45f, 1f));
            var le = pic.GetComponent<LayoutElement>(); le.minHeight = le.preferredHeight = 118f;
            var raw = new GameObject("Thumb", typeof(RectTransform), typeof(RawImage));
            raw.transform.SetParent(pic.transform, false);
            var rr = raw.GetComponent<RectTransform>(); rr.anchorMin = new Vector2(0.5f, 0.5f); rr.anchorMax = new Vector2(0.5f, 0.5f); rr.sizeDelta = new Vector2(112f, 112f);
            var ri = raw.GetComponent<RawImage>(); ri.raycastTarget = false;
            Texture2D tex = PrizeThumbs.Get(r);
            if (tex != null) { ri.texture = tex; if (!known) ri.color = new Color(0f, 0f, 0f, 0.85f); }   // silhouette des lots inconnus
            else
            {
                Object.Destroy(raw);
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                ic.transform.SetParent(pic.transform, false);
                var ii = ic.GetComponent<Image>(); ii.sprite = UiTheme.Icon(r.furniture ? "home" : "star"); ii.preserveAspect = true; ii.raycastTarget = false;
                var ir = ic.GetComponent<RectTransform>(); ir.anchorMin = new Vector2(0.3f, 0.2f); ir.anchorMax = new Vector2(0.7f, 0.8f); ir.offsetMin = ir.offsetMax = Vector2.zero;
                if (!known) ii.color = new Color(0, 0, 0, 0.8f);
            }
            // pastille de rareté
            var tag = new GameObject("Rarity", typeof(RectTransform), typeof(Image));
            tag.transform.SetParent(pic.transform, false);
            UiKit.Round(tag, rc);
            var tr = tag.GetComponent<RectTransform>(); tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0f, 1f); tr.anchoredPosition = new Vector2(4f, -4f); tr.sizeDelta = new Vector2(84f, 20f);
            var tt = UiKit.AddText(tag.transform, RarityName(r.rarity).ToUpper(), 12, new Color(0.05f, 0.04f, 0.1f), -1f, 20f, TextAnchor.MiddleCenter, true);
            var ttr = tt.rectTransform; ttr.anchorMin = Vector2.zero; ttr.anchorMax = Vector2.one; ttr.offsetMin = ttr.offsetMax = Vector2.zero;
            if (!string.IsNullOrEmpty(badge))
            {
                var bd = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                bd.transform.SetParent(pic.transform, false);
                UiKit.Round(bd, new Color(0.05f, 0.04f, 0.12f, 0.9f));
                var br = bd.GetComponent<RectTransform>(); br.anchorMin = br.anchorMax = br.pivot = new Vector2(1f, 1f); br.anchoredPosition = new Vector2(-4f, -4f); br.sizeDelta = new Vector2(62f, 20f);
                var bt = UiKit.AddText(bd.transform, badge, 12, UiKit.Gold, -1f, 20f, TextAnchor.MiddleCenter, true);
                var btr = bt.rectTransform; btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one; btr.offsetMin = btr.offsetMax = Vector2.zero;
            }

            var name = UiKit.AddText(card.transform, known ? r.displayName : "? ? ?", 16, Color.white, -1f, 22f, TextAnchor.MiddleCenter, true);
            name.horizontalOverflow = HorizontalWrapMode.Wrap; name.resizeTextForBestFit = true; name.resizeTextMinSize = 11; name.resizeTextMaxSize = 16;
            if (info != null) { var it = UiKit.AddText(card.transform, info, 13, UiKit.Dim, -1f, 18f, TextAnchor.MiddleCenter); it.supportRichText = true; }
            if (price != null) { var pt = UiKit.AddText(card.transform, price, 16, UiKit.Gold, -1f, 22f, TextAnchor.MiddleCenter, true); pt.supportRichText = true; }
            if (button != null)
            {
                var b = UiKit.AddButton(card.transform, button, 160f, 32f, onClick, canClick);
                if (canClick) { var cb = b.colors; cb.normalColor = new Color(0.2f, 0.62f, 0.4f); cb.highlightedColor = new Color(0.3f, 0.78f, 0.5f); cb.selectedColor = cb.normalColor; b.colors = cb; }
            }
            return card.GetComponent<RectTransform>();
        }

        /// <summary>Ajoute à la grille sa hauteur préférée pour le défilement (la grille calcule elle-même ses lignes).</summary>
        public static void FitColumns(RectTransform grid, float availableWidth)
        {
            var g = grid.GetComponent<GridLayoutGroup>();
            int cols = Mathf.Max(1, Mathf.FloorToInt((availableWidth - 12f + g.spacing.x) / (g.cellSize.x + g.spacing.x)));
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = cols;
        }
    }
}
