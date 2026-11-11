using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using UnityEngine;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Interface minimale (IMGUI) : monnaies, invite d'interaction, état de la machine, notification de gain.
    /// Provisoire : remplacée par l'interface Canvas/TextMeshPro en Phase 6-7.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private MachineInteractor interactor;
        private string toast = "";
        private float toastUntil;
        private GUIStyle label, big, box;

        private void OnEnable() => MachineBase.AnyRewardWon += OnReward;
        private void OnDisable() => MachineBase.AnyRewardWon -= OnReward;

        private void Start() => interactor = FindFirstObjectByType<MachineInteractor>();

        private void OnReward(MachineBase machine, RewardData reward)
        {
            toast = $"★ {reward.displayName} — {ClawMachine.RarityName(reward.rarity)} ★";
            toastUntil = Time.time + 3.5f;
        }

        private void OnGUI()
        {
            float scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            Styles();

            // Monnaies
            var cm = CurrencyManager.Instance;
            string coins = cm != null ? cm.Get(CurrencyType.Coins).ToString() : "—";
            string tickets = cm != null ? cm.Get(CurrencyType.Tickets).ToString() : "—";
            string xp = cm != null ? cm.Get(CurrencyType.Experience).ToString() : "—";
            GUI.Box(new Rect(12, 12, 330, 40), GUIContent.none, box);
            GUI.Label(new Rect(22, 16, 320, 32), $"Pièces {coins}    Tickets {tickets}    XP {xp}", label);

            if (interactor == null) return;

            // État de la machine en cours d'utilisation
            if (interactor.Active is ClawMachine claw)
            {
                GUI.Box(new Rect(w * 0.5f - 420, 640, 840, 54), GUIContent.none, box);
                GUI.Label(new Rect(w * 0.5f - 410, 646, 820, 44), claw.StatusText, big);
                GUI.Label(new Rect(w * 0.5f - 410, 600, 820, 36), "ZQSD / WASD / flèches : déplacer la pince      ESPACE : valider", label);
            }
            else if (interactor.Nearest != null && interactor.Nearest.Data != null)
            {
                var d = interactor.Nearest.Data;
                GUI.Box(new Rect(w * 0.5f - 300, 640, 600, 54), GUIContent.none, box);
                GUI.Label(new Rect(w * 0.5f - 290, 646, 580, 44), $"E : jouer — {d.displayName} ({d.costPerPlay} pièces)", big);
            }

            // Notification de gain
            if (Time.time < toastUntil)
                GUI.Label(new Rect(w * 0.5f - 400, 110, 800, 60), toast, big);
        }

        private void Styles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            label.normal.textColor = new Color(0.85f, 0.95f, 1f);
            big = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = new Color(1f, 0.9f, 0.3f);
            box = new GUIStyle(GUI.skin.box);
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.04f, 0.02f, 0.1f, 0.75f));
            tex.Apply();
            box.normal.background = tex;
        }
    }
}
