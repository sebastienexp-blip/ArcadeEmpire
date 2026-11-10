using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcadeEmpire.Core
{
    /// <summary>Scène Boot : une fois les managers initialisés, charge la salle d'arcade.</summary>
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] private string firstScene = "ArcadeRoom";

        private void Start()
        {
            SceneManager.LoadScene(firstScene);
        }
    }
}
