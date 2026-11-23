#if UNITY_EDITOR
using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Machines;
using UnityEditor;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>
    /// Construit le coin pusher avec des PRIMITIVES Unity (modèle provisoire). Repère local : façade du côté -Z,
    /// plateau y = 0,9 (z de -0,55 à 0,95), poussoir cinématique, zone de récupération sous le bord avant.
    /// </summary>
    public static class CoinPusherBuilder
    {
        const string DataDir = "Assets/_Project/Data";
        const string MatDir = "Assets/_Project/Materials";

        public static CoinPusherMachine Build(Transform parent, Vector3 pos, float rotY)
        {
            ProjectSetup.EnsureFolder(DataDir + "/Rewards");
            ProjectSetup.EnsureFolder(MatDir);

            var data = EnsureMachineData();
            var tokenSmall = EnsureReward("token_tickets", "Jeton tickets", Rarity.Rare, 10, 15);
            var tokenJackpot = EnsureReward("token_jackpot", "Jeton Jackpot", Rarity.Epic, 40, 50);

            var body = RoomBuilder.Mat("Pusher_Body", new Color(0.09f, 0.08f, 0.13f), 0.5f);
            var tray = RoomBuilder.Mat("Pusher_Tray", new Color(0.08f, 0.45f, 0.58f), 0.3f);
            var plateMat = RoomBuilder.Mat("Pusher_PlateSteel", new Color(0.55f, 0.58f, 0.68f), 0.9f);   // acier poli
            plateMat.SetFloat("_Metallic", 0.85f);
            var pink = RoomBuilder.Neon("Neon_Pink", new Color(1f, 0.1f, 0.6f));
            var cyan = RoomBuilder.Neon("Neon_Cyan", new Color(0.1f, 0.9f, 1f));
            var yellow = RoomBuilder.Neon("Neon_Yellow", new Color(1f, 0.85f, 0.1f));
            var glass = ClawGlass();
            var coinMat = RoomBuilder.Mat("Pusher_Coin", new Color(0.68f, 0.4f, 0.2f), 0.8f);
            var tokenMat = RoomBuilder.Mat("Pusher_Token", new Color(0.2f, 0.9f, 0.5f), 0.6f);
            var jackpotMat = RoomBuilder.Mat("Pusher_Jackpot", new Color(0.8f, 0.3f, 1f), 0.6f);

            coinPhys = EnsurePhysicsMaterial("Coin_Physics", 0.42f, 0.36f, 0.03f);   // laiton contre laiton : les pièces s'accrochent et avancent en bloc
            var platePhys = EnsurePhysicsMaterial("Plate_Physics", 0.09f, 0.07f, 0.03f, PhysicsMaterialCombine.Minimum);
            var slidePhys = EnsurePhysicsMaterial("Slide_Physics", 0.03f, 0.03f, 0.02f, PhysicsMaterialCombine.Minimum);   // quasi sans frottement : les pièces ne restent pas collées
            
            var trayPhys = EnsurePhysicsMaterial("Tray_Physics", 0.07f, 0.055f, 0.03f, PhysicsMaterialCombine.Minimum);   // métal poli : les pièces glissent sur le plateau
            coinHull = EnsureCoinHull();

            var root = new GameObject("CoinPusher_Classic").transform;
            root.SetParent(parent, false);
            root.position = pos;
            root.rotation = Quaternion.Euler(0f, rotY, 0f);

            // --- Caisson et plateau
            B("BaseLower", root, new Vector3(0, 0.25f, 0.025f), new Vector3(1.5f, 0.5f, 1.85f), body, true);
            B("Tray", root, new Vector3(0, 0.7f, 0.2f), new Vector3(1.3f, 0.4f, 1.5f), tray, true);
            root.Find("Tray").GetComponent<Collider>().sharedMaterial = trayPhys;
            root.Find("Tray").GetComponent<Collider>().contactOffset = 0.002f;
            B("Bin_L", root, new Vector3(-0.7f, 0.7f, -0.725f), new Vector3(0.1f, 0.4f, 0.35f), body, true);
            B("Bin_R", root, new Vector3(0.7f, 0.7f, -0.725f), new Vector3(0.1f, 0.4f, 0.35f), body, true);
            B("Bin_Front", root, new Vector3(0, 0.7f, -0.925f), new Vector3(1.5f, 0.4f, 0.05f), body, true);
            B("BinGlow", root, new Vector3(0, 0.505f, -0.725f), new Vector3(1.2f, 0.01f, 0.3f), yellow, false);

            // --- Parois et plafond
            B("Wall_L", root, new Vector3(-0.675f, 1.2f, 0.2f), new Vector3(0.05f, 0.6f, 1.5f), glass, true);
            B("Wall_R", root, new Vector3(0.675f, 1.2f, 0.2f), new Vector3(0.05f, 0.6f, 1.5f), glass, true);
            B("Wall_Back", root, new Vector3(0, 1.2655f, 0.975f), new Vector3(1.4f, 0.469f, 0.05f), body, true);   // fente en dessous : la plaque coulisse dessous
            // --- Caisson arrière : la plaque recule dedans (elle ne dépasse plus de la machine, comme sur les vraies)
            B("RearHousing", root, new Vector3(0, 1.17f, 1.27f), new Vector3(1.5f, 0.7f, 0.55f), body, false);
            B("RearHousingTrim", root, new Vector3(0, 1.17f, 1.5475f), new Vector3(1.3f, 0.04f, 0.005f), cyan, false);
            B("Top", root, new Vector3(0, 1.62f, 0.2f), new Vector3(1.5f, 0.25f, 1.6f), body, true);
            B("Marquee", root, new Vector3(0, 1.95f, 0.45f), new Vector3(1.5f, 0.4f, 1.0f), pink, false);
            B("FrontPillar_L", root, new Vector3(-0.7f, 1.2f, -0.55f), new Vector3(0.1f, 0.6f, 0.1f), body, true);
            B("FrontPillar_R", root, new Vector3(0.7f, 1.2f, -0.55f), new Vector3(0.1f, 0.6f, 0.1f), body, true);

            // --- Capot racleur en plexiglas : fixe, bord inférieur à 6 mm au-dessus de la plaque (moins épais qu'une pièce)
            var backMat = RoomBuilder.Mat("Pusher_BackWall", new Color(0.1f, 0.07f, 0.28f), 0.45f);
            B("Hood", root, new Vector3(0, 1.263f, 0.315f), new Vector3(1.3f, 0.467f, 0.03f), backMat, true);   // bord bas à 4,5 mm de la plaque : moins épais qu'une pièce
            B("BackDeco_Top", root, new Vector3(0, 1.488f, 0.2985f), new Vector3(1.28f, 0.014f, 0.004f), cyan, false);
            B("BackDeco_Bottom", root, new Vector3(0, 1.04f, 0.2985f), new Vector3(1.28f, 0.014f, 0.004f), pink, false);
            var mural = GameObject.CreatePrimitive(PrimitiveType.Quad);   // fresque du mur du fond (étoiles, vagues, bulles)
            mural.name = "BackMural";
            Object.DestroyImmediate(mural.GetComponent<Collider>());
            mural.transform.SetParent(root, false);
            mural.transform.localPosition = new Vector3(0, 1.265f, 0.2992f);
            mural.transform.localScale = new Vector3(1.26f, 0.43f, 1f);
            mural.GetComponent<MeshRenderer>().sharedMaterial = TexMat("Pusher_Mural", MuralTexture(), 1.1f);

            // --- Couloir de chute : les pièces tombent debout (sur la tranche) contre la paroi du fond, guidées par une vitre fine (27 mm de large)
            // la vitre s'arrête au-dessus des pièces posées sur la plaque (bord bas à 1,145 m) : une pièce qui quitte les toboggans peut basculer à plat vers l'avant
            B("ChuteGuide", root, new Vector3(0, 1.3275f, 0.235f), new Vector3(1.3f, 0.365f, 0.01f), glass, true);
            root.Find("ChuteGuide").GetComponent<Collider>().sharedMaterial = slidePhys;
            root.Find("Hood").GetComponent<Collider>().sharedMaterial = slidePhys;
            root.Find("Hood").GetComponent<Collider>().contactOffset = 0.002f;

            // --- Pas de toboggans : les pièces tombent droit contre le mur du fond, dans la fente choisie (4 fentes)
            // --- Poussoir (Rigidbody cinématique, déplacé par CoinPusherMachine)
            var pusherGo = new GameObject("Pusher");
            pusherGo.transform.SetParent(root, false);
            pusherGo.transform.localPosition = new Vector3(0, 0.965f, 0.17f);   // au repos la plaque est sortie
            pusherGo.transform.localRotation = Quaternion.Euler(-PlateTilt, 0f, 0f);
            var prb = pusherGo.AddComponent<Rigidbody>();
            prb.isKinematic = true;
            prb.interpolation = RigidbodyInterpolation.Interpolate;
            prb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var pcol = pusherGo.AddComponent<BoxCollider>();
            pcol.size = new Vector3(1.28f, 0.12f, 1.12f);
            pcol.center = new Vector3(0f, 0f, 0.25f);   // prolongée vers l'arrière : plus aucun trou derrière la plaque
            pcol.sharedMaterial = platePhys;
            pcol.contactOffset = 0.002f;
            var pvis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pvis.name = "PlateVisual";
            Object.DestroyImmediate(pvis.GetComponent<BoxCollider>());
            pvis.transform.SetParent(pusherGo.transform, false);
            pvis.transform.localScale = new Vector3(1.28f, 0.12f, 0.62f);
            pvis.GetComponent<MeshRenderer>().sharedMaterial = plateMat;
            var prear = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prear.name = "PlateRear";
            Object.DestroyImmediate(prear.GetComponent<BoxCollider>());
            prear.transform.SetParent(pusherGo.transform, false);
            prear.transform.localPosition = new Vector3(0f, 0f, 0.56f);
            prear.transform.localScale = new Vector3(1.28f, 0.12f, 0.5f);
            prear.GetComponent<MeshRenderer>().sharedMaterial = plateMat;
            // liserés lumineux : nez de plaque et rainures du dessus (visuels seulement)
            foreach (var st in new[] { (new Vector3(0f, 0.032f, -0.3115f), new Vector3(1.28f, 0.022f, 0.004f), 0), (new Vector3(0f, -0.022f, -0.3115f), new Vector3(1.28f, 0.01f, 0.004f), 1) })
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                s.name = "PlateNose_" + st.Item3;
                Object.DestroyImmediate(s.GetComponent<BoxCollider>());
                s.transform.SetParent(pusherGo.transform, false);
                s.transform.localPosition = st.Item1; s.transform.localScale = st.Item2;
                s.GetComponent<MeshRenderer>().sharedMaterial = st.Item3 == 0 ? RoomBuilder.Neon("Neon_Yellow", new Color(1f, 0.85f, 0.2f)) : RoomBuilder.Neon("Neon_Cyan", new Color(0.2f, 0.9f, 1f));
            }
            for (int g = 0; g < 6; g++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                s.name = "PlateGroove_" + g;
                Object.DestroyImmediate(s.GetComponent<BoxCollider>());
                s.transform.SetParent(pusherGo.transform, false);
                s.transform.localPosition = new Vector3(0f, 0.0598f, -0.27f + g * 0.1f);
                s.transform.localScale = new Vector3(1.24f, 0.0016f, 0.005f);
                s.GetComponent<MeshRenderer>().sharedMaterial = RoomBuilder.Neon("Neon_Cyan", new Color(0.2f, 0.9f, 1f));
            }

            // --- Commandes : indicateur de visée (se déplace réellement) et bouton (s'enfonce à chaque pièce)
            var aim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aim.name = "AimIndicator";
            Object.DestroyImmediate(aim.GetComponent<BoxCollider>());
            aim.transform.SetParent(root, false);
            aim.transform.localPosition = new Vector3(0, 1.48f, 0.27f);
            aim.transform.localScale = new Vector3(0.14f, 0.03f, 0.14f);
            aim.GetComponent<MeshRenderer>().sharedMaterial = yellow;

            B("Podium", root, new Vector3(0, 0.3f, -1.15f), new Vector3(1.0f, 0.6f, 0.4f), body, true);
            B("Deck", root, new Vector3(0, 0.625f, -1.15f), new Vector3(1.0f, 0.05f, 0.4f), body, true);
            var button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            button.name = "DropButton";
            Object.DestroyImmediate(button.GetComponent<Collider>());
            button.transform.SetParent(root, false);
            button.transform.localPosition = new Vector3(0, 0.665f, -1.15f);
            button.transform.localScale = new Vector3(0.16f, 0.015f, 0.16f);
            button.GetComponent<MeshRenderer>().sharedMaterial = cyan;

            // --- Modèle de pièce (désactivé, copié par le pool) + pièces et lots posés sur le plateau
            var template = BuildCoin("CoinTemplate", root, new Vector3(0, 2.5f, 0), 0f, coinMat, CoinD, CoinT, null);
            template.SetActive(false);

            var coins = new GameObject("Coins").transform;
            coins.SetParent(root, false);
            int index = 0;
            var rnd = new System.Random(2024);

            // Mécanisme réel : la plaque mobile (étage supérieur) porte les pièces ; un capot en plexiglas fixe, juste au-dessus d'elle,
            // racle les pièces quand elle recule : elles tombent alors devant elle sur le plateau fixe, et la plaque les pousse vers le bord.
            // Au repos, la plaque est sortie : ses rangées de pièces sont devant le capot.
            float plateTop = 0.965f + 0.06f;
            // zones réservées (x, z, rayon de l'objet) : aucune pièce ne doit chevaucher un objet spécial
            var reserved = new List<Vector3>();
            System.Action<Vector3, float> reserve = (pos, r) => reserved.Add(new Vector3(pos.x, r, pos.z));
            System.Func<float, float, bool> isReserved = (px, pz) =>
            {
                foreach (var r in reserved)
                {
                    float lim = r.y + CoinD * 0.5f + 0.004f;
                    if ((r.x - px) * (r.x - px) + (r.z - pz) * (r.z - pz) < lim * lim) return true;
                }
                return false;
            };

            // objets spéciaux : leur place est réservée (aucune pièce dessus)
            var bonusPlan = new[]
            {
                (new Vector3(-0.34f, plateTop + 0.0065f, 0.115f), PusherBonus.Kind.CoinRain, new Color(0.2f, 0.9f, 1f)),
                (new Vector3(0.0f, plateTop + 0.0065f, 0.115f), PusherBonus.Kind.DoubleGains, new Color(1f, 0.5f, 0.1f)),
                (new Vector3(0.3f, plateTop + 0.0065f, 0.115f), PusherBonus.Kind.Wheel, new Color(0.7f, 0.3f, 1f)),
                (new Vector3(-0.27f, plateTop + 0.0065f, 0.0f), PusherBonus.Kind.Wheel, new Color(0.7f, 0.3f, 1f)),
                (new Vector3(0.27f, 0.9055f, -0.36f), PusherBonus.Kind.Gem, new Color(0.35f, 0.7f, 1f)),
            };
            var prizePlan = new[]
            {
                ("capsule_purple", new Vector3(-0.46f, plateTop + 0.05f, 0.14f), 0.085f),
                ("mini_toy_green", new Vector3(0.47f, plateTop + 0.05f, 0.14f), 0.085f),
                ("figurine_gold", new Vector3(0.18f, plateTop + 0.06f, 0.2f), 0.1f),
                ("plush_pink", new Vector3(-0.28f, 0.96f, -0.3f), 0.1f),
                ("figurine_silver", new Vector3(-0.5f, 0.96f, -0.4f), 0.1f),
            };
            var tokenPlan = new[]
            {
                (new Vector3(0.35f, 0.9055f, -0.22f), tokenMat, 0.09f, tokenSmall),
                (new Vector3(-0.2f, 0.9055f, -0.2f), jackpotMat, 0.1f, tokenJackpot),
            };
            // faux billets en plastique (épais de 6 mm : le capot les racle comme les pièces) : valeur selon la couleur
            var billPlan = new[]
            {
                (new Vector3(-0.1f, plateTop + 0.0035f, 0.05f), 2, 50), (new Vector3(0.2f, plateTop + 0.0035f, 0.0f), 0, 20),
                (new Vector3(-0.6f + 0.06f, plateTop + 0.0035f, -0.02f), 3, 100),
                (new Vector3(0.3f, 0.9035f, -0.45f), 2, 50),
            };
            foreach (var b in bonusPlan) reserve(b.Item1, 0.0525f);
            foreach (var pz in prizePlan) reserve(pz.Item2, pz.Item3 * 0.55f);
            foreach (var t in tokenPlan) reserve(t.Item1, t.Item3 * 0.5f);
            foreach (var bl in billPlan) reserve(bl.Item1, 0.066f);
            var ballPlan = new[] { new Vector3(-0.05f, plateTop + 0.036f, -0.08f), new Vector3(0.42f, plateTop + 0.036f, -0.08f) };
            foreach (var bp in ballPlan) reserve(bp, 0.035f);
            reserve(new Vector3(0.6f, 0f, -0.5f), 0.045f);   // pied de l'ascenseur à balles
            var coinMatLight = RoomBuilder.Mat("Pusher_Coin_Light", new Color(0.82f, 0.52f, 0.3f), 0.8f);

            // plateau fixe : tapis serré (empilement hexagonal) jusqu'au bord, comme une machine en régime normal :
            // la poussée de la plaque se transmet à travers le tapis et fait tomber les pièces du bord.
            int row = 0;
            for (float z = -0.173f; z >= -0.541f; z -= 0.0572f, row++)
                for (float x = -0.6f + (row % 2) * 0.033f; x <= 0.6f; x += 0.066f)
                    for (int layer = 0; layer < 3; layer++)
                    {
                        if (layer == 1 && rnd.NextDouble() < 0.2) continue;
                        if (layer == 2 && rnd.NextDouble() < 0.65) continue;
                        float jx = (float)(rnd.NextDouble() - 0.5) * 0.003f, jz = (float)(rnd.NextDouble() - 0.5) * 0.003f;
                        float ox = layer == 1 ? 0.033f : (layer == 2 ? -0.016f : 0f), oz = layer == 1 ? 0.019f : (layer == 2 ? 0.029f : 0f);
                        float px = Mathf.Clamp(x + jx + ox, -0.615f, 0.615f), pzz = Mathf.Clamp(z + jz + oz, -0.54f, -0.172f);
                        if (isReserved(px, pzz)) continue;
                        index++;
                        Vector3 p = new Vector3(px, 0.9036f + layer * 0.0072f, pzz);
                        float yaw = (float)(rnd.NextDouble() * 360.0);
                        var c = BuildCoin("Coin_" + index, coins, p, yaw, rnd.NextDouble() < 0.2 ? coinMatLight : coinMat, CoinD, CoinT, null);
                        float lean = layer == 0 ? 0f : 4f;
                        c.transform.localRotation = Quaternion.Euler((float)(rnd.NextDouble() - 0.5) * lean, yaw, (float)(rnd.NextDouble() - 0.5) * lean);
                    }

            // plaque sortie : rangées bien alignées devant le capot (1 à 2 couches)
            for (int layer = 0; layer < 3; layer++)
                for (float z = 0.262f; z >= -0.11f; z -= 0.066f)
                    for (float x = -0.585f; x <= 0.59f; x += 0.066f)
                    {
                        if (layer >= 1 && rnd.NextDouble() < 0.4) continue;
                        float jx = (float)(rnd.NextDouble() - 0.5) * 0.003f, jz = (float)(rnd.NextDouble() - 0.5) * 0.003f;
                        if (isReserved(x + jx + (layer == 1 ? 0.033f : 0f), z + jz)) continue;
                        index++;
                        Vector3 p = new Vector3(x + jx + (layer == 1 ? 0.033f : 0f), plateTop + 0.0036f + layer * 0.0072f, z + jz);
                        float yaw = (float)(rnd.NextDouble() * 360.0);
                        BuildCoin("Coin_" + index, coins, p, yaw, rnd.NextDouble() < 0.2 ? coinMatLight : coinMat, CoinD, CoinT, null);
                    }

            foreach (var t in tokenPlan) BuildCoin("Token_" + (++index), coins, t.Item1, (float)(rnd.NextDouble() * 360.0), t.Item2, t.Item3, 0.01f, t.Item4);
            foreach (var bl in billPlan) BillAt(coins, "Bill_" + (++index), bl.Item1, (float)(rnd.NextDouble() * 360.0), bl.Item2, bl.Item3);
            foreach (var b in bonusPlan) BonusAt(coins, "Bonus_" + b.Item2 + "_" + (++index), b.Item1, b.Item2, b.Item3);
            foreach (var pz in prizePlan) PrizeAt(coins, pz.Item1, pz.Item2, pz.Item3);
            var ballMat = RoomBuilder.Mat("Pusher_JackpotBall", new Color(1f, 0.72f, 0.12f), 0.96f);
            for (int bi = 0; bi < ballPlan.Length; bi++)   // balles jackpot (chrome doré)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "JackpotBall_" + bi;
                ball.transform.SetParent(coins, false);
                ball.transform.localPosition = ballPlan[bi];
                ball.transform.localScale = Vector3.one * 0.07f;
                ball.GetComponent<MeshRenderer>().sharedMaterial = ballMat;
                var sc = ball.GetComponent<SphereCollider>(); sc.sharedMaterial = coinPhys; sc.contactOffset = 0.002f;
                var brb = ball.AddComponent<Rigidbody>();
                brb.mass = 0.08f; brb.angularDamping = 0.4f; brb.linearDamping = 0.05f;
                brb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                brb.solverIterations = 12; brb.maxDepenetrationVelocity = 1.2f;
                ball.AddComponent<PusherBall>();
                ball.AddComponent<PusherBonusVisual>().customText = "JACKPOT";
            }

            // --- Habillage détaillé : coque FBX texturée ; les primitives restent comme volumes physiques invisibles
            var pal = ModelLib.Palette(null);
            // carrosserie laquée (violet profond) et chromes : fini « borne neuve » au lieu du métal brut texturé
            var cab = ModelLib.Palette(null);
            var lacquer = RoomBuilder.Mat("Pusher_Lacquer", new Color(0.26f, 0.05f, 0.36f), 0.9f);
            lacquer.SetFloat("_Metallic", 0.35f);
            var chrome = RoomBuilder.Mat("Pusher_Chrome", new Color(0.92f, 0.93f, 0.97f), 0.95f);
            chrome.SetFloat("_Metallic", 1f);
            cab["Main"] = lacquer; cab["Paint"] = lacquer; cab["Metal"] = chrome;
            ModelLib.Textured(tray, "felt", null, 3f, 0f, 0.15f);
            var shell = ModelLib.Spawn("pusher_shell", root, cab);
            if (shell != null)
            {
                var hide = new HashSet<string> { "BaseLower", "Bin_L", "Bin_R", "Bin_Front", "Wall_Back", "Top", "Marquee", "FrontPillar_L", "FrontPillar_R", "Podium", "Deck" };
                var toHide = new List<Transform>();
                foreach (Transform c in root) if (hide.Contains(c.name)) toHide.Add(c);
                foreach (var c in toHide)
                {
                    if (c.GetComponent<Collider>() == null) Object.DestroyImmediate(c.gameObject);
                    else c.GetComponent<Renderer>().enabled = false;
                }
            }
            ModelLib.Dress(aim.transform, "pusher_aim", pal);
            ModelLib.Dress(button.transform, "arcade_button", ModelLib.WithButtonColor(pal, new Color(0.1f, 0.9f, 1f)), 0.65f);

            // --- Vitre avant (visuelle seulement : les pièces tombent par-dessous), rail d'insertion et fente
            B("Glass_Front", root, new Vector3(0, 1.2f, -0.575f), new Vector3(1.3f, 0.55f, 0.02f), glass, false);
            B("InsertRail", root, new Vector3(0, 1.49f, 0.27f), new Vector3(1.1f, 0.02f, 0.12f), body, false);
            B("InsertSlot", root, new Vector3(0, 1.488f, 0.27f), new Vector3(1.0f, 0.005f, 0.03f), yellow, false);
            for (int sl = 0; sl < CoinPusherMachine.SlotX.Length; sl++)   // 4 fentes d'insertion repérées
            {
                B("SlotMark_" + (sl + 1), root, new Vector3(CoinPusherMachine.SlotX[sl], 1.5f, 0.27f), new Vector3(0.11f, 0.012f, 0.05f), cyan, false);
                B("SlotMouth_" + (sl + 1), root, new Vector3(CoinPusherMachine.SlotX[sl], 1.502f, 0.27f), new Vector3(0.07f, 0.014f, 0.016f), pink, false);
            }

            // --- Afficheur numérique (canvas monde, face au joueur) et éclairages LED animés
            var display = new GameObject("Display").transform;
            display.SetParent(root, false);
            display.localPosition = new Vector3(0, 1.62f, -0.612f);
            B("DisplayFrame", root, new Vector3(0, 1.62f, -0.604f), new Vector3(1.04f, 0.2f, 0.012f), body, false);
            var leds = new Light[4];
            Vector3[] ledPos = { new Vector3(-0.62f, 1.45f, -0.45f), new Vector3(0.62f, 1.45f, -0.45f), new Vector3(-0.62f, 1.4f, 0.7f), new Vector3(0.62f, 1.4f, 0.7f) };
            for (int i = 0; i < leds.Length; i++)
            {
                var lg = new GameObject("Led_" + i);
                lg.transform.SetParent(root, false);
                lg.transform.localPosition = ledPos[i];
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point; l.range = 1.6f; l.intensity = 1f; l.shadows = LightShadows.None;
                leds[i] = l;
            }

            // --- Ancrage caméra
            var anchor = new GameObject("CameraAnchor").transform;
            anchor.SetParent(root, false);
            anchor.localPosition = new Vector3(0, 1.75f, -2.4f);
            anchor.localRotation = Quaternion.LookRotation(new Vector3(0, 0.95f, 0.35f) - anchor.localPosition);

            var machine = root.gameObject.AddComponent<CoinPusherMachine>();
            machine.Configure(data, anchor, prb, aim.transform, button.transform, template.transform, coins);
            machine.SetupExtras(display, leds);

            // --- 5 voies de récupération sous le bord avant (une voie « chance » éclairée à tour de rôle : gains x3)
            float[] laneX = { -0.52f, -0.26f, 0f, 0.26f, 0.52f };
            var laneOff = RoomBuilder.Mat("Pusher_LaneOff", new Color(0.08f, 0.3f, 0.4f), 0.4f);
            var laneLights = new Renderer[laneX.Length];
            for (int ln = 0; ln < laneX.Length; ln++)
            {
                var zone = new GameObject("Collector_" + ln);
                zone.transform.SetParent(root, false);
                zone.transform.localPosition = new Vector3(laneX[ln], 0.68f, -0.72f);
                var bc = zone.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(0.262f, 0.34f, 0.32f);
                var col = zone.AddComponent<PusherCollector>();
                col.Configure(machine);
                col.lane = ln;
                var ll = RoomBuilder.Box("LaneLight_" + ln, root, new Vector3(laneX[ln], 0.897f, -0.566f), new Vector3(0.22f, 0.012f, 0.022f), laneOff, false, true);
                GameObjectUtility.SetStaticEditorFlags(ll, 0);
                laneLights[ln] = ll.GetComponent<Renderer>();
                if (ln < laneX.Length - 1) GameObjectUtility.SetStaticEditorFlags(RoomBuilder.Box("LaneDivider_" + ln, root, new Vector3(laneX[ln] + 0.13f, 0.93f, -0.566f), new Vector3(0.012f, 0.07f, 0.022f), body, false, true), 0);
            }

            // --- Plateau fixe vibrant (corps cinématique)
            var mech = root.gameObject.AddComponent<PusherMechanics>();
            var trayT = root.Find("Tray");
            mech.tray = null; mech.trayLocal = trayT.localPosition;   // pas de vibreur : les vraies machines n'en ont pas, et les pièces peuvent dormir
            mech.laneLights = laneLights; mech.laneOn = yellow; mech.laneOff = laneOff;

            // --- Roue bonus du mur du fond (découpée dans la texture : alpha clipping) + pointeur
            var wheelQ = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wheelQ.name = "BonusWheel";
            Object.DestroyImmediate(wheelQ.GetComponent<Collider>());
            wheelQ.transform.SetParent(root, false);
            wheelQ.transform.localPosition = new Vector3(0, 1.235f, 0.298f);
            wheelQ.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
            var wheelMat = TexMat("Pusher_Wheel", WheelTexture(), 1.2f);
            wheelMat.SetFloat("_AlphaClip", 1f); wheelMat.SetFloat("_Cutoff", 0.5f);
            wheelMat.EnableKeyword("_ALPHATEST_ON");
            wheelMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            wheelQ.GetComponent<MeshRenderer>().sharedMaterial = wheelMat;
            mech.wheel = wheelQ.transform;
            var pointer = RoomBuilder.Box("WheelPointer", root, new Vector3(0, 1.418f, 0.296f), new Vector3(0.032f, 0.032f, 0.006f), yellow, false, true);
            pointer.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // --- Modèles de billets (désactivés) pour la pluie de billets
            var billTemplates = new Transform[BillColors.Length];
            int[] billValues = { 20, 35, 50, 100 };
            for (int bt = 0; bt < billTemplates.Length; bt++)
            {
                var tgo = BillAt(root, "BillTemplate_" + bt, new Vector3(0f, 2.6f, 0f), 0f, bt, billValues[bt]);
                tgo.SetActive(false);
                billTemplates[bt] = tgo.transform;
            }
            // --- Ascenseur à balles : tube en verre dans le coin avant droit (poteau physique : les pièces le contournent)
            var tubeGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tubeGo.name = "BallLift";
            tubeGo.transform.SetParent(root, false);
            tubeGo.transform.localPosition = new Vector3(0.6f, 1.19f, -0.5f);
            tubeGo.transform.localScale = new Vector3(0.09f, 0.29f, 0.09f);
            tubeGo.GetComponent<MeshRenderer>().sharedMaterial = glass;
            GameObjectUtility.SetStaticEditorFlags(tubeGo, 0);
            foreach (float ry in new[] { 0.905f, 1.19f, 1.475f })
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "BallLiftRing";
                Object.DestroyImmediate(ring.GetComponent<Collider>());
                ring.transform.SetParent(root, false);
                ring.transform.localPosition = new Vector3(0.6f, ry, -0.5f);
                ring.transform.localScale = new Vector3(0.1f, 0.006f, 0.1f);
                ring.GetComponent<MeshRenderer>().sharedMaterial = yellow;
            }
            machine.SetupMechanics(mech, billTemplates, tubeGo.transform);

            // --- Décor : guirlande chenillard, lumières d'ambiance, liserés lumineux
            mech.marquee = BuildMarquee(root, yellow, RoomBuilder.Mat("Pusher_BulbOff", new Color(0.42f, 0.36f, 0.24f), 0.6f));
            B("BaseGlow", root, new Vector3(0, 0.06f, -0.935f), new Vector3(1.44f, 0.02f, 0.012f), cyan, false);
            B("DeckEdgeGlow", root, new Vector3(0, 0.893f, -0.575f), new Vector3(1.3f, 0.012f, 0.012f), pink, false);
            var ambientList = new List<Light>();
            foreach (var lp in new[] { (-0.45f, new Color(0.2f, 0.9f, 1f)), (0.45f, new Color(1f, 0.25f, 0.7f)) })
            {
                var lg = new GameObject("AmbientLight").AddComponent<Light>();
                lg.transform.SetParent(root, false);
                lg.transform.localPosition = new Vector3(lp.Item1, 1.42f, 0.1f);
                lg.type = LightType.Point; lg.color = lp.Item2; lg.range = 1.3f; lg.intensity = 2.5f; lg.shadows = LightShadows.None;
                ambientList.Add(lg);
            }
            mech.ambient = ambientList.ToArray();

            return machine;
        }

        /// <summary>Crée le prefab de la machine (placée dynamiquement par BuildingManager) et le relie à sa MachineData.</summary>
        public static MachineData EnsurePrefab()
        {
            ProjectSetup.EnsureFolder("Assets/_Project/Prefabs");
            MachineData data = EnsureMachineData();
            var go = Build(null, Vector3.zero, 0f).gameObject;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            data.prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/_Project/Prefabs/CoinPusher_Classic.prefab");
            Object.DestroyImmediate(go);
            EditorUtility.SetDirty(data);
            return data;
        }

        // ------------------------------------------------------------------ billets, fresque, guirlande
        static readonly Color[] BillColors = { new Color(0.25f, 0.75f, 0.35f), new Color(0.3f, 0.55f, 1f), new Color(1f, 0.4f, 0.7f), new Color(1f, 0.8f, 0.2f) };

        static Material TexMat(string name, Texture2D tex, float emission)
        {
            var m = RoomBuilder.Mat(name, Color.white, 0.35f);
            m.SetTexture("_BaseMap", tex);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", Color.white * emission * 0.55f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Texture2D SaveTexture(string name, Texture2D t)
        {
            ProjectSetup.EnsureFolder("Assets/_Project/Generated");
            string path = "Assets/_Project/Generated/" + name + ".png";
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Faux billet : fond coloré, double bordure, médaillon central, guillochis et coins numérotés.</summary>
        static Texture2D BillTexture(int variant)
        {
            const int W = 256, H = 112;
            Color c = BillColors[variant];
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W, v = y / (float)H;
                    float g = 0.75f + 0.25f * Mathf.Sin(u * 90f + Mathf.Sin(v * 22f) * 2.5f) * Mathf.Cos(v * 60f);
                    Color col = Color.Lerp(c * 0.55f, Color.Lerp(c, Color.white, 0.35f), g * (0.5f + 0.5f * v));
                    int ex = Mathf.Min(x, W - 1 - x), ey = Mathf.Min(y, H - 1 - y), e = Mathf.Min(ex, ey);
                    if (e < 3) col = Color.Lerp(c, Color.white, 0.8f);
                    else if (e >= 6 && e < 8) col = c * 0.35f;
                    float dx = (x - W * 0.5f) / (H * 0.36f), dy = (y - H * 0.5f) / (H * 0.36f), d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < 1f) col = Color.Lerp(col, Color.white, 0.55f);
                    if (d < 1f && d > 0.88f) col = c * 0.3f;
                    if (d < 0.55f) col = Color.Lerp(c * 0.45f, c, Mathf.Clamp01(dy * 0.5f + 0.6f));
                    if (d < 0.2f && dy > 0f) col = c * 0.3f;
                    bool cornerA = x > 12 && x < 52 && y > H - 34 && y < H - 8 && ((x / 6 + y / 6) % 2 == 0);
                    bool cornerB = x < W - 12 && x > W - 52 && y < 34 && y > 8 && ((x / 6 + y / 6) % 2 == 0);
                    if (cornerA || cornerB) col = c * 0.25f;
                    col.a = 1f;
                    px[y * W + x] = col;
                }
            tex.SetPixels(px); tex.Apply();
            return SaveTexture("Pusher_Bill_" + variant, tex);
        }

        /// <summary>Roue bonus : 8 segments (ordre = CoinPusherMachine.Outcome), le segment k occupe l'angle (k+0,5) x 45° à partir de la droite.</summary>
        static Texture2D WheelTexture()
        {
            const int S = 512;
            Color[] cols =
            {
                new Color(1f, 0.8f, 0.2f), new Color(1f, 0.3f, 0.7f), new Color(0.2f, 0.9f, 1f), new Color(0.3f, 0.5f, 1f),
                new Color(0.3f, 0.9f, 0.4f), new Color(1f, 0.5f, 0.1f), new Color(0.7f, 0.3f, 1f), new Color(0.95f, 0.95f, 1f)
            };
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = x - S * 0.5f + 0.5f, dy = y - S * 0.5f + 0.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    Color col = new Color(0f, 0f, 0f, 0f);
                    if (r <= 254f)
                    {
                        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg; if (ang < 0f) ang += 360f;
                        int k = Mathf.Min(7, Mathf.FloorToInt(ang / 45f));
                        float within = ang - k * 45f;
                        col = Color.Lerp(cols[k] * 0.75f, cols[k], Mathf.Clamp01(r / 230f));
                        if (within < 1.6f || within > 43.4f) col = new Color(0.08f, 0.05f, 0.2f);   // séparateurs
                        if (r > 232f) col = ((Mathf.FloorToInt(ang / 7.5f) % 2) == 0) ? new Color(1f, 0.9f, 0.4f) : new Color(0.15f, 0.08f, 0.3f);   // couronne d'ampoules
                        if (r < 46f) col = new Color(1f, 0.85f, 0.3f);
                        if (r < 46f && r > 38f) col = new Color(0.4f, 0.2f, 0.05f);
                        // k+1 pastilles par segment pour les distinguer
                        float mid = (k + 0.5f) * 45f * Mathf.Deg2Rad;
                        for (int d = 0; d <= k; d++)
                        {
                            float off = (d - k * 0.5f) * 0.11f;
                            float cx = Mathf.Cos(mid + off) * 150f, cy = Mathf.Sin(mid + off) * 150f;
                            if ((dx - cx) * (dx - cx) + (dy - cy) * (dy - cy) < 49f) col = new Color(0.08f, 0.05f, 0.2f);
                        }
                        col.a = 1f;
                    }
                    px[y * S + x] = col;
                }
            tex.SetPixels(px); tex.Apply();
            return SaveTexture("Pusher_Wheel", tex);
        }

        static readonly Dictionary<int, Material> billMats = new Dictionary<int, Material>();

        static GameObject BillAt(Transform parent, string name, Vector3 pos, float yaw, int variant, int value)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(0.12f, 0.006f, 0.056f);
            var bc = go.GetComponent<BoxCollider>(); bc.sharedMaterial = coinPhys; bc.contactOffset = 0.002f;
            if (!billMats.TryGetValue(variant, out var bm) || bm == null) billMats[variant] = bm = TexMat("Pusher_Bill_" + variant, BillTexture(variant), 0.5f);
            go.GetComponent<MeshRenderer>().sharedMaterial = bm;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.01f; rb.linearDamping = 0.02f; rb.angularDamping = 0.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.sleepThreshold = 0.03f; rb.solverIterations = 12; rb.maxDepenetrationVelocity = 1.2f;
            var pb = go.AddComponent<PusherBonus>(); pb.kind = PusherBonus.Kind.Cash; pb.value = value;
            return go;
        }

        /// <summary>Fresque du mur du fond : dégradé nuit, étoiles, vagues néon, bulles et soleil doré.</summary>
        static Texture2D MuralTexture()
        {
            const int W = 512, H = 176;
            var rng = new System.Random(7);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float v = y / (float)H, u = x / (float)W;
                    Color col = Color.Lerp(new Color(0.05f, 0.04f, 0.22f), new Color(0.35f, 0.1f, 0.45f), 1f - v);
                    float w1 = 0.22f + 0.05f * Mathf.Sin(u * 28f), w2 = 0.34f + 0.05f * Mathf.Sin(u * 21f + 1.5f), w3 = 0.46f + 0.04f * Mathf.Sin(u * 35f + 3f);
                    if (Mathf.Abs(v - w1) < 0.012f) col = new Color(0.1f, 0.9f, 1f);
                    if (Mathf.Abs(v - w2) < 0.012f) col = new Color(1f, 0.2f, 0.65f);
                    if (Mathf.Abs(v - w3) < 0.01f) col = new Color(1f, 0.85f, 0.2f);
                    float dx = (u - 0.5f) * (W / (float)H), dy = (v - 0.62f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < 0.2f) col = Color.Lerp(new Color(1f, 0.6f, 0.15f), new Color(1f, 0.9f, 0.4f), 1f - d / 0.2f);
                    else if (d < 0.23f) col = new Color(1f, 0.3f, 0.5f);
                    col.a = 1f;
                    px[y * W + x] = col;
                }
            for (int i = 0; i < 90; i++)   // étoiles
            {
                int sx = rng.Next(4, W - 4), sy = rng.Next(60, H - 4), r = rng.Next(1, 3);
                Color sc = i % 3 == 0 ? new Color(1f, 0.9f, 0.4f) : Color.white;
                for (int k = -r; k <= r; k++) { px[sy * W + Mathf.Clamp(sx + k, 0, W - 1)] = sc; px[Mathf.Clamp(sy + k, 0, H - 1) * W + sx] = sc; }
            }
            for (int i = 0; i < 26; i++)   // bulles
            {
                int cx = rng.Next(10, W - 10), cy = rng.Next(6, H - 40), r = rng.Next(4, 11);
                for (int a = 0; a < 48; a++)
                {
                    int bx = cx + Mathf.RoundToInt(Mathf.Cos(a / 48f * 6.2832f) * r), by = cy + Mathf.RoundToInt(Mathf.Sin(a / 48f * 6.2832f) * r);
                    if (bx >= 0 && bx < W && by >= 0 && by < H) px[by * W + bx] = new Color(0.7f, 0.95f, 1f);
                }
            }
            tex.SetPixels(px); tex.Apply();
            return SaveTexture("Pusher_Mural", tex);
        }

        /// <summary>Guirlande d'ampoules chenillard autour de l'ouverture.</summary>
        static PusherMarquee BuildMarquee(Transform root, Material onMat, Material offMat)
        {
            var bulbs = new List<Renderer>();
            var holder = new GameObject("Marquee").transform;
            holder.SetParent(root, false);
            System.Action<Vector3> add = pos =>
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Bulb";
                Object.DestroyImmediate(s.GetComponent<Collider>());
                s.transform.SetParent(holder, false);
                s.transform.localPosition = pos;
                s.transform.localScale = Vector3.one * 0.032f;
                s.GetComponent<MeshRenderer>().sharedMaterial = offMat;
                bulbs.Add(s.GetComponent<MeshRenderer>());
            };
            for (float x = -0.64f; x <= 0.645f; x += 0.08f) add(new Vector3(x, 1.468f, -0.61f));
            for (float y = 1.43f; y >= 0.99f; y -= 0.073f) { add(new Vector3(-0.7f, y, -0.607f)); add(new Vector3(0.7f, y, -0.607f)); }
            var m = holder.gameObject.AddComponent<PusherMarquee>();
            m.bulbs = bulbs.ToArray(); m.on = onMat; m.off = offMat;
            return m;
        }

        static void BonusAt(Transform parent, string name, Vector3 pos, PusherBonus.Kind kind, Color color)
        {
            var mat = RoomBuilder.Mat("Pusher_Bonus_" + kind, color, 0.7f);
            var go = BuildCoin(name, parent, pos, 0f, mat, 0.105f, 0.012f, null, kind);
            go.transform.localRotation = Quaternion.identity;
            go.AddComponent<PusherBonusVisual>();   // étiquette flottante + pulsation : bien visible comme sur une vraie borne
        }

        static void PrizeAt(Transform parent, string id, Vector3 pos, float size)
        {
            RewardData reward = ClawMachineBuilder.GetReward(id);
            if (reward == null || !ClawMachineBuilder.GetPrizeLook(id, out string model, out Color color, out Rarity rarity)) return;
            var mat = RoomBuilder.Mat("Prize_" + id, color, 0.35f);
            var visual = ModelLib.Spawn(model, null, ModelLib.Palette(mat));
            if (visual == null) return;
            var go = new GameObject("Prize_" + id);
            go.transform.SetParent(parent, false);
            visual.transform.SetParent(go.transform, false);
            var raw = ModelLib.LocalBounds(go.transform);
            float u = size / Mathf.Max(raw.size.x, raw.size.y, raw.size.z);
            go.transform.localScale = new Vector3(u, u, u);
            var b = ModelLib.LocalBounds(go.transform);
            var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size * 0.85f;
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.06f; rb.linearDamping = 0.1f; rb.angularDamping = 0.4f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            go.AddComponent<PrizeItem>().reward = reward;
        }

        /// <summary>Pièce ou jeton : cylindre plat, collider convexe, Rigidbody à détection continue.</summary>
        static GameObject BuildCoin(string name, Transform parent, Vector3 localPos, float yaw, Material mat, float diameter, float thickness, RewardData reward, PusherBonus.Kind? bonus = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(diameter, thickness * 0.5f, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
            var meshCol = go.AddComponent<MeshCollider>();
            meshCol.sharedMesh = coinHull;      // enveloppe convexe à 12 pans (bien plus légère que le cylindre visuel)
            meshCol.convex = true;
            meshCol.sharedMaterial = coinPhys;
            var detail = ModelLib.Spawn("coin_unit", null, ModelLib.CoinPalette(mat.name, mat.GetColor("_BaseColor")));
            if (detail != null)
            {
                var dmf = detail.GetComponentInChildren<MeshFilter>(); var dr = detail.GetComponentInChildren<MeshRenderer>();
                if (dmf != null && dr != null)
                {
                    go.GetComponent<MeshFilter>().sharedMesh = dmf.sharedMesh;
                    go.GetComponent<MeshRenderer>().sharedMaterials = dr.sharedMaterials;
                }
                Object.DestroyImmediate(detail);
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = (reward == null && bonus == null) ? 0.012f : 0.035f;
            meshCol.contactOffset = 0.002f;     // contact fin : l'épaisseur d'une pièce est de quelques mm
            rb.maxDepenetrationVelocity = 1.2f; // plus d'éjections quand deux pièces se chevauchent
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.angularDamping = 0.12f;
            rb.linearDamping = 0.02f;
            rb.sleepThreshold = 0.03f;          // les pièces au repos s'endorment : peu de CPU, elles se réveillent au contact
            rb.solverIterations = 12;
            rb.solverVelocityIterations = 3;
            rb.interpolation = RigidbodyInterpolation.None;

            if (bonus.HasValue) go.AddComponent<PusherBonus>().kind = bonus.Value;
            else if (reward == null) go.AddComponent<PusherCoin>();
            else go.AddComponent<PrizeItem>().reward = reward;
            return go;
        }

        const float PlateTilt = 0f;
        // Pièces réalistes : ~6 cm de diamètre pour une plaque de 1,28 m (rapport d'une vraie machine), épaisseur ~1/9 du diamètre
        const float CoinD = 0.062f, CoinT = 0.0068f;
        static PhysicsMaterial coinPhys;
        static Mesh coinHull;

        static PhysicsMaterial EnsurePhysicsMaterial(string name, float staticF, float dynamicF, float bounce, PhysicsMaterialCombine combine = PhysicsMaterialCombine.Average)
        {
            string path = MatDir + "/" + name + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (m == null)
            {
                m = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(m, path);
            }
            m.staticFriction = staticF; m.dynamicFriction = dynamicF; m.bounciness = bounce;
            m.frictionCombine = combine;
            m.bounceCombine = PhysicsMaterialCombine.Average;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Cylindre à 12 pans (rayon 0,5, hauteur 2 : mêmes proportions que la primitive Unity) pour le collider des pièces.</summary>
        static Mesh EnsureCoinHull()
        {
            ProjectSetup.EnsureFolder("Assets/_Project/Meshes");
            string path = "Assets/_Project/Meshes/CoinCollider12.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            const int n = 12;
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 1f, Mathf.Sin(a) * 0.5f));
                v.Add(new Vector3(Mathf.Cos(a) * 0.5f, -1f, Mathf.Sin(a) * 0.5f));
            }
            v.Add(new Vector3(0, 1, 0)); v.Add(new Vector3(0, -1, 0));
            int top = 2 * n, bottom = 2 * n + 1;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                int a0 = 2 * i, a1 = 2 * i + 1, b0 = 2 * j, b1 = 2 * j + 1;
                t.AddRange(new[] { a0, b0, a1, b0, b1, a1 });
                t.AddRange(new[] { top, b0, a0 });
                t.AddRange(new[] { bottom, a1, b1 });
            }
            var mesh = new Mesh { name = "CoinCollider12" };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void B(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider)
            => RoomBuilder.Box(name, parent, pos, size, mat, collider, true);

        static Material ClawGlass()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Claw_Glass.mat");
            return m != null ? m : RoomBuilder.Mat("Pusher_Glass", new Color(0.6f, 0.85f, 1f), 0.9f);
        }

        static MachineData EnsureMachineData()
        {
            ProjectSetup.EnsureFolder(DataDir);
            string path = DataDir + "/Machine_CoinPusher.asset";
            var d = AssetDatabase.LoadAssetAtPath<MachineData>(path);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<MachineData>();
                AssetDatabase.CreateAsset(d, path);
            }
            d.id = "coin_pusher";
            d.displayName = "Coin Pusher";
            d.category = MachineCategory.CoinPusher;
            d.costPerPlay = 1;
            d.purchasePrice = 800;
            d.unlockLevel = 1;
            d.footprintSize = new Vector2(1.5f, 2.8f);
            d.footprintCenterZ = -0.45f;
            EditorUtility.SetDirty(d);
            return d;
        }

        static RewardData EnsureReward(string id, string name, Rarity rarity, int tickets, long xp)
        {
            string path = DataDir + "/Rewards/Reward_" + id + ".asset";
            var r = AssetDatabase.LoadAssetAtPath<RewardData>(path);
            if (r == null)
            {
                r = ScriptableObject.CreateInstance<RewardData>();
                AssetDatabase.CreateAsset(r, path);
            }
            r.id = id;
            r.displayName = name;
            r.rarity = rarity;
            r.ticketValue = tickets;
            r.experienceValue = xp;
            EditorUtility.SetDirty(r);
            return r;
        }
    }
}
#endif
