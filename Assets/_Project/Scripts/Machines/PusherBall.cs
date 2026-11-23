using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>Balle jackpot du coin pusher : quand elle tombe dans le bac, elle remonte par l'ascenseur, lance la roue (chances de jackpot)
    /// puis retombe sur la plaque — elle reste en jeu, comme sur les grandes machines japonaises.</summary>
    public class PusherBall : MonoBehaviour
    {
        public bool InLift { get; set; }
        public Rigidbody Body { get; private set; }
        public Collider Col { get; private set; }

        private void Awake() { Body = GetComponent<Rigidbody>(); Col = GetComponent<Collider>(); }
    }
}
