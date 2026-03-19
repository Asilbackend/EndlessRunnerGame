using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSelectorController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GameConfigSO config;

    [Header("UI")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject lockedOverlay;

    private int _index;
    private MapDefinitionSO _current;

    public MapDefinitionSO Current => _current;

    private void Awake()
    {
        if (prevButton) prevButton.onClick.AddListener(Prev);
        if (nextButton) nextButton.onClick.AddListener(Next);
        if (selectButton) selectButton.onClick.AddListener(ConfirmSelection);
    }

    public void InitFromSave()
    {
        if (config == null || config.maps == null || config.maps.Length == 0)
        {
            _index = 0;
            _current = null;
            return;
        }

        string fallback = config.defaultMapId;
        string savedId = PlayerPrefsManager.GetString(PlayerPrefsKeys.SelectedMapId, fallback);

        _index = FindIndexById(savedId);
        if (_index < 0) _index = 0;

        ApplyIndex(_index, save: false);
    }

    private int FindIndexById(string id)
    {
        if (config?.maps == null || string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < config.maps.Length; i++)
            if (config.maps[i] != null && config.maps[i].id == id)
                return i;
        return -1;
    }

    private void Prev()
    {
        if (config?.maps == null || config.maps.Length == 0) return;
        ApplyIndex((_index - 1 + config.maps.Length) % config.maps.Length, save: true);
    }

    private void Next()
    {
        if (config?.maps == null || config.maps.Length == 0) return;
        ApplyIndex((_index + 1) % config.maps.Length, save: true);
    }

    private void ApplyIndex(int newIndex, bool save)
    {
        if (config?.maps == null || newIndex < 0 || newIndex >= config.maps.Length) return;

        _index = newIndex;
        _current = config.maps[_index];

        if (_current == null) return;

        bool locked = IsLocked(_current);

        if (thumbnailImage && _current.thumbnail) thumbnailImage.sprite = _current.thumbnail;
        if (titleText) titleText.text = _current.displayName;
        if (lockedOverlay) lockedOverlay.SetActive(locked);

        if (selectButton) selectButton.interactable = !locked;

        if (save)
        {
            PlayerPrefsManager.SetString(PlayerPrefsKeys.SelectedMapId, _current.id);
            UIEventBus.RaiseMapChanged(_current.id);
            AnalyticsEvents.MapSelected(_current.id);
        }
    }

    private bool IsLocked(MapDefinitionSO map)
    {
        if (map == null || !map.lockedByDefault) return false;
        string key = $"unlock_map_{map.id}";
        return PlayerPrefs.GetInt(key, 0) != 1;
    }

    private void ConfirmSelection()
    {
        if (_current == null) return;
        PlayerPrefsManager.SetString(PlayerPrefsKeys.SelectedMapId, _current.id);
        UIEventBus.RaiseMapChanged(_current.id);
    }
}
