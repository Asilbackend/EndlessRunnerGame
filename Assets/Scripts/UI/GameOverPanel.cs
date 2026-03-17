using UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameOverPanel : MonoBehaviour
{
    public enum MedalType { Bronze, Silver, Gold, Champ }

    [System.Serializable]
    public class MedalConfig
    {
        public string title;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public AudioEventSFX sfx;
        [Tooltip("Sprite for the title background. Leave null to use the default.")]
        public Sprite titleBackground;
        [Tooltip("Sprite for the decorative effect image. Leave null to hide it.")]
        public Sprite effectSprite;
    }

    // ── Buttons ──────────────────────────────────────────────────────────────
    [SerializeField] Button playAgainButton;
    [SerializeField] Button lastCheckpointButton;
    [SerializeField] Button mainMenuButton;

    [Header("Checkpoint Button Icon")]
    [SerializeField] Image checkpointButtonIcon;
    [SerializeField] Sprite checkpointReplaySprite;
    [SerializeField] Sprite checkpointAdsSprite;

    [Header("Health Status Icon")]
    [SerializeField] Image healthStatusIcon;
    [SerializeField] Sprite adStatusSprite;
    [SerializeField] Sprite brokenHeartSprite;

    // ── Title Area ────────────────────────────────────────────────────────────
    [Header("Title Area")]
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] Image titleBackgroundImage;
    [SerializeField] Sprite defaultTitleBackground;
    [SerializeField] Image effectImage;

    // ── Score / Distance ──────────────────────────────────────────────────────
    [Header("Score Stats")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI highScoreText;
    [SerializeField] TextMeshProUGUI distanceText;
    [SerializeField] TextMeshProUGUI highDistanceText;
    [Tooltip("Shown only when a new score record is set.")]
    [SerializeField] GameObject newScoreRecordBadge;
    [Tooltip("Shown only when a new distance record is set.")]
    [SerializeField] GameObject newDistanceRecordBadge;

    // ── Medal ─────────────────────────────────────────────────────────────────
    [Header("Medal Display")]
    [SerializeField] Image medalIcon;
    [SerializeField] TextMeshProUGUI medalDescriptionText;
    [SerializeField] Animator medalAnimator;
    [Tooltip("Animator trigger name fired when the medal pops in.")]
    [SerializeField] string medalPopTrigger = "Pop";

    [Header("Medal Configs")]
    [SerializeField]
    MedalConfig bronzeConfig = new MedalConfig
    {
        title = "CRASHED!",
        description = "Dust yourself off and try again.",
        sfx = AudioEventSFX.MedalBronze
    };
    [SerializeField]
    MedalConfig silverConfig = new MedalConfig
    {
        title = "NOT BAD!",
        description = "You're warming up — keep pushing!",
        sfx = AudioEventSFX.MedalSilver
    };
    [SerializeField]
    MedalConfig goldConfig = new MedalConfig
    {
        title = "SO CLOSE!",
        description = "You were THIS close to glory!",
        sfx = AudioEventSFX.MedalGold
    };
    [SerializeField]
    MedalConfig champConfig = new MedalConfig
    {
        title = "NAILED IT!",
        description = "New personal record — absolute legend!",
        sfx = AudioEventSFX.MedalChamp
    };

    [Header("Medal Thresholds (fraction of high score)")]
    [SerializeField][Range(0f, 1f)] float silverThreshold = 0.50f;
    [SerializeField][Range(0f, 1f)] float goldThreshold = 0.85f;

    // ── State ─────────────────────────────────────────────────────────────────
    private MedalType _currentMedal;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            AddButtonHoverSound(playAgainButton);
        }
        if (lastCheckpointButton != null)
        {
            lastCheckpointButton.onClick.AddListener(OnCheckpointOrAdClicked);
            AddButtonHoverSound(lastCheckpointButton);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            AddButtonHoverSound(mainMenuButton);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void Show()
    {
        gameObject.SetActive(true);
        RefreshHealthState();
        RefreshStats();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Call this from the medal pop Animation Event to play the medal SFX.
    /// </summary>
    public void PlayMedalSFX()
    {
        AudioManager.Instance?.PlaySFX(GetMedalConfig(_currentMedal).sfx);
    }

    public void PlayEndSFX()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.EndGameOverPanel);
    }

    // ── Private ───────────────────────────────────────────────────────────────
    private void AddButtonHoverSound(Button button)
    {
        if (button == null) return;

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        trigger.triggers.Add(entry);
    }

    private void RefreshHealthState()
    {
        bool hasHealth = GameController.Instance != null && GameController.Instance.GameHealth > 0;

        if (checkpointButtonIcon != null)
            checkpointButtonIcon.sprite = hasHealth ? checkpointReplaySprite : checkpointAdsSprite;

        if (healthStatusIcon != null)
            healthStatusIcon.sprite = hasHealth ? brokenHeartSprite : adStatusSprite;
    }

    private void RefreshStats()
    {
        if (GameController.Instance == null) return;

        int currentScore = GameController.Instance.GamePoints;
        int highScore = GameController.Instance.HighestScore;
        float currentDistance = GameController.Instance.WorldManager?.WorldMover?.TotalDistanceTraveled ?? 0f;
        float highDistance = GameController.Instance.HighestDistance;

        if (scoreText != null)
            scoreText.text = currentScore.ToString();

        if (highScoreText != null)
            highScoreText.text = highScore.ToString();

        if (distanceText != null)
            distanceText.text = $"{currentDistance:F1}m";

        if (highDistanceText != null)
            highDistanceText.text = $"{highDistance:F1}m";

        if (newScoreRecordBadge != null)
            newScoreRecordBadge.SetActive(GameController.Instance.IsNewHighestScore);

        if (newDistanceRecordBadge != null)
            newDistanceRecordBadge.SetActive(GameController.Instance.IsNewHighestDistance);

        _currentMedal = CalculateMedal(currentScore, highScore);
        ApplyMedalConfig(_currentMedal);
    }

    private MedalType CalculateMedal(int current, int highest)
    {
        if (GameController.Instance.IsNewHighestScore)
            return MedalType.Champ;

        if (highest <= 0)
            return MedalType.Bronze;

        float ratio = (float)current / highest;
        if (ratio >= goldThreshold) return MedalType.Gold;
        if (ratio >= silverThreshold) return MedalType.Silver;
        return MedalType.Bronze;
    }

    private void ApplyMedalConfig(MedalType medal)
    {
        MedalConfig cfg = GetMedalConfig(medal);

        if (titleText != null)
            titleText.text = cfg.title;

        if (medalDescriptionText != null)
            medalDescriptionText.text = cfg.description;

        if (medalIcon != null && cfg.icon != null)
            medalIcon.sprite = cfg.icon;

        // Title background — use medal-specific or fall back to default
        if (titleBackgroundImage != null)
        {
            Sprite bg = cfg.titleBackground != null ? cfg.titleBackground : defaultTitleBackground;
            if (bg != null)
                titleBackgroundImage.sprite = bg;
        }

        // Effect image — show medal-specific or hide
        if (effectImage != null)
        {
            bool hasEffect = cfg.effectSprite != null;
            effectImage.gameObject.SetActive(hasEffect);
            if (hasEffect)
                effectImage.sprite = cfg.effectSprite;
        }

        // Trigger the medal pop animation
        if (medalAnimator != null && !string.IsNullOrEmpty(medalPopTrigger))
            medalAnimator.SetTrigger(medalPopTrigger);
    }

    private MedalConfig GetMedalConfig(MedalType medal) => medal switch
    {
        MedalType.Bronze => bronzeConfig,
        MedalType.Silver => silverConfig,
        MedalType.Gold => goldConfig,
        MedalType.Champ => champConfig,
        _ => bronzeConfig
    };

    // ── Button Callbacks ──────────────────────────────────────────────────────
    private void OnCheckpointOrAdClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);

        bool hasHealth = GameController.Instance != null && GameController.Instance.GameHealth > 0;
        if (hasHealth)
        {
            GameController.Instance.ResetToLastCheckpoint();
            Hide();
        }
        else
        {
            AdService.Instance.ShowRewardedAd(OnAdCompleted);
        }
    }

    private void OnAdCompleted()
    {
        GameController.Instance?.AddLife();
        GameController.Instance?.ResetToLastCheckpoint();
        Hide();
    }

    private void OnPlayAgainClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        GameController.Instance?.ResetGame();
        Hide();
    }

    private void OnMainMenuClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        Hide();
        SceneLoader.Load("MainMenu");
    }
}
