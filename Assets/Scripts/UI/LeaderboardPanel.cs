using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI
{
    /// <summary>
    /// Leaderboard panel showing personal stats across 3 categories.
    /// Currently displays local records; designed to plug into Firebase for global leaderboards later.
    /// </summary>
    public class LeaderboardPanel : MonoBehaviour
    {
        // ── Tab Buttons ────────────────────────────────────────────────────────
        [Header("Tab Buttons")]
        [SerializeField] private Button highScoreTab;
        [SerializeField] private Button longestRunTab;
        [SerializeField] private Button totalCoinsTab;

        [Header("Tab Visual State")]
        [SerializeField] private Color activeTabColor   = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        // ── Content Area ───────────────────────────────────────────────────────
        [Header("Content")]
        [SerializeField] private TMP_Text categoryTitle;
        [SerializeField] private TMP_Text playerValueText;
        [SerializeField] private TMP_Text playerRankText;
        [SerializeField] private TMP_Text subtitleText;

        // ── Stat Rows (optional: show all 3 at once as a summary) ──────────────
        [Header("Summary Rows (optional)")]
        [SerializeField] private GameObject summaryContainer;
        [SerializeField] private TMP_Text summaryHighScoreValue;
        [SerializeField] private TMP_Text summaryLongestRunValue;
        [SerializeField] private TMP_Text summaryTotalCoinsValue;

        // ── Close ──────────────────────────────────────────────────────────────
        [Header("Close")]
        [SerializeField] private Button closeButton;

        // ── State ──────────────────────────────────────────────────────────────
        private LeaderboardCategory _activeCategory = LeaderboardCategory.HighScore;

        public enum LeaderboardCategory
        {
            HighScore,
            LongestRun,
            TotalCoins
        }

        private void Start()
        {
            if (highScoreTab)  highScoreTab.onClick.AddListener(() => ShowCategory(LeaderboardCategory.HighScore));
            if (longestRunTab) longestRunTab.onClick.AddListener(() => ShowCategory(LeaderboardCategory.LongestRun));
            if (totalCoinsTab) totalCoinsTab.onClick.AddListener(() => ShowCategory(LeaderboardCategory.TotalCoins));
            if (closeButton)   closeButton.onClick.AddListener(Hide);

            gameObject.SetActive(false);
        }

        public void Show()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuOpen);
            gameObject.SetActive(true);
            RefreshSummary();
            ShowCategory(_activeCategory);
        }

        public void Hide()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuClose);
            gameObject.SetActive(false);
        }

        private void ShowCategory(LeaderboardCategory category)
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
            _activeCategory = category;

            UpdateTabVisuals(category);

            switch (category)
            {
                case LeaderboardCategory.HighScore:
                    int highScore = PlayerPrefsManager.GetInt(PlayerPrefsKeys.HighestScore, 0);
                    SetContent("BEST SCORE", NumberFormatter.Format(highScore), "Your highest score in a single run");
                    break;

                case LeaderboardCategory.LongestRun:
                    float highDist = PlayerPrefsManager.GetFloat(PlayerPrefsKeys.HighestDistance, 0f);
                    SetContent("LONGEST RUN", NumberFormatter.FormatDistance(highDist), "Your longest distance in a single run");
                    break;

                case LeaderboardCategory.TotalCoins:
                    int totalCoins = PlayerPrefsManager.GetInt(PlayerPrefsKeys.TotalCoinsCollected, 0);
                    SetContent("TOTAL COINS", NumberFormatter.Format(totalCoins), "Total coins collected across all runs");
                    break;
            }
        }

        private void SetContent(string title, string value, string subtitle)
        {
            if (categoryTitle)   categoryTitle.text = title;
            if (playerValueText) playerValueText.text = value;
            if (subtitleText)    subtitleText.text = subtitle;

            // Rank placeholder — will be populated when Firebase leaderboard is added
            if (playerRankText) playerRankText.text = "-";
        }

        private void UpdateTabVisuals(LeaderboardCategory active)
        {
            SetTabColor(highScoreTab,  active == LeaderboardCategory.HighScore);
            SetTabColor(longestRunTab, active == LeaderboardCategory.LongestRun);
            SetTabColor(totalCoinsTab, active == LeaderboardCategory.TotalCoins);
        }

        private void SetTabColor(Button tab, bool isActive)
        {
            if (tab == null) return;

            var text = tab.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.color = isActive ? activeTabColor : inactiveTabColor;
        }

        /// <summary>
        /// Refresh the summary strip (all 3 stats at a glance).
        /// </summary>
        private void RefreshSummary()
        {
            if (summaryContainer == null) return;

            int highScore  = PlayerPrefsManager.GetInt(PlayerPrefsKeys.HighestScore, 0);
            float highDist = PlayerPrefsManager.GetFloat(PlayerPrefsKeys.HighestDistance, 0f);
            int totalCoins = PlayerPrefsManager.GetInt(PlayerPrefsKeys.TotalCoinsCollected, 0);

            if (summaryHighScoreValue)  summaryHighScoreValue.text  = NumberFormatter.Format(highScore);
            if (summaryLongestRunValue) summaryLongestRunValue.text = NumberFormatter.FormatDistance(highDist);
            if (summaryTotalCoinsValue) summaryTotalCoinsValue.text = NumberFormatter.Format(totalCoins);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  FUTURE: Firebase Leaderboard Integration
        // ════════════════════════════════════════════════════════════════════════
        //
        //  When you add Firebase Firestore, implement these:
        //
        //  public void SubmitScore(string userId, int score) { ... }
        //  public void FetchTopScores(LeaderboardCategory category, int limit, System.Action<List<LeaderboardEntry>> callback) { ... }
        //  public void FetchPlayerRank(string userId, LeaderboardCategory category, System.Action<int> callback) { ... }
        //
        //  LeaderboardEntry would contain: rank, displayName, value, isCurrentPlayer
        //  Then populate a ScrollView list of LeaderboardRow prefabs.
        // ════════════════════════════════════════════════════════════════════════
    }
}
