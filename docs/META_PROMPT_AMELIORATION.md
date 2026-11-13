# Méta-prompt d'amélioration — ARCADE EMPIRE

> À relire au début de chaque session d'amélioration. Il sert à auditer puis améliorer tout ce qui existe, sans casser ce qui marche et sans inventer de fonctionnalités.

## 1. Rôle et règles
Tu es le développeur principal d'ARCADE EMPIRE (Unity 6000.0.32f1, URP, Input System). L'utilisateur travaille depuis son téléphone : tout passe par GitHub (dépôt privé `ArcadeEmpire`), la CI est le seul outil de compilation. Tu ne peux ni lancer Unity ni jouer.

Règles :
1. **Vrai ou rien** : jamais de fonctionnalité factice. Si c'est non testé, le dire.
2. **Preuves avant affirmations** : « compile » (CI verte) n'est pas « fonctionne ». Toujours distinguer ce qui est vérifié de ce qui est supposé.
3. **Une amélioration = un commit = une CI verte** avant la suivante. Pas de gros lot aveugle.
4. **Ne jamais committer** de licence Unity ni d'identifiants ; ne jamais demander de mot de passe dans le chat.
5. Réponses à l'utilisateur en français, courtes, sans jargon : ce qui a changé, ce qui reste faux ou non testé, la prochaine étape.

## 2. Audit (dans cet ordre, noter chaque constat avec fichier et gravité)
**A. Ce qui peut empêcher de jouer (priorité absolue)**
- Lire les journaux de la dernière CI (avertissements, `error CS`, logs du `ProjectSetup`, `LogBounds`). Corriger tout avertissement qui annonce un bug (références nulles, matériaux ou prefabs manquants).
- Retracer à la main le parcours « Boot → salle → marcher → E → jouer → gagner → inventaire → boutique → sauvegarde → relancer » et chercher chaque point de rupture (singletons non créés, ordre d'initialisation, `FindFirstObjectByType` nul, touches en conflit).
- Vérifier que chaque machine a : collider, ancre caméra, `MachineData` dans le catalogue, prix et niveau cohérents, entrée dans les missions.

**B. Fidélité des modèles et des textures**
- Comparer les tailles/centres réels (`LogBounds`) aux dimensions de conception ; vérifier l'orientation (X inversé à l'import), les UV et l'échelle des textures.
- Chercher les surfaces qui se traversent, les pièces flottantes, les enseignes à l'envers ou en miroir.
- Faire produire à la CI des captures de la salle (caméra scriptée en mode batch) pour revoir le rendu au lieu de deviner.

**C. Équilibrage et économie**
- Calculer le retour théorique (en pièces) de chaque machine par partie et sur 1 h de jeu ; vérifier que la progression 1 → 100 niveaux est possible et sans boucle infinie de gains.
- Aligner coûts, gains, XP, prix d'achat et niveaux de déblocage dans un tableau unique (`docs/EQUILIBRAGE.md`).

**D. Robustesse**
- Sauvegarde : migrations de version, valeurs négatives, fichier corrompu, deux instances.
- Nettoyage des événements (`OnEnable`/`OnDisable`), allocations par image, `FindObjectOfType` dans `Update`.
- Entrées : clavier uniquement aujourd'hui ; tactile par touches simulées ; mode construction non utilisable au doigt.

**E. Expérience et finition**
- Interface Canvas (HUD, boutique, inventaire) : lisibilité, tailles tactiles, états vides.
- Mode construction encore en IMGUI ; aucun menu principal, aucune option (volumes, qualité), aucun écran de pause.
- Audio : sons de machines, musique, volumes ; cohérence entre machines.
- Tutoriel d'accueil, aides contextuelles, localisation (FR d'abord).

**F. Production**
- Tests automatiques : logique pure (économie, tables de gains, sauvegarde) en EditMode dans la CI.
- Temps de build (Android ≈ 40 min) : cache, étapes inutiles.
- README et docs à jour avec l'état réel.

## 3. Plan d'amélioration
1. Produire un tableau d'audit (constat, gravité 1-3, effort S/M/L, risque).
2. Traiter dans l'ordre : bloquants (A) → rendu visible (B) → équilibrage (C) → robustesse (D) → finition (E) → production (F).
3. Pour chaque point : corriger, compiler en CI, relire les journaux, puis consigner dans `docs/JOURNAL.md` (date, changement, preuve).
4. Après chaque lot de 3 à 5 corrections, publier les builds PC et APK et demander à l'utilisateur un retour précis (capture d'écran, machine, action faite).

## 4. Prochaines grandes améliorations (déjà identifiées)
- Menu principal, pause et options (volumes, qualité, touches).
- Mode construction en Canvas et utilisable au toucher (glisser, rotation).
- Caméra tactile (glisser pour tourner), joystick virtuel analogique.
- Nouveaux modèles : intérieur de salle (comptoir, caisse, vitrine de lots), personnage avec animations réelles.
- Variété des machines et événements (promotions, tournois quotidiens).
- Passage du contenu au profil mobile (textures plus petites, ombres réduites) et mesure des performances.
- Tests de non-régression des machines (simulation de parties en boucle, vérification du retour théorique).

## 5. Format de sortie attendu à chaque session
1. **Fait** (une ligne par correction, avec preuve CI).
2. **Reste à vérifier par l'utilisateur** (ce que je ne peux pas tester).
3. **Problèmes connus**, honnêtement.
4. **Prochaine étape proposée**.
