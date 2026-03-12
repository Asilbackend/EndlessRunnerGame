using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VehicleSelectorController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GameConfigSO config;

    [Header("UI")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Image vehicleIcon;
    [SerializeField] private TMP_Text vehicleNameText;

    [SerializeField] private StarsPanel healthStars;
    [SerializeField] private StarsPanel driftStars;


    private int _index;
    private PlayableObjectData _current;

    public PlayableObjectData Current => _current;

    private PlayableObjectData[] _vehicles => config?.playableObjects?.PlayableObjectDatas?.Count > 0
        ? config.playableObjects.PlayableObjectDatas.ToArray()
        : null;

    private void Awake()
    {
        if (prevButton) prevButton.onClick.AddListener(Prev);
        if (nextButton) nextButton.onClick.AddListener(Next);
    }

    public void InitFromSave()
    {
        var list = _vehicles;
        if (list == null || list.Length == 0)
        {
            _index = 0;
            _current = null;
            return;
        }

        string fallback = config.defaultVehicleId;
        string savedId = PlayerPrefsManager.GetString(PlayerPrefsKeys.SelectedVehicleId, fallback);

        _index = FindIndexById(savedId);
        if (_index < 0) _index = 0;

        ApplyIndex(_index, save: false);
    }

    private int FindIndexById(string id)
    {
        var list = _vehicles;
        if (list == null || string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < list.Length; i++)
            if (list[i] != null && list[i].EffectiveId == id)
                return i;
        for (int i = 0; i < list.Length; i++)
            if (list[i] != null && list[i].name.ToString() == id)
                return i;
        return -1;
    }

    private void Prev()
    {
        var list = _vehicles;
        if (list == null || list.Length == 0) return;
        ApplyIndex((_index - 1 + list.Length) % list.Length, save: true);
    }

    private void Next()
    {
        var list = _vehicles;
        if (list == null || list.Length == 0) return;
        ApplyIndex((_index + 1) % list.Length, save: true);
    }

    private void ApplyIndex(int newIndex, bool save)
    {
        var list = _vehicles;
        if (list == null || newIndex < 0 || newIndex >= list.Length) return;

        _index = newIndex;
        _current = list[_index];
        if (_current == null) return;

        bool locked = IsLocked(_current);

        if (vehicleIcon && _current.icon) vehicleIcon.sprite = _current.icon;
        if (vehicleNameText) vehicleNameText.text = string.IsNullOrEmpty(_current.displayName) ? _current.name.ToString() : _current.displayName;

        healthStars?.SetStars(Current.healthStars);
        driftStars?.SetStars(_current.driftStars);

        // Play vehicle selection sound on every vehicle change (browse or select)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioEventSFX.SelectVehicle);
        }

        if (save && !locked)
        {
            PlayerPrefsManager.SetString(PlayerPrefsKeys.SelectedVehicleId, _current.EffectiveId);
            UIEventBus.RaiseVehicleChanged(_current.EffectiveId);
        }

    }

    private bool IsLocked(PlayableObjectData v)
    {
        if (v == null || !v.lockedByDefault) return false;
        string key = $"unlock_vehicle_{v.EffectiveId}";
        return PlayerPrefs.GetInt(key, 0) != 1;
    }
}
