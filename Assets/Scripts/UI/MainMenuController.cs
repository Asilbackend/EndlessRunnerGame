using Player;
using TMPro;
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

    [Header("Top Bar UI")]
    [SerializeField] private TMP_Text coinsText;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button dailyRewardButton;

    private void Awake()
    {
        if (playButton) playButton.onClick.AddListener(OnPlayClicked);

        if (shopButton) shopButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick); Debug.Log("Shop"); });
        if (leaderboardButton) leaderboardButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick); Debug.Log("Leaderboard"); });
        if (settingsButton) settingsButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick); Debug.Log("Settings"); });
        if (dailyRewardButton) dailyRewardButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick); Debug.Log("Daily Reward"); });

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

        if (coinsText) coinsText.text = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, 0).ToString();

        // Play main menu music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(AudioEventMusic.MainMenu, loop: true);
        }
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
