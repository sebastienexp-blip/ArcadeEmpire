using System;
using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
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

        public MachineData Data => data;
        public Transform CameraAnchor => cameraAnchor;
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
            OnRewardWon?.Invoke(this, reward);
        }

        protected virtual void OnPlayerEntered() { }
        protected virtual void OnPlayerExited() { }
        protected abstract void OnPlayStarted();
        protected virtual void OnPlayEnded() { }
    }
}
