# ARCADE EMPIRE

Jeu Unity 3D de gestion de salle d'arcade (machines à pinces, coin pusher, mini-jeux, économie, progression). Cibles : **PC** (graphismes complets) et **Android** (version allégée).

## État d'avancement

- [x] **Phase 1** : architecture, économie de base, sauvegarde, données, classe de base des machines, qualité PC/Mobile
- [x] Phase 2 : première salle (primitives provisoires, générée par `RoomBuilder` ; visite à la caméra libre)
- [x] Phase 3 : personnage provisoire (capsule), déplacements ZQSD/WASD + Maj, caméra 3e personne (clic droit = orbite, molette = zoom)
- [x] Phase 4 : machine à pinces jouable (E : s'installer, ESPACE : insérer la pièce puis descendre, ZQSD : joystick, Échap : quitter). Refonte réaliste : portique (pont + chariot) mobile, joystick avec inertie et câble qui se balance, prise dépendant du centrage, du poids et de la rareté du lot (puis usée par le transport), pièce animée dans le monnayeur, afficheur numérique, LED, sons dédiés
- [x] Phase 5 : coin pusher (visée Q/D, ESPACE : lâcher une pièce ; pièces et jetons poussés = gains)
- [x] Phase 6 : économie et inventaire (touche I : inventaire, collection, revente, sauvegarde manuelle)
- [x] Phase 7 : progression (niveaux 1 à 100, XP), missions du jour, boutique en tickets, amélioration de la pince (touche B)
- [x] Phase 8 : construction (touche C : vue du dessus, acheter/placer/déplacer/pivoter R/revendre Suppr, grille 0,5 m), décors, 5 ambiances de salle
- [x] Phase 9 (1re série) : roue de la fortune, machine à sous, testeur de force, basket, réflexes (achat dans le mode construction, touche C)
- [x] Phase 9 (2e série) : air hockey (contre IA), stand de tir, pêche aux poissons
- [x] Phase 9 (3e série) : course d'arcade, stacker rétro
- [x] Refonte du coin pusher : pièces physiques à collider léger, matériaux physiques, plaque en FixedUpdate, insertion animée, bac avec gains à récupérer (R), afficheur numérique, sons liés aux chocs, LED animées, FBX détaillé
- [x] Coin pusher v4 (réaliste) : vraie mécanique à deux étages (plaque à course de 22 cm qui garde un tapis de médailles, capot racleur, plateau du bas plein), frottements laiton/laiton et métal poli, 4 fentes d'insertion, 5 voies chance (x3), roue bonus à 8 effets, 2 balles jackpot avec ascenseur, jackpot progressif, billets en plastique, lots, médailles dorées/argentées, carrosserie laquée
- [x] Machine à pince v2 (réaliste) : 12 peluches modélisées d'une seule surface lisse (champ de distance + marching cubes, `Tools/modelgen/make_plush.py`) avec yeux brillants, broderies, étiquette et texture fourrure ; 37 lots de 19 sortes (peluches, robots, capsules, figurine en boîte, figurines) en vrac, tas pré-tassé physiquement ; prise physique (liaison qui casse si la prise est mauvaise, pince forte à la prise et affaiblie en haut), descente jusqu'au contact, branches recourbées qui s'arrêtent sur le lot, garde de trappe en plexiglas, carrosserie laquée et LED blanches ; ~25 % de réussite mesurés au labo avec une visée humaine
- [x] Labo automatique (`.github/workflows/lab.yml`, `PusherLab.cs`) : le jeu PC joue seul sur un serveur Windows (`-pusherlab` ou `-clawlab`), mesure la physique (débit, couches, blocages, taux de prise et de gain) et prend des captures (release « labo »)
- [x] Grande salle complète (22 x 16 m) : 4 pinces à thème (Classique, K-pop avec idoles inventées NOVA ZERO / LUNA KISS, Confiseries, Toy Box avec monstres doudous, squishies, porte-clés et consoles génériques), 3 coin pushers (Classique, Océan, Royal), toutes disponibles dès le départ
- [x] Vitrine des lots (comptoir) : lots en tickets, meubles, gros lots sous cadenas (tickets + clés dorées) ; comptoir de rachat (revente, doublons) ; clés dorées gagnées avec les balles du coin pusher, les lots épiques et au hasard
- [x] Maison du joueur (téléporteur au fond de la salle) : salon, cuisine, chambre, salle de jeux ; 20 meubles obtenus comme lots (vitrine ou carton surprise à 6 % par gain) qui s'installent seuls ; étagères qui exposent les lots gagnés
- [x] Personnage refait (surfaces lisses, `make_player.py`) ; interface refaite : joystick analogique, boutons d'action ronds contextuels, menus plein écran en cartes avec vignettes 3D des lots
- [x] Sons réalistes générés avec ElevenLabs (`Assets/_Project/Resources/Audio`, chargés par `SoundBank`) : pièces, pince, coin pusher, roue, jackpot, interface, achats, clés, niveau, téléporteur, pas, ambiance de salle, musique lofi de la maison
- [ ] Phase 10 (en cours) : sauvegarde validée à la lecture, musique d'ambiance générée (M musique on/off, - et + volume), optimisations, 10 succès (onglet Missions), HUD, boutique et inventaire en Canvas (UGUI) ; seul le mode construction reste en IMGUI. Mouvement avec inertie, caméra amortie (recentrage, FOV, pincement), animation de marche procédurale plus fidèle. Commandes tactiles (Android) : croix + boutons qui simulent le clavier (TouchControls)
- [ ] Phase 10 : optimisation, tests, finition

## Tout se gère depuis GitHub (aucun PC requis)

Le workflow `.github/workflows/build.yml` (onglet **Actions**) compile le jeu dans le cloud avec **Unity 6000.0.32f1**, la même version que Korvalune. La licence est lue dans le secret GitHub `UNITY_LICENSE` (Settings > Secrets and variables > Actions : y coller le contenu du fichier `Unity_lic.ulf`).

- À chaque modification de `Assets/`, `Packages/` ou `ProjectSettings/`, ou via *Run workflow* :
  - ~~APK Android~~ : mis de côté, à réactiver en fin de projet (bloc commenté dans `build.yml`)
  - **Jeu Windows** (profil graphique PC) : release `pc-derniere` (zip)
- La CI crée automatiquement les deux assets URP (PC / Mobile) et la scène `Boot` (script `ProjectSetup`).
- Les rapports de build sont dans les artefacts de chaque exécution (`ci-out/<plateforme>/build.md`).
- Depuis un téléphone : onglet *Releases* du dépôt, télécharger l'APK.

## Ouvrir le projet dans Unity (optionnel, sur PC/Mac)

Unity Hub > *Add project from disk*, éditeur **6000.0.32f1**. Menu **ArcadeEmpire > Configurer le projet** pour régénérer URP et la scène Boot.

## Profils graphiques

| | PC | Mobile |
|---|---|---|
| HDR | oui | non |
| MSAA | 4x | désactivé |
| Ombres | 50 m, 4 cascades | 15 m, 1 cascade |
| Échelle de rendu | 100 % | 80 % |

`GraphicsQualityManager` choisit le profil selon la plateforme (réglable via `SaveData.graphicsQualityOverride` : -1 auto, 0 PC, 1 Mobile).

## Structure

```
Assets/_Project/
  Scripts/
    Core/       GraphicsQualityManager
    Editor/     ProjectSetup, Builder (CI)
    Economy/    CurrencyManager, InventoryManager, ProgressionManager, MissionManager, ShopManager, UpgradeManager
    Save/       SaveData, SaveSystem, SaveManager
    Data/       RewardData, RewardCatalog, MachineData (ScriptableObjects)
    Machines/   MachineBase, ClawMachine, CoinPusherMachine, PrizeItem, ChuteZone, PusherCoin, PusherCollector (+ Minigames à venir)
    Building/   BuildingManager, BuildMode, PlacedObject, RoomTheme(+Manager/Target)
    Machines/Games/  ArcadeGameMachine (base), WheelMachine, SlotMachine, HammerMachine, BasketballMachine, ReflexMachine
    UI/         GameHud, InventoryPanel, ShopPanel (provisoires, IMGUI)
  (Scenes, Prefabs, Materials, Audio, Art : créés au fil des phases)
```

## Modèles 3D et textures (générés par code)

- `Tools/modelgen/build_all.py` : 22 modèles FBX avec UV (peluches, robot, capsule, chat doré, décors, coque de la pince et du coin pusher, tête et doigts de pince, joystick, bouton, plateau, pièces, personnage en 5 pièces articulées) vers `Assets/_Project/Art/Models`.
- `Tools/modelgen/make_textures.py` : textures procédurales tileables (albedo + normales) vers `Assets/_Project/Art/Textures`.
- `preview.py` / `preview2.py` : aperçus de contrôle (rendu logiciel, pas Unity).
- Côté Unity : `ModelLib` (palette de matériaux URP par nom de slot), `TextureImportSettings` (normales automatiques), `PlayerVisualAnimator` (marche procédurale).
- Les primitives restent comme volumes physiques (collisions) sous la coque détaillée.

## Principes

- Un système = un script, aucune logique centrale géante.
- Les machines héritent de `MachineBase` ; les monnaies passent uniquement par `CurrencyManager`.
- Sauvegarde JSON dans `Application.persistentDataPath`, avec copie de secours et sauvegarde automatique (toutes les 60 s, à la mise en pause et à la fermeture).
- Aucune dépense d'argent réel requise pour progresser.
