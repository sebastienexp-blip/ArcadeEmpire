using ArcadeEmpire.Economy;
using UnityEngine;

namespace ArcadeEmpire.Core
{
    /// <summary>
    /// Sons globaux du jeu (ElevenLabs) : ambiance de la salle d'arcade en boucle, clé dorée gagnée, passage de niveau,
    /// téléporteur. L'ambiance s'efface à la maison (la musique y passe en lofi, voir MusicPlayer).
    /// </summary>
    public class GameSounds : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<GameSounds>() != null) return;
            var go = new GameObject("GameSounds");
            DontDestroyOnLoad(go);
            go.AddComponent<GameSounds>();
        }

        /// <summary>Vrai quand le joueur est dans sa maison (la maison est construite à x = 60).</summary>
        public static bool AtHome
        {
            get
            {
                var p = FindPlayer();
                return p != null && p.position.x > 40f;
            }
        }

        private static Transform player;
        private static Transform FindPlayer()
        {
            if (player == null) { var pc = FindFirstObjectByType<ArcadeEmpire.Player.PlayerController>(); if (pc != null) player = pc.transform; }
            return player;
        }

        private AudioSource ambience;
        private CurrencyManager cm;
        private ProgressionManager pm;
        private long lastKeys = -1;

        private void Start()
        {
            var clip = SoundBank.Get("ambience");
            if (clip != null)
            {
                ambience = gameObject.AddComponent<AudioSource>();
                ambience.clip = clip; ambience.loop = true; ambience.spatialBlend = 0f; ambience.playOnAwake = false; ambience.volume = 0f;
                ambience.Play();
            }
            ArcadeEmpire.Player.PlayerController.Teleported += OnTeleport;
        }

        private void OnDestroy()
        {
            ArcadeEmpire.Player.PlayerController.Teleported -= OnTeleport;
            if (cm != null) cm.OnBalanceChanged -= OnBalance;
            if (pm != null) pm.OnLevelUp -= OnLevel;
        }

        private void OnTeleport() => SoundBank.Play2D("teleport", 0.75f);
        private void OnLevel(int level, long coins, int tokens) => SoundBank.Play2D("levelup", 0.8f);

        private void OnBalance(CurrencyType type, long balance)
        {
            if (type != CurrencyType.Keys) return;
            if (lastKeys >= 0 && balance > lastKeys) SoundBank.Play2D("key", 0.75f);
            lastKeys = balance;
        }

        private void Update()
        {
            if (cm == null && CurrencyManager.Instance != null) { cm = CurrencyManager.Instance; cm.OnBalanceChanged += OnBalance; lastKeys = cm.Get(CurrencyType.Keys); }
            if (pm == null && ProgressionManager.Instance != null) { pm = ProgressionManager.Instance; pm.OnLevelUp += OnLevel; }
            if (ambience != null)
            {
                float target = (AtHome || FindPlayer() == null ? 0f : 0.32f) * GameAudio.EffectiveSfx;
                ambience.volume = Mathf.MoveTowards(ambience.volume, target, Time.unscaledDeltaTime * 0.5f);
            }
        }
    }
}
