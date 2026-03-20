using DailyReward;
using Managers;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private bool _debugMode = true;
    public bool DebugMode => _debugMode;


    [SerializeField] private bool _noCollisionMode = true;
    public bool NoCollisionMode => _noCollisionMode;

    [SerializeField] private PlayableObjectsSO _playableObjectsSO;
    public PlayableObjectsSO PlayableObjectsSO => _playableObjectsSO;

    private string _selectedVehicleIdOverride;

    // Persist and retrieve selected vehicle from PlayerPrefs so PlayerController can read it reliably
    public void SetSelectedVehicleId(string vehicleId)
    {
        _selectedVehicleIdOverride = vehicleId;
        if (!string.IsNullOrEmpty(vehicleId))
        {
            PlayerPrefsManager.SetString(PlayerPrefsKeys.SelectedVehicleId, vehicleId);
        }
    }
    public string GetSelectedVehicleId() => _selectedVehicleIdOverride;

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }


    public static GameManager Instance { get; private set; }
    private readonly int _defaultPoints = 0;

    //public int PlayerHealth { get; private set; }
    public int PlayerPoints { get; private set; }
    public int PlayerGems   { get; private set; }
    public int PlayerGameNumber { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        // Ensure DailyRewardManager exists on this persistent GameObject
        if (GetComponent<DailyRewardManager>() == null)
            gameObject.AddComponent<DailyRewardManager>();

        // Load selected vehicle from PlayerPrefs so it's available immediately
        _selectedVehicleIdOverride = PlayerPrefsManager.GetString(PlayerPrefsKeys.SelectedVehicleId, string.Empty);

        //PlayerHealth = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Health, _defaultHealth);
        PlayerPoints = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, _defaultPoints);
        PlayerGems   = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Gems, 0);
        PlayerGameNumber = PlayerPrefsManager.GetInt(PlayerPrefsKeys.GameNumber, 0);
    }

    //public void SetPlayerHealth(int health)
    //{
    //    PlayerHealth = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Health, health);
    //    PlayerPrefsManager.SetInt(PlayerPrefsKeys.Health, health);
    //}
    public void SetPlayerPoints(int points)
    {
        PlayerPoints = points;
        PlayerPrefsManager.SetInt(PlayerPrefsKeys.Points, points);
    }

    public void AddGems(int amount)
    {
        PlayerGems = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Gems, 0) + amount;
        PlayerPrefsManager.SetInt(PlayerPrefsKeys.Gems, PlayerGems);
    }

    public bool SpendGems(int amount)
    {
        int current = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Gems, 0);
        if (current < amount) return false;
        PlayerGems = current - amount;
        PlayerPrefsManager.SetInt(PlayerPrefsKeys.Gems, PlayerGems);
        return true;
    }

    public bool SpendCoins(int amount)
    {
        int current = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, 0);
        if (current < amount) return false;
        SetPlayerPoints(current - amount);
        return true;
    }

    public void IncrementGameNumber()
    {
        PlayerGameNumber = PlayerPrefsManager.GetInt(PlayerPrefsKeys.GameNumber, 0) + 1;
        PlayerPrefsManager.SetInt(PlayerPrefsKeys.GameNumber, PlayerGameNumber);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
