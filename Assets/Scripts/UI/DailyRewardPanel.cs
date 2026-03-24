using DailyReward;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace UI
{
    /// <summary>
    /// Panel showing daily objectives and their progress.
    /// Wire up in the Inspector: 3 DailyRewardCards, a claim button, and a close button.
    /// </summary>
    public class DailyRewardPanel : MonoBehaviour
    {
        [Header("Objective Rows")]
        [SerializeField] private DailyRewardCard[] objectiveCards = new DailyRewardCard[3];

        [Header("Balance Bar")]
        [SerializeField] private CurrencyDisplay coinBalanceDisplay;
        [SerializeField] private CurrencyDisplay gemBalanceDisplay;

        [Header("Reward")]
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimButtonText;
        [SerializeField] private GameObject rewardIcon;

        [Header("Close")]
        [SerializeField] private Button closeButton;

        private DailyRewardManager _manager;
        private System.Action _onCurrencyChanged;
        private System.Action _onClose;

        private void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(Hide);
            if (claimButton) claimButton.onClick.AddListener(OnClaimClicked);
        }

        /// <param name="onCurrencyChanged">Called when a reward is claimed so external UI (e.g. MainMenu balance) can refresh.</param>
        /// <param name="onClose">Called when the panel closes.</param>
        public void Show(System.Action onCurrencyChanged = null, System.Action onClose = null)
        {
            _onCurrencyChanged = onCurrencyChanged;
            _onClose = onClose;
            _manager = DailyRewardManager.Instance;
            if (_manager == null)
            {
                Debug.LogWarning("DailyRewardPanel: DailyRewardManager not found.");
                return;
            }

            // Force-reload from PlayerPrefs to pick up progress saved during a game session
            _manager.Reload();

            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuOpen);
            gameObject.SetActive(true);

            coinBalanceDisplay?.InitValue(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, 0));
            gemBalanceDisplay?.InitValue(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Gems, 0));

            _manager.OnProgressChanged -= Refresh; // prevent double-sub
            _manager.OnProgressChanged += Refresh;

            Refresh();
        }

        public void Hide()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuClose);

            if (_manager != null)
                _manager.OnProgressChanged -= Refresh;

            gameObject.SetActive(false);
            _onClose?.Invoke();
            _onClose = null;
        }

        private void OnDestroy()
        {
            if (_manager != null)
                _manager.OnProgressChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_manager?.Data?.objectives == null) return;

            var objectives = _manager.Data.objectives;

            for (int i = 0; i < objectiveCards.Length; i++)
            {
                if (objectiveCards[i] == null) continue;

                if (i >= objectives.Count)
                    objectiveCards[i].Hide();
                else
                    objectiveCards[i].Populate(objectives[i]);
            }

            // Claim button state
            bool canClaim = _manager.AllComplete && !_manager.Data.rewardClaimed;
            bool alreadyClaimed = _manager.Data.rewardClaimed;

            if (claimButton)
                claimButton.interactable = canClaim;

            if (claimButtonText)
            {
                if (alreadyClaimed)
                    claimButtonText.text = "CLAIMED";
                else if (canClaim)
                    claimButtonText.text = "CLAIM REWARD";
                else
                    claimButtonText.text = "COMPLETE ALL";
            }

            if (rewardIcon)
                rewardIcon.SetActive(!alreadyClaimed);
        }

        private void OnClaimClicked()
        {
            if (_manager == null) return;

            if (_manager.TryClaimReward())
            {
                AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
                gemBalanceDisplay?.SetValue(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Gems, 0));
                _onCurrencyChanged?.Invoke();
                Refresh();
            }
        }
    }
}
