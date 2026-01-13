using Managers;
using UI;
using UnityEngine;
using World;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    public int GameHealth { get; private set; }
    public int GameCoins { get; private set; } = 0;
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
        SetCoins(0);
    }

    public int GetCoins()
    {
        return GameCoins;
    }

    public void SetCoins(int amount)
    {
        GameCoins = amount;
        _uiManager.PlayerHUD?.SetRunCoins(GameCoins);
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
        GameCoins = 0;
        IsGameOver = false;
        _uiManager.PlayerHUD.RefreshFromGameManager();
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
        _gameManager.SetPlayerCoins(PlayerPrefsManager.GetInt(PlayerPrefsKeys.Coins, 0) + GameCoins);
        //_gameManager.SetPlayerHealth(Health);
        _gameManager.IncrementGameNumber();
    }
}
