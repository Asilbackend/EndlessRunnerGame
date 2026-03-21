using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Global leaderboard panel. Fetches top-100 players per category from Firebase Firestore
    /// and shows them in a scrollable list. A pinned strip at the bottom always shows the
    /// current player's rank — even when they are outside the top 100.
    ///
    /// Required prefab wiring in the Inspector:
    ///   Tab buttons       — highScoreTab, longestRunTab, totalCoinsTab
    ///   Close button      — closeButton
    ///   Category title    — categoryTitle (TMP_Text)
    ///   Scroll content    — rowContainer (Transform inside a ScrollRect > Viewport > Content)
    ///                       Add: Vertical Layout Group + Content Size Fitter (Vertical = Preferred)
    ///   Row prefab        — leaderboardRowPrefab (LeaderboardRow component, fixed height via Layout Element)
    ///   State labels      — loadingText, emptyText (TMP_Text)
    ///   Name-entry panel  — nameEntryPanel (NameEntryPanel)
    ///   Player rank strip — playerRankRow (LeaderboardRow, outside the ScrollRect, always visible)
    ///                       playerRankSection (GameObject wrapper — shown/hidden)
    /// </summary>
    public class LeaderboardPanel : MonoBehaviour
    {
        // ── Tab Buttons ─────────────────────────────────────────────────────────
        [Header("Tab Buttons")]
        [SerializeField]
        private Button highScoreTab;

        [SerializeField]
        private Button longestRunTab;

        [SerializeField]
        private Button totalCoinsTab;

        [Header("Tab Visual State")]
        [SerializeField]
        private Color activeTabColor = new Color(1f, 1f, 1f, 1f);

        [SerializeField]
        private Color inactiveTabColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        // ── Scroll List ─────────────────────────────────────────────────────────
        [Header("Scroll List")]
        [SerializeField]
        private TMP_Text categoryTitle;

        [SerializeField]
        private Transform rowContainer;

        [SerializeField]
        private GameObject leaderboardRowPrefab;

        [Header("State Labels")]
        [SerializeField]
        private TMP_Text loadingText;

        [SerializeField]
        private TMP_Text emptyText;

        // ── Pinned Player Rank ──────────────────────────────────────────────────
        [Header("Player Rank Strip (pinned, outside scroll)")]
        [SerializeField]
        private GameObject playerRankSection;

        [SerializeField]
        private LeaderboardRow playerRankRow;

        // ── Name Entry ──────────────────────────────────────────────────────────
        [Header("Name Entry")]
        [SerializeField]
        private NameEntryPanel nameEntryPanel;

        // ── Close ───────────────────────────────────────────────────────────────
        [Header("Close")]
        [SerializeField]
        private Button closeButton;

        // ── State ───────────────────────────────────────────────────────────────
        private LeaderboardCategory _activeCategory = LeaderboardCategory.HighScore;
        private readonly List<GameObject> _spawnedRows = new List<GameObject>();
        private bool _isFetching;

        private void Awake()
        {
            if (highScoreTab)
                highScoreTab.onClick.AddListener(() =>
                    SelectCategory(LeaderboardCategory.HighScore)
                );
            if (longestRunTab)
                longestRunTab.onClick.AddListener(() =>
                    SelectCategory(LeaderboardCategory.LongestRun)
                );
            if (totalCoinsTab)
                totalCoinsTab.onClick.AddListener(() =>
                    SelectCategory(LeaderboardCategory.TotalCoins)
                );
            if (closeButton)
                closeButton.onClick.AddListener(Hide);

            if (nameEntryPanel)
                nameEntryPanel.OnNameConfirmed += _ => FetchAndDisplay(_activeCategory);
        }

        // ── Public API ──────────────────────────────────────────────────────────

        public void Show()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuOpen);
            gameObject.SetActive(true);

            var svc = FirebaseLeaderboardService.Instance;

            // First-time player: ask for a display name before showing scores
            if (svc != null && !svc.HasDisplayName)
            {
                ShowLoading(false);
                ShowEmpty(false);
                ShowPlayerRankSection(false);
                ClearRows();
                nameEntryPanel?.Show();
                return;
            }

            ShowCategory(_activeCategory);
        }

        public void Hide()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuClose);
            nameEntryPanel?.Hide();
            gameObject.SetActive(false);
        }

        // ── Private ─────────────────────────────────────────────────────────────

        private void SelectCategory(LeaderboardCategory category)
        {
            if (_isFetching)
                return;
            AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
            ShowCategory(category);
        }

        private void ShowCategory(LeaderboardCategory category)
        {
            _activeCategory = category;
            UpdateTabVisuals(category);

            categoryTitle.text = category switch
            {
                LeaderboardCategory.HighScore => "BEST SCORE",
                LeaderboardCategory.LongestRun => "LONGEST RUN",
                LeaderboardCategory.TotalCoins => "TOTAL COINS",
                _ => "",
            };

            FetchAndDisplay(category);
        }

        private void FetchAndDisplay(LeaderboardCategory category)
        {
            if (_isFetching)
                return;
            _isFetching = true;

            ClearRows();
            ShowLoading(true);
            ShowEmpty(false);
            ShowPlayerRankSection(false);

            var svc = FirebaseLeaderboardService.Instance;
            if (svc == null)
            {
                ShowLoading(false);
                ShowEmpty(true);
                _isFetching = false;
                return;
            }

            // Fetch top 100 and the player's own rank in parallel
            bool listDone = false;
            bool rankDone = false;
            List<LeaderboardEntry> fetchedEntries = null;
            LeaderboardEntry playerEntry = null;

            void TryFinish()
            {
                if (!listDone || !rankDone)
                    return;

                _isFetching = false;
                ShowLoading(false);

                if (fetchedEntries == null || fetchedEntries.Count == 0)
                {
                    ShowEmpty(true);
                }
                else
                {
                    ShowEmpty(false);
                    PopulateRows(fetchedEntries, category);
                }

                // Always show the pinned player strip
                if (playerEntry != null)
                {
                    playerRankRow?.Setup(playerEntry, category);
                    ShowPlayerRankSection(true);
                }
            }

            svc.FetchTopScores(
                category,
                100,
                entries =>
                {
                    fetchedEntries = entries;
                    listDone = true;
                    TryFinish();
                }
            );

            svc.FetchPlayerRank(
                category,
                entry =>
                {
                    playerEntry = entry;
                    rankDone = true;
                    TryFinish();
                }
            );
        }

        private void PopulateRows(List<LeaderboardEntry> entries, LeaderboardCategory category)
        {
            if (leaderboardRowPrefab == null || rowContainer == null)
                return;

            foreach (var entry in entries)
            {
                var go = Instantiate(leaderboardRowPrefab, rowContainer);
                var row = go.GetComponent<LeaderboardRow>();
                if (row != null)
                    row.Setup(entry, category);
                _spawnedRows.Add(go);
            }
        }

        private void ClearRows()
        {
            foreach (var go in _spawnedRows)
                Destroy(go);
            _spawnedRows.Clear();
        }

        private void ShowLoading(bool show)
        {
            if (loadingText)
                loadingText.gameObject.SetActive(show);
        }

        private void ShowEmpty(bool show)
        {
            if (emptyText)
                emptyText.gameObject.SetActive(show);
        }

        private void ShowPlayerRankSection(bool show)
        {
            if (playerRankSection)
                playerRankSection.SetActive(show);
        }

        private void UpdateTabVisuals(LeaderboardCategory active)
        {
            SetTabColor(highScoreTab, active == LeaderboardCategory.HighScore);
            SetTabColor(longestRunTab, active == LeaderboardCategory.LongestRun);
            SetTabColor(totalCoinsTab, active == LeaderboardCategory.TotalCoins);
        }

        private void SetTabColor(Button tab, bool isActive)
        {
            if (tab == null)
                return;
            var text = tab.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.color = isActive ? activeTabColor : inactiveTabColor;
        }
    }
}
