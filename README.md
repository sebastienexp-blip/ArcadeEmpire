# ARCADE EMPIRE

Jeu Unity 3D de gestion de salle d'arcade (machines à pinces, coin pusher, mini-jeux, économie, progression). Cibles : **PC** (graphismes complets) et **Android** (version allégée).

## État d'avancement

- [x] **Phase 1** : architecture, économie de base, sauvegarde, données, classe de base des machines, qualité PC/Mobile
- [ ] Phase 2 : environnement 3D et première salle
- [ ] Phase 3 : personnage et déplacements
- [ ] Phase 4 : machine à pinces
- [ ] Phase 5 : coin pusher
- [ ] Phase 6 : économie et inventaire
- [ ] Phase 7 : progression et boutique
- [ ] Phase 8 : construction et personnalisation
- [ ] Phase 9 : autres machines et mini-jeux
- [ ] Phase 10 : optimisation, tests, finition

## Ouvrir le projet

1. Installer Unity Hub, puis l'éditeur **2022.3 LTS** (module Android Build Support inclus pour l'APK).
2. Cloner ce dépôt, puis dans Unity Hub : *Add > Add project from disk*.
3. À la première ouverture, Unity télécharge les packages (URP, Input System, Cinemachine, TextMeshPro) et génère les fichiers `.meta`. Si une autre version 2022.3.x est installée, accepter la mise à niveau proposée.

> `ProjectSettings/` ne contient pour l'instant que `ProjectVersion.txt` : Unity génère le reste à l'ouverture. Vérifier ensuite que le pipeline URP est configuré (étape 2 ci-dessous).

## Configuration initiale (une seule fois, dans l'éditeur)

### 1. Scène de démarrage
Créer `Assets/_Project/Scenes/Boot.unity`, avec un GameObject vide **Managers** portant les composants :
`CurrencyManager`, `SaveManager`, `GraphicsQualityManager`.
Ajouter la scène dans *File > Build Settings*.

### 2. URP : deux profils de qualité
1. *Assets > Create > Rendering > URP Asset (with Universal Renderer)* deux fois : `URP_PC` et `URP_Mobile`.
2. `URP_PC` : HDR activé, Anti-aliasing MSAA 4x, ombres temps réel (distance 50 m, cascades 4), Soft Shadows, Render Scale 1.0.
3. `URP_Mobile` : HDR désactivé, MSAA 2x ou désactivé, ombres (distance 15 m, cascades 1), Render Scale 0.8, SRP Batcher activé.
4. *Edit > Project Settings > Quality* : créer deux niveaux nommés exactement **PC** et **Mobile**, et assigner à chacun l'URP Asset correspondant.
5. Le script `GraphicsQualityManager` sélectionne le bon niveau au lancement selon la plateforme.

### 3. Créer les données de test
- *Create > ArcadeEmpire > Reward* : créer 3 récompenses (commune, rare, épique) avec un `id` unique.
- *Create > ArcadeEmpire > Machine* : créer la fiche de la machine à pinces (`costPerPlay` = 5).

## Structure

```
Assets/_Project/
  Scripts/
    Core/       GraphicsQualityManager
    Economy/    CurrencyManager
    Save/       SaveData, SaveSystem, SaveManager
    Data/       RewardData, MachineData (ScriptableObjects)
    Machines/   MachineBase (+ Claw, CoinPusher, Minigames à venir)
  (Scenes, Prefabs, Materials, Audio, Art : créés au fil des phases)
```

## Principes

- Un système = un script, aucune logique centrale géante.
- Les machines héritent de `MachineBase` ; les monnaies passent uniquement par `CurrencyManager`.
- Sauvegarde JSON dans `Application.persistentDataPath`, avec copie de secours et sauvegarde automatique (toutes les 60 s, à la mise en pause et à la fermeture).
- Aucune dépense d'argent réel requise pour progresser.
