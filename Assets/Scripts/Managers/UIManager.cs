using Managers;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Central UI manager singleton. Holds references to various UI components
    /// (for example: PlayerProgressUI, HUD) so other systems can access them easily.
    /// Attach this to a persistent GameObject in the scene (or let it persist via DontDestroyOnLoad).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private HUD playerHUD;
        [SerializeField] private GameOverPanel gameOverPanel;
        [SerializeField] private GameMenuPanel gameMenuPanel;
        [SerializeField] private CountdownUI countdownUI;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public HUD PlayerHUD => playerHUD;
        public GameOverPanel GameOverPanel => gameOverPanel;
        public GameMenuPanel GameMenuPanel => gameMenuPanel;
        public CountdownUI CountdownUI => countdownUI;

        /// <summary>
        /// Force a refresh on all known UI components.
        /// </summary>
        public void ForceRefreshAll()
        {
            playerHUD?.ForceRefresh();
        }
    }
}
