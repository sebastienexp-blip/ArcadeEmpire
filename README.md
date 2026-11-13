# ARCADE EMPIRE

Jeu Unity 3D de gestion de salle d'arcade (machines à pinces, coin pusher, mini-jeux, économie, progression). Cibles : **PC** (graphismes complets) et **Android** (version allégée).

## État d'avancement

- [x] **Phase 1** : architecture, économie de base, sauvegarde, données, classe de base des machines, qualité PC/Mobile
- [x] Phase 2 : première salle (primitives provisoires, générée par `RoomBuilder` ; visite à la caméra libre)
- [x] Phase 3 : personnage provisoire (capsule), déplacements ZQSD/WASD + Maj, caméra 3e personne (clic droit = orbite, molette = zoom)
- [x] Phase 4 : machine à pinces jouable (E : s'installer, ESPACE : payer / descendre, ZQSD : déplacer la pince, Échap : quitter)
- [x] Phase 5 : coin pusher (visée Q/D, ESPACE : lâcher une pièce ; pièces et jetons poussés = gains)
- [x] Phase 6 : économie et inventaire (touche I : inventaire, collection, revente, sauvegarde manuelle)
- [x] Phase 7 : progression (niveaux 1 à 100, XP), missions du jour, boutique en tickets, amélioration de la pince (touche B)
- [x] Phase 8 : construction (touche C : vue du dessus, acheter/placer/déplacer/pivoter R/revendre Suppr, grille 0,5 m), décors, 5 ambiances de salle
- [x] Phase 9 (1re série) : roue de la fortune, machine à sous, testeur de force, basket, réflexes (achat dans le mode construction, touche C)
- [x] Phase 9 (2e série) : air hockey (contre IA), stand de tir, pêche aux poissons
- [x] Phase 9 (3e série) : course d'arcade, stacker rétro
- [ ] Phase 10 (en cours) : sauvegarde validée à la lecture, musique d'ambiance générée (M musique on/off, - et + volume)
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
