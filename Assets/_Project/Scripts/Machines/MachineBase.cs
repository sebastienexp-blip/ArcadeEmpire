using System;
using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Save;
using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Classe de base de toutes les machines : entrée/sortie du joueur, paiement d'une partie,
    /// attribution des récompenses. Les machines concrètes (pince, coin pusher…) en héritent
    /// et implémentent uniquement leur mécanique.
    /// </summary>
    public abstract class MachineBase : MonoBehaviour
    {
        [SerializeField] protected MachineData data;

        [Tooltip("Point où la caméra rapprochée se place (Cinemachine) pendant l'utilisation.")]
        [SerializeField] protected Transform cameraAnchor;

        /// <summary>Toutes les machines actives de la scène (utilisé par l'interaction du joueur).</summary>
        public static readonly List<MachineBase> All = new List<MachineBase>();

        /// <summary>Déclenché pour toute récompense gagnée sur n'importe quelle machine (HUD, succès, missions).</summary>
        public static event Action<MachineBase, RewardData> AnyRewardWon;

        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public MachineData Data => data;
        public Transform CameraAnchor => cameraAnchor;
        /// <summary>Message d'état affiché par le HUD pendant l'utilisation.</summary>
        public string StatusText { get; protected set; } = "";

        /// <summary>Rappel des commandes affiché par le HUD pendant l'utilisation.</summary>
        public virtual string ControlsHint => "";

        public bool IsInUse { get; private set; }
        public bool IsPlaying { get; private set; }

        public event Action<MachineBase> OnEntered;
        public event Action<MachineBase> OnExited;
        public event Action<MachineBase, RewardData> OnRewardWon;

        /// <summary>Le joueur s'installe devant la machine.</summary>
        public bool Enter()
        {
            if (IsInUse) return false;
            IsInUse = true;
            OnPlayerEntered();
            OnEntered?.Invoke(this);
            return true;
        }

        /// <summary>Le joueur quitte la machine.</summary>
        public void Exit()
        {
            if (!IsInUse) return;
            if (IsPlaying) EndPlay();
            IsInUse = false;
            OnPlayerExited();
            OnExited?.Invoke(this);
        }

        /// <summary>Débite le coût d'une partie et lance la mécanique. Retourne false si fonds insuffisants.</summary>
        public bool TryStartPlay()
        {
            if (!IsInUse || IsPlaying || data == null || CurrencyManager.Instance == null) return false;
            if (!CurrencyManager.Instance.TrySpend(CurrencyType.Coins, data.costPerPlay)) return false;
            IsPlaying = true;
            OnPlayStarted();
            return true;
        }

        /// <summary>À appeler par la mécanique quand la partie est terminée.</summary>
        protected void EndPlay()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            OnPlayEnded();
        }

        /// <summary>
        /// À appeler quand une récompense est gagnée : crédite tickets et XP.
        /// (L'ajout à l'inventaire sera branché en Phase 6 via l'événement OnRewardWon.)
        /// </summary>
        protected void GrantReward(RewardData reward)
        {
            if (reward == null || CurrencyManager.Instance == null) return;
            CurrencyManager.Instance.Add(CurrencyType.Tickets, reward.ticketValue);
            CurrencyManager.Instance.Add(CurrencyType.Experience, reward.experienceValue);
            AddToInventory(reward);
            OnRewardWon?.Invoke(this, reward);
            AnyRewardWon?.Invoke(this, reward);
        }

        /// <summary>Ajoute la récompense à l'inventaire persistant (sauvegardé par SaveManager).</summary>
        private static void AddToInventory(RewardData reward)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.Add(reward, 1);
                return;
            }
            if (SaveManager.Instance == null) return;
            var inv = SaveManager.Instance.Data.inventory;
            var entry = inv.Find(e => e.rewardId == reward.id);
            if (entry == null) inv.Add(new InventoryEntry { rewardId = reward.id, count = 1 });
            else entry.count++;
        }

        protected virtual void OnPlayerEntered() { }
        protected virtual void OnPlayerExited() { }
        protected abstract void OnPlayStarted();
        protected virtual void OnPlayEnded() { }
    }
}
