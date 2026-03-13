using Managers;
using Powerup;
using UI;
using UnityEngine;
using World;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    public int GameHealth { get; private set; }
    public int GamePoints { get; private set; } = 0;
    public bool IsGameOver { get; private set; } = false;
    public float ReverseMultiplier { get; private set; } = 2;


    private GameManager _gameManager;
    private UIManager _uiManager;
    private int _defaultHealth = 3;

    public PlayerController PlayerController;
    [HideInInspector] public WorldManager WorldManager;
    [HideInInspector] public float ReverseTime = 0.5f;
    [HideInInspector] public float StartTime = 1;
    [HideInInspector] public float PlayerColliderDisabledTime = 2;

    [Header("Coin Streak")]
    [SerializeField] private float streakResetTime = 1.5f; // Reset streak if no collect for this long
    [SerializeField] private float minStreakPitch = 1f;
    [SerializeField] private float maxStreakPitch = 1.8f;

    [Header("Speed-based Music Pitch")]
    [SerializeField] private float minMusicPitch = 1f; // Pitch at baseSpeed
    [SerializeField] private float maxMusicPitch = 1.5f; // Pitch at max speed

    [SerializeField] private World.ParticleEffectsSO _particleEffectsSO;
    [SerializeField] private GameConfigSO config;
    public World.ParticleEffectsSO ParticleEffectsSO => _particleEffectsSO;

    private int coinStreak = 0;
    private float streakTimer = 0f;
    private WorldMover _worldMover;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
        _gameManager = GameManager.Instance;
        _uiManager = UIManager.Instance;
        if (PlayerController == null)
        {
            PlayerController = FindFirstObjectByType<PlayerController>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        WorldManager = GetComponent<WorldManager>();
        _worldMover = GetComponent<WorldMover>();

        // Ensure PowerupManager exists on this GameObject
        if (GetComponent<PowerupManager>() == null)
            gameObject.AddComponent<PowerupManager>();

        SetPoints(0);

        if (config == null) return;

        string vehicleId = PlayerPrefsManager.GetString(PlayerPrefsKeys.SelectedVehicleId, config.defaultVehicleId);

        if (GameManager.Instance != null)
            GameManager.Instance.SetSelectedVehicleId(vehicleId);

        // Get vehicle health from the selected vehicle's data
        int vehicleHealth = _defaultHealth;
        if (GameManager.Instance != null && GameManager.Instance.PlayableObjectsSO != null)
        {
            var vehicleData = GameManager.Instance.PlayableObjectsSO.GetPlayableObjectDataById(vehicleId);
            if (vehicleData != null && vehicleData.health > 0)
                vehicleHealth = vehicleData.health;
        }
        SetHealth(vehicleHealth);

        // Play background music
        StartCoroutine(PlayGameplayMusicDelayed());
    }

    private void Update()
    {
        // Decrease streak timer and reset if expired
        if (coinStreak > 0 && !IsGameOver)
        {
            streakTimer -= Time.deltaTime;
            if (streakTimer <= 0)
            {
                coinStreak = 0;
            }
        }

        // Update distance meter display (always, so it's visible during game over too)
        if (!IsGameOver && _worldMover != null && _uiManager?.PlayerHUD != null)
        {
            _uiManager.PlayerHUD.SetDistanceMeter(_worldMover.TotalDistanceTraveled);
        }

        // Update music pitch based on current speed multiplier (chunk-based)
        if (!IsGameOver && _worldMover != null && AudioManager.Instance != null)
        {
            float speedMultiplier = _worldMover.CurrentSpeedMultiplier;
            // speedMultiplier ranges from 1.0 to 2.0 based on chunks passed
            float pitchProgress = Mathf.Clamp01(speedMultiplier - 1f);
            float musicPitch = Mathf.Lerp(minMusicPitch, maxMusicPitch, pitchProgress);
            AudioManager.Instance.SetMusicPitch(musicPitch);
        }
    }

    private System.Collections.IEnumerator PlayGameplayMusicDelayed()
    {
        yield return null; // Wait one frame for AudioManager to initialize
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(AudioEventMusic.Gameplay, loop: true, customFadeDuration: 1f);
        }
    }

    public int GetPoints()
    {
        return GamePoints;
    }

    public void SetPoints(int amount)
    {
        GamePoints = amount;
        _uiManager.PlayerHUD?.SetRunPoints(GamePoints);
    }

    public int GetHealth()
    {
        return GameHealth;
    }

    public void SetHealth(int amount)
    {
        GameHealth = amount;
        _uiManager.PlayerHUD?.SetHealth(amount);
    }

    /// <summary>
    /// Call this when a coin is collected. Returns the pitch multiplier based on current streak.
    /// </summary>
    public float OnCoinCollected()
    {
        coinStreak++;
        streakTimer = streakResetTime; // Reset the timer

        // Calculate pitch based on streak (0 = minPitch, maxStreak = maxPitch)
        float streakProgress = Mathf.Clamp01((coinStreak - 1) / 10f); // Assume maxStreak ~10
        float pitchMultiplier = Mathf.Lerp(minStreakPitch, maxStreakPitch, streakProgress);

        return pitchMultiplier;
    }

    /// <summary>
    /// Get current coin streak (for UI or debugging).
    /// </summary>
    public int GetCoinStreak() => coinStreak;

    /// <summary>
    /// Called when a chunk is passed to increase speed progression.
    /// </summary>
    public void OnChunkPassed()
    {
        if (_worldMover != null)
        {
            _worldMover.OnChunkPassed();
        }
    }

    public void GameOver()
    {
        // Clear all active powerup effects
        if (PowerupManager.Instance != null)
            PowerupManager.Instance.ClearAll();

        PlayerController.StopAnimationAndWheels();
        int currentHealth = GetHealth();
        currentHealth = Mathf.Max(0, --currentHealth);
        SetHealth(currentHealth);
        SaveProgress();
        IsGameOver = true;
        WorldManager.PauseWorld();

        // Play game over sound (music continues playing)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioEventSFX.GameOver);
        }

        _uiManager.GameOverPanel.Show();
    }

    public void ResetGame()
    {
        // Clear powerup state on full reset
        if (PowerupManager.Instance != null)
            PowerupManager.Instance.ClearAll();

        GamePoints = 0;
        IsGameOver = false;
        coinStreak = 0; // Reset coin streak on game restart
        _uiManager.PlayerHUD.RefreshFromGameController();

        // ResetWorld() resets speed and chunk count, so the speed multiplier
        // returns to 1.0. Update() will automatically sync pitch next frame.
        WorldManager.ResetWorld();

        // Resume gameplay music after world reset (speed is now base)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(AudioEventMusic.Gameplay, loop: true, customFadeDuration: 1f);
        }
    }

    public void ResetToLastCheckpoint()
    {
        IsGameOver = false;
        PlayerController.OnDeath();

        // Don't reset music pitch here — Update() will sync it with
        // the current speed multiplier next frame, avoiding a pitch glitch.

        WorldManager.ResetToLastCheckpoint(ReverseTime);
    }

    public void SaveProgress()
    {
        _gameManager.SetPlayerPoints(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, 0) + GamePoints);
        //_gameManager.SetPlayerHealth(Health);
        _gameManager.IncrementGameNumber();
    }
}
