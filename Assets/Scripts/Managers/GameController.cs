using Managers;
using UI;
using UnityEngine;
using World;
using static UnityEngine.Rendering.STP;

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

    [SerializeField] private World.ParticleEffectsSO _particleEffectsSO;
    [SerializeField] private GameConfigSO config;
    public World.ParticleEffectsSO ParticleEffectsSO => _particleEffectsSO;

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

    private void Start()
    {
        WorldManager = GetComponent<WorldManager>();
        SetHealth(_defaultHealth);
        SetPoints(0);

        if (config == null) return;

        string vehicleId = PlayerPrefsManager.GetString(PlayerPrefsKeys.SelectedVehicleId, config.defaultVehicleId);

        if (GameManager.Instance != null)
            GameManager.Instance.SetSelectedVehicleId(vehicleId);
        GameHealth = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Health, _defaultHealth);
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

    public void GameOver()
    {
        PlayerController.StopAnimationAndWheels();
        int currentHealth = GetHealth();
        currentHealth = Mathf.Max(0, --currentHealth);
        SetHealth(currentHealth);
        SaveProgress();
        IsGameOver = true;
        WorldManager.PauseWorld();
        _uiManager.GameOverPanel.Show();
    }

    public void ResetGame()
    {
        GamePoints = 0;
        IsGameOver = false;
        _uiManager.PlayerHUD.RefreshFromGameController();
        WorldManager.ResetWorld();
    }

    public void ResetToLastCheckpoint()
    {
        IsGameOver = false;
        PlayerController.OnDeath();
        WorldManager.ResetToLastCheckpoint(ReverseTime);
    }

    public void SaveProgress()
    {
        _gameManager.SetPlayerPoints(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, 0) + GamePoints);
        //_gameManager.SetPlayerHealth(Health);
        _gameManager.IncrementGameNumber();
    }
}
