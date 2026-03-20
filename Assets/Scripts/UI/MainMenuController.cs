using DailyReward;
using Player;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GameConfigSO config;

    [Header("Selectors")]
    [SerializeField] private MapSelectorController mapSelector;
    [SerializeField] private VehicleSelectorController vehicleSelector;

    [Header("Currency Display")]
    [SerializeField] private UI.CurrencyDisplay coinsDisplay;
    [SerializeField] private UI.CurrencyDisplay gemsDisplay;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button dailyRewardButton;

    [Header("Panels")]
    [SerializeField] private UI.SettingsPanel settingsPanel;
    [SerializeField] private UI.LeaderboardPanel leaderboardPanel;
    [SerializeField] private UI.Shop.ShopPanel shopPanel;
    [SerializeField] private UI.DailyRewardPanel dailyRewardPanel;

    private void Awake()
    {
        if (playButton) playButton.onClick.AddListener(OnPlayClicked);

        if (shopButton) shopButton.onClick.AddListener(OnShopClicked);
        if (leaderboardButton) leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        if (settingsButton) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (dailyRewardButton) dailyRewardButton.onClick.AddListener(OnDailyRewardClicked);

        // Add hover sounds to all buttons
        AddButtonHoverSound(playButton);
        AddButtonHoverSound(shopButton);
        AddButtonHoverSound(leaderboardButton);
        AddButtonHoverSound(settingsButton);
        AddButtonHoverSound(dailyRewardButton);
    }

    private void AddButtonHoverSound(Button button)
    {
        if (button == null) return;

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        trigger.triggers.Add(entry);
    }

    private void Start()
    {
        if (mapSelector) mapSelector.InitFromSave();
        if (vehicleSelector) vehicleSelector.InitFromSave();

        // InitValue on first display so no animation fires on scene load
        coinsDisplay?.InitValue(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, 0));
        gemsDisplay?.InitValue(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Gems, 0));

        // Play main menu music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(AudioEventMusic.MainMenu, loop: true);
        }
    }

    public void RefreshCurrencyDisplay()
    {
        coinsDisplay?.SetValue(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, 0));
        gemsDisplay?.SetValue(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Gems, 0));
    }

    private void OnShopClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        if (shopPanel != null)
            shopPanel.Show(onClose: RefreshCurrencyDisplay);
    }

    private void OnLeaderboardClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        if (leaderboardPanel != null)
            leaderboardPanel.Show();
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        if (settingsPanel != null)
            settingsPanel.Show();
    }

    private void OnDailyRewardClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        if (dailyRewardPanel != null)
            dailyRewardPanel.Show(onCurrencyChanged: RefreshCurrencyDisplay);
    }

    private void OnPlayClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);

        var selectedMap = mapSelector != null ? mapSelector.Current : null;
        var selectedVehicle = vehicleSelector != null ? vehicleSelector.Current : null;

        if (selectedMap == null || selectedVehicle == null)
        {
            Debug.LogWarning("MainMenuController: Missing map or vehicle selection.");
            return;
        }

        PlayerPrefsManager.SetString(PlayerPrefsKeys.SelectedMapId, selectedMap.id);
        PlayerPrefsManager.SetString(PlayerPrefsKeys.SelectedVehicleId, selectedVehicle.EffectiveId);

        if (!string.IsNullOrWhiteSpace(selectedMap.sceneName))
            SceneLoader.Load(selectedMap.sceneName);
        else
            Debug.LogWarning("MainMenuController: No scene name on selected map.");
    }
}
