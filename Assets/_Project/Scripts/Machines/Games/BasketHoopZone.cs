using System.Collections.Generic;
using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>Zone d'anneau : prévient la machine quand un ballon la traverse.</summary>
    public class BasketHoopZone : MonoBehaviour
    {
        [SerializeField] private BasketballMachine machine;
        public void Configure(BasketballMachine m) => machine = m;

        private void OnTriggerEnter(Collider other)
        {
            if (machine != null && other.attachedRigidbody != null && other.GetComponent<BasketBall>() != null)
                machine.RegisterBasket(other.attachedRigidbody);
        }
    }
}
