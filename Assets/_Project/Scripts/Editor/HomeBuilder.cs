#if UNITY_EDITOR
using System.Collections.Generic;
using ArcadeEmpire.Building;
using ArcadeEmpire.Data;
using UnityEditor;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>
    /// Maison du joueur (même scène, à l'écart de la salle) : salon, cuisine, chambre, salle de jeux, étagères d'exposition.
    /// Chaque meuble est un LOT (vitrine : tickets, et clés dorées pour les meubles premium sous cadenas) qui apparaît à son
    /// emplacement une fois obtenu. Construit aussi, dans la salle : la vitrine des lots, le comptoir de rachat et le téléporteur.
    /// </summary>
    public static class HomeBuilder
    {
        public static readonly Vector3 HouseOrigin = new Vector3(60f, 0f, 0f);
        const string DataDir = "Assets/_Project/Data";

        sealed class Furn
        {
            public string id, name, model; public int tickets, keys; public Color main, second, light, accent; public (Vector3 pos, float rot)[] at;
        }

        static Furn F(string id, string name, string model, int tickets, int keys, Color main, Color second, Color light, Color accent, params (Vector3, float)[] at)
            => new Furn { id = id, name = name, model = model, tickets = tickets, keys = keys, main = main, second = second, light = light, accent = accent, at = at };

        static readonly Color Oak = new Color(0.62f, 0.44f, 0.28f), Walnut = new Color(0.36f, 0.22f, 0.13f), White = new Color(0.94f, 0.94f, 0.92f),
            Cream = new Color(0.96f, 0.92f, 0.84f), Screen = new Color(0.35f, 0.65f, 1f), Pink = new Color(1f, 0.35f, 0.7f), Cyan = new Color(0.2f, 0.9f, 1f);

        static readonly Furn[] Furniture =
        {
            // Repères : rot 0 = façade vers -Z, 180 = vers +Z, 90 = vers -X, 270 = vers +X. Passages (1,2 m) à garder libres :
            // porte d'entrée x=-4 (façade z=-6) ; salon<->cuisine z=-2,5 (x=0) ; salon<->chambre x=-2 (z=1) ; cuisine<->jeux x=2 (z=1) ; chambre<->jeux z=3,5 (x=0)
            // salon (x -8..0, z -6..1) : coin TV contre le mur gauche
            F("home_tv", "TV écran géant", "home_tv", 250, 1, Walnut, Walnut, White, Screen, (new Vector3(-7.62f, 0f, -2.6f), 270f)),
            F("home_rug", "Grand tapis", "home_rug", 50, 0, new Color(0.55f, 0.2f, 0.3f), new Color(0.85f, 0.65f, 0.4f), Cream, Cyan, (new Vector3(-5.7f, 0f, -2.6f), 90f)),
            F("home_coffee_table", "Table basse", "home_coffee_table", 60, 0, Oak, Oak, White, Cyan, (new Vector3(-5.75f, 0f, -2.6f), 90f)),
            F("home_sofa", "Canapé moelleux", "home_sofa", 120, 0, new Color(0.25f, 0.35f, 0.55f), Cream, new Color(0.35f, 0.45f, 0.68f), Pink, (new Vector3(-4.2f, 0f, -2.6f), 90f)),
            F("home_lamp", "Lampadaire", "home_lamp", 40, 0, Cream, Cream, White, new Color(1f, 0.9f, 0.7f), (new Vector3(-7.45f, 0f, -0.45f), 0f)),
            F("home_bookshelf", "Bibliothèque", "home_bookshelf", 90, 0, new Color(0.8f, 0.3f, 0.3f), Oak, new Color(0.3f, 0.6f, 0.9f), new Color(0.95f, 0.8f, 0.3f), (new Vector3(-5.9f, 0f, 0.73f), 0f)),
            F("home_plant", "Plante verte", "home_plant", 30, 0, new Color(0.2f, 0.6f, 0.25f), Oak, White, Cyan, (new Vector3(-0.45f, 0f, -5.5f), 0f)),
            // cuisine (x 0..8, z -6..1) : plan de travail et frigo contre la façade, table au centre
            F("home_kitchen", "Cuisine équipée", "home_kitchen", 200, 0, White, Oak, new Color(0.2f, 0.2f, 0.22f), Cyan, (new Vector3(4.0f, 0f, -5.6f), 180f)),
            F("home_fridge", "Réfrigérateur", "home_fridge", 150, 0, new Color(0.8f, 0.82f, 0.86f), Oak, White, Cyan, (new Vector3(6.0f, 0f, -5.58f), 180f)),
            F("home_table", "Table à manger", "home_table", 80, 0, Oak, Oak, White, Cyan, (new Vector3(4.5f, 0f, -2.2f), 0f)),
            F("home_chair", "Chaises (x2)", "home_chair", 60, 0, Oak, Oak, new Color(0.9f, 0.4f, 0.35f), Cyan, (new Vector3(4.5f, 0f, -1.45f), 0f), (new Vector3(4.5f, 0f, -2.95f), 180f)),
            // chambre (x -8..0, z 1..6) : lit contre le mur du fond, coin bureau contre le mur gauche
            F("home_bed", "Lit double", "home_bed", 220, 0, new Color(0.45f, 0.55f, 0.85f), Walnut, White, Cyan, (new Vector3(-4.6f, 0f, 4.8f), 0f)),
            F("home_nightstand", "Table de chevet", "home_nightstand", 50, 0, new Color(1f, 0.9f, 0.7f), Walnut, White, Cyan, (new Vector3(-5.75f, 0f, 5.62f), 0f)),
            F("home_wardrobe", "Armoire", "home_wardrobe", 160, 0, White, Oak, White, Cyan, (new Vector3(-1.2f, 0f, 5.6f), 0f)),
            F("home_desk", "Bureau gamer", "home_desk", 180, 1, new Color(0.12f, 0.12f, 0.14f), Walnut, White, Pink, (new Vector3(-7.58f, 0f, 2.6f), 270f)),
            F("home_gaming_chair", "Fauteuil gamer", "home_gaming_chair", 120, 1, new Color(0.15f, 0.15f, 0.18f), Walnut, White, Pink, (new Vector3(-6.75f, 0f, 2.6f), 90f)),
            // salle de jeux (x 0..8, z 1..6) : borne au fond, écran contre le mur droit, pouf devant
            F("home_arcade", "Borne d'arcade", "home_arcade", 400, 3, new Color(0.15f, 0.1f, 0.4f), Pink, White, Cyan, (new Vector3(6.9f, 0f, 5.38f), 0f)),
            F("home_neon", "Néon mural", "home_neon", 60, 0, Pink, Cyan, White, Pink, (new Vector3(5.2f, 1.6f, 5.88f), 0f)),
            F("home_beanbag", "Pouf poire", "decor_pouf", 70, 0, new Color(0.95f, 0.45f, 0.2f), Cream, Cream, Cyan, (new Vector3(5.6f, 0f, 3.0f), 0f)),
            F("home_tv_game", "Écran de jeux", "home_tv", 300, 2, new Color(0.1f, 0.1f, 0.12f), new Color(0.1f, 0.1f, 0.12f), White, new Color(0.9f, 0.4f, 1f), (new Vector3(7.62f, 0f, 3.0f), 90f)),
        };

        // ------------------------------------------------------------------ récompenses « meuble »
        static RewardData EnsureFurnitureReward(Furn f)
        {
            ProjectSetup.EnsureFolder(DataDir + "/Rewards");
            string path = DataDir + "/Rewards/Reward_" + f.id + ".asset";
            var r = AssetDatabase.LoadAssetAtPath<RewardData>(path);
            if (r == null) { r = ScriptableObject.CreateInstance<RewardData>(); AssetDatabase.CreateAsset(r, path); }
            r.id = f.id; r.displayName = f.name; r.furniture = true;
            r.rarity = f.keys >= 2 ? Rarity.Legendary : f.keys == 1 ? Rarity.Epic : f.tickets >= 150 ? Rarity.Rare : Rarity.Common;
            r.shopTicketPrice = f.tickets; r.keyPrice = f.keys; r.ticketValue = 0; r.experienceValue = 20 + f.tickets / 5;
            // prefab d'affichage (vignette de la vitrine / collection)
            ProjectSetup.EnsureFolder("Assets/_Project/Prefabs/Furniture");
            var vis = ModelLib.Spawn(f.model, null, FurnPalette(f));
            if (vis != null)
            {
                vis.name = "Furniture_" + f.id;
                r.prefab = PrefabUtility.SaveAsPrefabAsset(vis, "Assets/_Project/Prefabs/Furniture/" + f.id + ".prefab");
                Object.DestroyImmediate(vis);
            }
            EditorUtility.SetDirty(r);
            return r;
        }

        static Dictionary<string, Material> FurnPalette(Furn f)
        {
            var pal = ModelLib.Palette(null);
            pal["Main"] = RoomBuilder.Mat("Home_" + f.id + "_Main", f.main, 0.35f);
            pal["Second"] = RoomBuilder.Mat("Home_" + f.id + "_Second", f.second, 0.3f);
            pal["Light"] = RoomBuilder.Mat("Home_" + f.id + "_Light", f.light, 0.2f);
            pal["Accent"] = RoomBuilder.Neon("Home_" + f.id + "_Accent", f.accent);
            if (f.model == "home_sofa" || f.model == "home_bed" || f.model == "decor_pouf" || f.model == "home_rug" || f.model == "home_gaming_chair")
            {
                ModelLib.Textured(pal["Main"], "fabric", null, 3f, 0f, 0.2f);
                ModelLib.Textured(pal["Light"], "fabric", null, 3f, 0f, 0.2f);
            }
            return pal;
        }

        // ------------------------------------------------------------------ maison
        public static Transform BuildHouse(Transform parent, out Transform returnPadSpot)
        {
            var root = new GameObject("House").transform;
            root.SetParent(parent, false);
            root.position = HouseOrigin;
            const float W = 16f, D = 12f, H = 3f;

            var wood = RoomBuilder.Mat("Home_FloorWood", new Color(0.6f, 0.42f, 0.27f), 0.45f);
            ModelLib.Textured(wood, "tile", null, 4f, 0f, 0.45f);
            var tiles = RoomBuilder.Mat("Home_FloorTiles", new Color(0.88f, 0.88f, 0.9f), 0.6f);
            ModelLib.Textured(tiles, "tile", null, 2f, 0f, 0.6f);
            var carpet = RoomBuilder.Mat("Home_Carpet", new Color(0.55f, 0.62f, 0.8f), 0.1f);
            ModelLib.Textured(carpet, "felt", null, 3f, 0f, 0.1f);
            var gameFloor = RoomBuilder.Mat("Home_GameFloor", new Color(0.18f, 0.14f, 0.3f), 0.3f);
            ModelLib.Textured(gameFloor, "felt", null, 3f, 0f, 0.3f);
            var wall = RoomBuilder.Mat("Home_Wall", new Color(0.93f, 0.9f, 0.85f), 0.2f);
            ModelLib.Textured(wall, "wall", null, 3f, 0f, 0.2f);
            var wallGame = RoomBuilder.Mat("Home_WallGame", new Color(0.25f, 0.2f, 0.45f), 0.2f);
            var ceil = RoomBuilder.Mat("Home_Ceiling", new Color(0.97f, 0.97f, 0.96f), 0.1f);
            var trim = RoomBuilder.Mat("Home_Trim", new Color(0.98f, 0.98f, 0.98f), 0.4f);
            var window = RoomBuilder.Neon("Home_Window", new Color(0.35f, 0.5f, 0.7f));
            var lamp = RoomBuilder.Neon("Home_CeilingLamp", new Color(1f, 0.95f, 0.85f));

            // sols par pièce
            Floor(root, "Floor_Living", new Vector3(-4f, 0f, -2.5f), new Vector2(8f, 7f), wood);
            Floor(root, "Floor_Kitchen", new Vector3(4f, 0f, -2.5f), new Vector2(8f, 7f), tiles);
            Floor(root, "Floor_Bedroom", new Vector3(-4f, 0f, 3.5f), new Vector2(8f, 5f), carpet);
            Floor(root, "Floor_Game", new Vector3(4f, 0f, 3.5f), new Vector2(8f, 5f), gameFloor);
            RoomBuilder.Box("Ceiling", root, new Vector3(0f, H + 0.05f, 0f), new Vector3(W, 0.1f, D), ceil, false, true);

            // murs extérieurs : porte d'entrée dans le salon (x = -4), fenêtres lumineuses
            const float DW = 1.2f, DH = 2.2f;   // passages : largeur et hauteur
            var doorMat = RoomBuilder.Mat("Home_Door", Walnut, 0.4f);
            RoomBuilder.Box("Wall_Back", root, new Vector3(0f, H / 2, D / 2), new Vector3(W, H, 0.2f), wall, true, true);
            RoomBuilder.Box("Wall_Left", root, new Vector3(-W / 2, H / 2, 0f), new Vector3(0.2f, H, D), wall, true, true);
            RoomBuilder.Box("Wall_Right", root, new Vector3(W / 2, H / 2, 0f), new Vector3(0.2f, H, D), wall, true, true);
            WallWithDoor(root, "Wall_Front", true, -D / 2, -W / 2, W / 2, -4f, 1.4f, DH, H, 0.2f, wall, trim);
            RoomBuilder.Box("FrontDoor", root, new Vector3(-4f, 1.1f, -D / 2 - 0.02f), new Vector3(1.3f, 2.2f, 0.08f), doorMat, true, true);   // porte fermée (on arrive par le téléporteur)
            RoomBuilder.Box("FrontDoorKnob", root, new Vector3(-3.55f, 1.0f, -D / 2 + 0.05f), new Vector3(0.06f, 0.06f, 0.06f), trim, false, true);
            RoomBuilder.Box("Doormat", root, new Vector3(-4f, 0.01f, -5.45f), new Vector3(1.2f, 0.02f, 0.7f), RoomBuilder.Mat("Home_Doormat", new Color(0.35f, 0.25f, 0.18f), 0.1f), false, true);
            foreach (var (pos, size) in new[] {
                (new Vector3(-6.4f, 1.6f, -D / 2 + 0.11f), new Vector3(1.4f, 1.1f, 0.02f)), (new Vector3(1.6f, 1.6f, -D / 2 + 0.11f), new Vector3(1.2f, 1.0f, 0.02f)),
                (new Vector3(7.2f, 1.6f, -D / 2 + 0.11f), new Vector3(0.9f, 1.0f, 0.02f)), (new Vector3(-W / 2 + 0.11f, 1.6f, 4.6f), new Vector3(0.02f, 1.1f, 1.4f)),
                (new Vector3(-W / 2 + 0.11f, 1.9f, -4.8f), new Vector3(0.02f, 0.9f, 1.2f)), (new Vector3(-2.7f, 1.6f, D / 2 - 0.11f), new Vector3(1.4f, 1.1f, 0.02f)) })
            {
                RoomBuilder.Box("Window", root, pos, size, window, false, true);
                bool alongX = size.x > size.z;
                RoomBuilder.Box("WindowSill", root, pos + new Vector3(alongX ? 0f : Mathf.Sign(-pos.x) * 0.04f, -size.y / 2 - 0.03f, alongX ? Mathf.Sign(-pos.z) * 0.04f : 0f),
                    alongX ? new Vector3(size.x + 0.12f, 0.05f, 0.1f) : new Vector3(0.1f, 0.05f, size.z + 0.12f), trim, false, true);
            }

            // cloisons intérieures : un passage encadré (chambranle + linteau) par mur, loin des meubles
            WallWithDoor(root, "Part_X_South", false, 0f, -D / 2, 1f, -2.5f, DW, DH, H, 0.14f, wall, trim);     // salon <-> cuisine
            WallWithDoor(root, "Part_X_North", false, 0f, 1f, D / 2, 3.5f, DW, DH, H, 0.14f, wall, trim);      // chambre <-> salle de jeux
            WallWithDoor(root, "Part_Z_West", true, 1f, -W / 2, 0f, -2f, DW, DH, H, 0.14f, wall, trim);        // salon <-> chambre
            WallWithDoor(root, "Part_Z_East", true, 1f, 0f, W / 2, 2f, DW, DH, H, 0.14f, wall, trim);          // cuisine <-> salle de jeux
            // peinture violette côté salle de jeux uniquement
            RoomBuilder.Box("GameWallBack", root, new Vector3(4f, H / 2, D / 2 - 0.11f), new Vector3(7.96f, H, 0.02f), wallGame, false, true);
            RoomBuilder.Box("GameWallRight", root, new Vector3(W / 2 - 0.11f, H / 2, 3.5f), new Vector3(0.02f, H, 4.9f), wallGame, false, true);
            foreach (var (x0, x1) in new[] { (0.08f, 2f - DW / 2), (2f + DW / 2, W / 2 - 0.12f) })
                RoomBuilder.Box("GameWallSouth", root, new Vector3((x0 + x1) / 2, H / 2, 1.08f), new Vector3(x1 - x0, H, 0.02f), wallGame, false, true);
            foreach (var (z0, z1) in new[] { (1.08f, 3.5f - DW / 2), (3.5f + DW / 2, D / 2 - 0.12f) })
                RoomBuilder.Box("GameWallWest", root, new Vector3(0.08f, H / 2, (z0 + z1) / 2), new Vector3(0.02f, H, z1 - z0), wallGame, false, true);
            RoomBuilder.Box("GameLintelS", root, new Vector3(2f, (DH + H) / 2, 1.08f), new Vector3(DW, H - DH, 0.02f), wallGame, false, true);
            RoomBuilder.Box("GameLintelW", root, new Vector3(0.08f, (DH + H) / 2, 3.5f), new Vector3(0.02f, H - DH, DW), wallGame, false, true);

            // éclairage : plafonniers + lumière chaude par pièce
            foreach (var c in new[] { new Vector3(-4f, 0f, -2.5f), new Vector3(4f, 0f, -2.5f), new Vector3(-4f, 0f, 3.5f), new Vector3(4f, 0f, 3.5f) })
            {
                RoomBuilder.Box("CeilingLamp", root, c + new Vector3(0f, H - 0.03f, 0f), new Vector3(0.6f, 0.04f, 0.6f), lamp, false, true);
                var lg = new GameObject("RoomLight").AddComponent<Light>();
                lg.transform.SetParent(root, false); lg.transform.localPosition = c + new Vector3(0f, H - 0.3f, 0f);
                lg.type = LightType.Point; lg.range = 7.5f; lg.intensity = 2.4f; lg.color = new Color(1f, 0.93f, 0.82f); lg.shadows = LightShadows.None;
            }

            // meubles (lots) : chaque emplacement est désactivé tant que le lot n'est pas possédé
            var display = root.gameObject.AddComponent<HomeDisplay>();
            foreach (var f in Furniture)
            {
                EnsureFurnitureReward(f);
                var holder = new GameObject("Spot_" + f.id);
                holder.transform.SetParent(root, false);
                var pal = FurnPalette(f);
                foreach (var (pos, rot) in f.at)
                {
                    var m = ModelLib.Spawn(f.model, holder.transform, pal);
                    if (m == null) continue;
                    m.transform.localPosition = pos; m.transform.localRotation = Quaternion.Euler(0f, rot, 0f);
                    if (f.model == "decor_pouf") m.transform.localScale = Vector3.one * 0.9f;
                    if (f.model == "home_neon") m.transform.localScale = new Vector3(-1f, 1f, 1f);   // texte du néon lisible (l'import FBX inverse X)
                    var b = ModelLib.LocalBounds(m.transform);
                    if (f.model != "home_rug" && f.model != "home_neon")
                    {
                        var bc = m.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size;
                    }
                }
                display.spots.Add(new HomeDisplay.Spot { rewardId = f.id, content = holder });
            }

            // étagères d'exposition des lots gagnés (toujours présentes)
            var shelfPal = ModelLib.Palette(null);
            shelfPal["Second"] = RoomBuilder.Mat("Home_ShelfWood", White, 0.4f);
            shelfPal["Accent"] = RoomBuilder.Neon("Home_ShelfLed", new Color(1f, 0.85f, 0.6f));
            // contre un mur plein, jamais devant un passage : salon (façade), cuisine (mur droit), chambre (cloison), salle de jeux (fond)
            foreach (var (pos, rot) in new[] { (new Vector3(-1.65f, 0f, -5.78f), 180f), (new Vector3(7.78f, 0f, -2.2f), 90f), (new Vector3(-4.6f, 0f, 1.29f), 180f), (new Vector3(3.2f, 0f, 5.78f), 0f) })
            {
                var sh = ModelLib.Spawn("home_shelf", root, shelfPal);
                if (sh == null) continue;
                sh.transform.localPosition = pos; sh.transform.localRotation = Quaternion.Euler(0f, rot, 0f);
                for (int lv = 0; lv < 3; lv++)
                    for (int k = 0; k < 4; k++)
                    {
                        var slot = new GameObject("ShelfSlot").transform;
                        slot.SetParent(sh.transform, false);
                        slot.localPosition = new Vector3(-0.57f + k * 0.38f, 0.62f + lv * 0.55f, -0.02f);
                        display.shelfSlots.Add(slot);
                    }
            }

            // arrivée (devant la porte, côté intérieur) + téléporteur retour
            var spawn = new GameObject("HouseSpawn").transform;
            spawn.SetParent(root, false);
            spawn.localPosition = new Vector3(-4f, 0f, -4.5f);
            spawn.localRotation = Quaternion.identity;
            var pad = Pad(root, new Vector3(-6.7f, 0f, -4.9f), "sign_home", new Color(0.5f, 0.75f, 1f));
            returnPadSpot = pad;

            // points de vue du labo
            View(root, "maison_salon", new Vector3(-1.0f, 1.8f, -5.2f), new Vector3(-6f, 0.6f, -1.5f));
            View(root, "maison_salon2", new Vector3(-7.2f, 1.8f, -5.3f), new Vector3(-2f, 0.8f, 0.5f));
            View(root, "maison_cuisine", new Vector3(1.0f, 1.8f, 0.3f), new Vector3(5f, 0.8f, -4.5f));
            View(root, "maison_chambre", new Vector3(-0.8f, 1.8f, 1.6f), new Vector3(-5f, 0.6f, 4.5f));
            View(root, "maison_jeux", new Vector3(0.8f, 1.8f, 1.6f), new Vector3(5.5f, 0.9f, 4.5f));
            View(root, "maison_haut", new Vector3(0f, 14f, -9f), new Vector3(0f, 0f, 0f));
            return spawn;
        }

        static void Floor(Transform root, string name, Vector3 c, Vector2 size, Material m)
        {
            var f = RoomBuilder.Box(name, root, c + new Vector3(0f, -0.05f, 0f), new Vector3(size.x, 0.1f, size.y), m, true, true);
        }

        /// <summary>Mur droit avec un passage encadré : deux pans, un linteau au-dessus, chambranle blanc autour de l'ouverture.
        /// alongX : le mur suit l'axe X à z = fixed (sinon l'axe Z à x = fixed), de a à b, ouverture centrée sur door.</summary>
        static void WallWithDoor(Transform root, string name, bool alongX, float fixedC, float a, float b, float door, float dw, float dh, float h, float thick, Material m, Material trim)
        {
            float d0 = door - dw / 2, d1 = door + dw / 2;
            Vector3 P(float along, float y) => alongX ? new Vector3(along, y, fixedC) : new Vector3(fixedC, y, along);
            Vector3 S(float len, float hh, float t) => alongX ? new Vector3(len, hh, t) : new Vector3(t, hh, len);
            if (d0 - a > 0.01f) RoomBuilder.Box(name + "_A", root, P((a + d0) / 2, h / 2), S(d0 - a, h, thick), m, true, true);
            if (b - d1 > 0.01f) RoomBuilder.Box(name + "_B", root, P((d1 + b) / 2, h / 2), S(b - d1, h, thick), m, true, true);
            RoomBuilder.Box(name + "_Lintel", root, P(door, (dh + h) / 2), S(dw, h - dh, thick), m, true, true);
            // chambranle (montants + traverse), un peu plus épais que le mur
            foreach (float e in new[] { d0 - 0.04f, d1 + 0.04f })
                RoomBuilder.Box(name + "_Jamb", root, P(e, dh / 2), S(0.08f, dh, thick + 0.04f), trim, false, true);
            RoomBuilder.Box(name + "_Head", root, P(door, dh + 0.04f), S(dw + 0.16f, 0.08f, thick + 0.04f), trim, false, true);
        }

        static void Partition(Transform root, string name, Vector3 c, Vector3 size, Material m) => RoomBuilder.Box(name, root, c, size, m, true, true);

        static void View(Transform root, string name, Vector3 pos, Vector3 look)
        {
            var v = new GameObject("LabView_" + name).transform;
            v.SetParent(root, false);
            v.localPosition = pos;
            v.localRotation = Quaternion.LookRotation(look - pos, Vector3.up);
        }

        /// <summary>Téléporteur : socle lumineux + arche + enseigne. Renvoie le transform du socle.</summary>
        static Transform Pad(Transform root, Vector3 pos, string sign, Color color)
        {
            var padRoot = new GameObject("TeleportPad").transform;
            padRoot.SetParent(root, false);
            padRoot.localPosition = pos;
            var glow = RoomBuilder.Neon("Teleport_Glow_" + sign, color);
            var metal = RoomBuilder.Mat("Teleport_Metal", new Color(0.8f, 0.82f, 0.88f), 0.85f);
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Disc"; Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(padRoot, false); disc.transform.localScale = new Vector3(1.2f, 0.03f, 1.2f); disc.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            disc.GetComponent<MeshRenderer>().sharedMaterial = metal;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Glow"; Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(padRoot, false); ring.transform.localScale = new Vector3(1.0f, 0.035f, 1.0f); ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = glow;
            foreach (float s in new[] { -0.7f, 0.7f }) RoomBuilder.Box("ArchPost", padRoot, new Vector3(s, 1.15f, 0f), new Vector3(0.1f, 2.3f, 0.1f), metal, true, true);
            RoomBuilder.Box("ArchTop", padRoot, new Vector3(0f, 2.35f, 0f), new Vector3(1.5f, 0.12f, 0.12f), metal, false, true);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Sign"; Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(padRoot, false); q.transform.localPosition = new Vector3(0f, 2.62f, 0f); q.transform.localScale = new Vector3(1.4f, 0.35f, 1f);
            q.GetComponent<MeshRenderer>().sharedMaterial = ModelLib.Glowing("Sign_" + sign, sign, 1.6f);
            var q2 = Object.Instantiate(q, padRoot); q2.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var lg = new GameObject("PadLight").AddComponent<Light>();
            lg.transform.SetParent(padRoot, false); lg.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            lg.type = LightType.Point; lg.range = 3f; lg.intensity = 2f; lg.color = color; lg.shadows = LightShadows.None;
            return padRoot;
        }

        // ------------------------------------------------------------------ salle : vitrine des lots, comptoir de rachat, téléporteur
        public static void BuildArcadeShops(Transform room, Transform houseSpawn, Transform houseReturnPad)
        {
            var white = RoomBuilder.Mat("Shop_White", new Color(0.95f, 0.95f, 0.97f), 0.85f);
            var dark = RoomBuilder.Mat("Shop_Dark", new Color(0.08f, 0.08f, 0.1f), 0.6f);
            var gold = RoomBuilder.Mat("Shop_Gold", new Color(1f, 0.78f, 0.25f), 0.9f); gold.SetFloat("_Metallic", 1f);
            var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Claw_Glass.mat");
            var led = RoomBuilder.Neon("Shop_Led", new Color(1f, 0.95f, 0.85f));
            var pink = RoomBuilder.Neon("Neon_Pink", new Color(1f, 0.1f, 0.6f));

            // --- grande vitrine contre le mur droit (face vers -X)
            var sc = new GameObject("Showcase").transform;
            sc.SetParent(room, false);
            sc.position = new Vector3(10.35f, 0f, 0f);
            sc.rotation = Quaternion.Euler(0f, 90f, 0f);   // façade (−Z local) vers −X monde
            const float L = 7f;
            RoomBuilder.Box("Base", sc, new Vector3(0f, 0.4f, 0f), new Vector3(L, 0.8f, 0.8f), white, true, true);
            RoomBuilder.Box("BaseLed", sc, new Vector3(0f, 0.78f, -0.405f), new Vector3(L - 0.1f, 0.03f, 0.01f), led, false, true);
            RoomBuilder.Box("Back", sc, new Vector3(0f, 1.65f, 0.38f), new Vector3(L, 1.7f, 0.04f), dark, true, true);
            RoomBuilder.Box("Top", sc, new Vector3(0f, 2.55f, 0f), new Vector3(L + 0.1f, 0.12f, 0.84f), white, true, true);
            RoomBuilder.Box("TopLed", sc, new Vector3(0f, 2.48f, -0.1f), new Vector3(L - 0.2f, 0.02f, 0.06f), led, false, true);
            if (glass != null) RoomBuilder.Box("GlassFront", sc, new Vector3(0f, 1.65f, -0.39f), new Vector3(L, 1.7f, 0.02f), glass, true, true);
            foreach (float s in new[] { -L / 2, L / 2 }) RoomBuilder.Box("Side", sc, new Vector3(s, 1.65f, 0f), new Vector3(0.05f, 1.7f, 0.8f), white, true, true);
            float[] shelfY = { 0.82f, 1.38f, 1.94f };
            for (int i = 1; i < 3; i++) if (glass != null) RoomBuilder.Box("GlassShelf", sc, new Vector3(0f, shelfY[i], 0f), new Vector3(L - 0.1f, 0.02f, 0.74f), glass, false, true);
            var signQ = GameObject.CreatePrimitive(PrimitiveType.Quad);
            signQ.name = "Sign"; Object.DestroyImmediate(signQ.GetComponent<Collider>());
            signQ.transform.SetParent(sc, false); signQ.transform.localPosition = new Vector3(0f, 2.95f, -0.2f); signQ.transform.localScale = new Vector3(3.2f, 0.75f, 1f);
            signQ.GetComponent<MeshRenderer>().sharedMaterial = ModelLib.Glowing("Sign_Showcase", "sign_showcase", 1.8f);
            for (int i = 0; i < 3; i++)
            {
                var lg = new GameObject("ShowcaseLight").AddComponent<Light>();
                lg.transform.SetParent(sc, false); lg.transform.localPosition = new Vector3(-2.3f + i * 2.3f, 2.3f, -0.1f);
                lg.type = LightType.Point; lg.range = 2.6f; lg.intensity = 2.2f; lg.color = new Color(1f, 0.96f, 0.9f); lg.shadows = LightShadows.None;
            }
            // lots exposés : peluches et idoles (bas/milieu), consoles et meubles premium sous cadenas (haut)
            string[] low = { "plush_brown", "plush_unicorn", "idol_ryu", "idol_nari", "monster_mint", "plush_dino", "lollipop_rainbow", "plush_panda", "idol_sora", "plush_fox" };
            for (int i = 0; i < low.Length; i++)
                DisplayPrize(sc, low[i], new Vector3(-3.1f + i * 0.69f, shelfY[0], -0.05f), 0.36f);
            string[] mid = { "idol_gold", "idol_haneul", "album_luna", "album_nova", "lightstick_purple", "choco_bar", "prize_box", "keychain_heart", "squishy_peach", "monster_pink" };
            for (int i = 0; i < mid.Length; i++)
                DisplayPrize(sc, mid[i], new Vector3(-3.1f + i * 0.69f, shelfY[1], -0.05f), 0.32f);
            string[] locked = { "console_home", "console_tower", "console_handheld" };
            for (int i = 0; i < locked.Length; i++)
            {
                Vector3 p = new Vector3(-2.6f + i * 1.0f, shelfY[2], -0.02f);
                DisplayPrize(sc, locked[i], p, 0.4f);
                LockBox(sc, p, glass, gold);
            }
            var kitPal = ModelLib.Palette(null);
            foreach (var (fid, x) in new[] { ("home_arcade", 0.9f), ("home_tv", 1.9f), ("home_desk", 2.9f) })
            {
                var kp = ModelLib.Palette(RoomBuilder.Mat("Kit_" + fid, fid == "home_arcade" ? new Color(0.3f, 0.2f, 0.7f) : fid == "home_tv" ? new Color(0.2f, 0.25f, 0.35f) : new Color(0.15f, 0.15f, 0.18f), 0.4f));
                kp["Accent"] = RoomBuilder.Neon("Kit_Accent", new Color(1f, 0.85f, 0.3f));
                var kit = ModelLib.Spawn("furniture_kit", sc, kp);
                if (kit == null) continue;
                kit.transform.localPosition = new Vector3(x, shelfY[2] + 0.2f, -0.02f);
                kit.transform.localScale = Vector3.one * 0.55f;
                LockBox(sc, new Vector3(x, shelfY[2], -0.02f), glass, gold);
            }

            // comptoir devant la vitrine
            var counter = new GameObject("ShowcaseCounter").transform;
            counter.SetParent(room, false);
            counter.position = new Vector3(8.7f, 0f, 0f);
            counter.rotation = Quaternion.Euler(0f, 90f, 0f);
            RoomBuilder.Box("Desk", counter, new Vector3(0f, 0.47f, 0f), new Vector3(3f, 0.94f, 0.6f), white, true, true);
            RoomBuilder.Box("DeskTop", counter, new Vector3(0f, 0.96f, 0f), new Vector3(3.1f, 0.04f, 0.66f), dark, false, true);
            RoomBuilder.Box("DeskLed", counter, new Vector3(0f, 0.9f, -0.305f), new Vector3(2.9f, 0.025f, 0.01f), pink, false, true);
            var ip = new GameObject("ShowcasePoint").AddComponent<InteractPoint>();
            ip.transform.SetParent(room, false); ip.transform.position = new Vector3(7.7f, 0f, 0f);
            ip.kind = InteractPoint.Kind.Showcase; ip.label = "Vitrine des lots — acheter avec tickets et clés"; ip.range = 2.2f;
            View(room, "vitrine", new Vector3(5.2f, 1.7f, -1.2f), new Vector3(10.3f, 1.4f, 0.3f));

            // --- comptoir de rachat (coin avant droit)
            var kiosk = new GameObject("BuybackKiosk").transform;
            kiosk.SetParent(room, false);
            kiosk.position = new Vector3(8.4f, 0f, -6.6f);
            kiosk.rotation = Quaternion.Euler(0f, 180f, 0f);   // façade vers +Z (vers la salle)
            var green = RoomBuilder.Mat("Shop_Green", new Color(0.12f, 0.55f, 0.4f), 0.7f);
            RoomBuilder.Box("Counter", kiosk, new Vector3(0f, 0.5f, 0f), new Vector3(2.6f, 1.0f, 0.7f), green, true, true);
            RoomBuilder.Box("CounterTop", kiosk, new Vector3(0f, 1.02f, 0f), new Vector3(2.7f, 0.05f, 0.8f), white, false, true);
            RoomBuilder.Box("Register", kiosk, new Vector3(0.7f, 1.15f, 0.05f), new Vector3(0.45f, 0.22f, 0.35f), dark, false, true);
            RoomBuilder.Box("RegisterScreen", kiosk, new Vector3(0.7f, 1.38f, 0.12f), new Vector3(0.35f, 0.22f, 0.03f), RoomBuilder.Neon("Shop_Screen", new Color(0.3f, 1f, 0.6f)), false, true);
            RoomBuilder.Box("BackShelf", kiosk, new Vector3(0f, 1.2f, 1.0f), new Vector3(2.6f, 2.4f, 0.4f), white, true, true);
            for (int lv = 0; lv < 3; lv++)
                for (int k = 0; k < 4; k++)
                    DisplayPrize(kiosk, new[] { "plush_cat", "gummy_red", "plush_green", "soda_cola", "idol_doyun", "keychain_star", "plush_yellow", "squishy_bun", "album_neon", "plush_pink", "monster_grey", "donut_pink" }[lv * 4 + k],
                        new Vector3(-0.9f + k * 0.6f, 0.45f + lv * 0.7f, 0.64f), 0.3f, 0f);
            for (int lv = 0; lv < 3; lv++)
                RoomBuilder.Box("ShelfBoard", kiosk, new Vector3(0f, 0.43f + lv * 0.7f, 0.66f), new Vector3(2.6f, 0.03f, 0.3f), white, false, true);
            var signB = Object.Instantiate(signQ, kiosk);
            signB.transform.localPosition = new Vector3(0f, 2.65f, 0.78f); signB.transform.localRotation = Quaternion.identity; signB.transform.localScale = new Vector3(2.2f, 0.55f, 1f);
            signB.GetComponent<MeshRenderer>().sharedMaterial = ModelLib.Glowing("Sign_Buyback", "sign_buyback", 1.8f);
            var ip2 = new GameObject("BuybackPoint").AddComponent<InteractPoint>();
            ip2.transform.SetParent(room, false); ip2.transform.position = new Vector3(8.4f, 0f, -5.6f);
            ip2.kind = InteractPoint.Kind.Sell; ip2.label = "Rachat de lots — revendre contre des pièces"; ip2.range = 1.9f;
            View(room, "boutique", new Vector3(6.2f, 1.7f, -3.2f), new Vector3(8.4f, 1.1f, -6.6f));

            // --- téléporteur vers la maison (coin avant gauche) + retour depuis la maison
            var pad = Pad(room, new Vector3(-7.5f, 0f, -6.2f), "sign_home", new Color(0.5f, 0.75f, 1f));
            pad.rotation = Quaternion.Euler(0f, 0f, 0f);
            var arrive = new GameObject("ArcadeArrival").transform;
            arrive.SetParent(room, false); arrive.position = new Vector3(-7.5f, 0f, -4.8f); arrive.rotation = Quaternion.identity;
            var ip3 = new GameObject("HomePoint").AddComponent<InteractPoint>();
            ip3.transform.SetParent(pad, false); ip3.transform.localPosition = Vector3.zero;
            ip3.kind = InteractPoint.Kind.GoHome; ip3.label = "Rentrer à la maison"; ip3.destination = houseSpawn; ip3.range = 1.4f;
            var ip4 = new GameObject("ArcadePoint").AddComponent<InteractPoint>();
            ip4.transform.SetParent(houseReturnPad, false); ip4.transform.localPosition = Vector3.zero;
            ip4.kind = InteractPoint.Kind.GoArcade; ip4.label = "Retourner à la salle d'arcade"; ip4.destination = arrive; ip4.range = 1.4f;
            View(room, "teleporteur", new Vector3(-5f, 1.7f, -3.5f), new Vector3(-7.5f, 1.2f, -6.2f));
        }

        static void DisplayPrize(Transform parent, string id, Vector3 localPos, float size, float yaw = 0f)
        {
            var go = ClawMachineBuilder.CreatePrize(id, parent, size);
            if (go == null) return;
            foreach (var c in go.GetComponents<Collider>()) Object.DestroyImmediate(c);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, 180f + yaw, 0f);   // les modèles regardent +Z : on les tourne vers la façade
            var b = ModelLib.LocalBounds(go.transform);
            go.transform.localPosition += Vector3.up * (-b.min.y * go.transform.localScale.y + 0.012f);
        }

        static void LockBox(Transform parent, Vector3 localPos, Material glass, Material gold)
        {
            if (glass != null) RoomBuilder.Box("LockBox", parent, localPos + new Vector3(0f, 0.24f, 0f), new Vector3(0.62f, 0.48f, 0.5f), glass, false, true);
            var pl = ModelLib.Palette(gold);
            pl["Metal"] = gold;
            var lk = ModelLib.Spawn("padlock", parent, pl);
            if (lk == null) return;
            lk.transform.localPosition = localPos + new Vector3(0f, 0.14f, -0.27f);
            lk.transform.localScale = Vector3.one * 0.22f;
        }
    }
}
#endif
