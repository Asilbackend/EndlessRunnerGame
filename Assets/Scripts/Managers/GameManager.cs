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

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }


    public static GameManager Instance { get; private set; }
    private readonly int _defaultHealth = 3;
    private readonly int _defaultPoints = 0;

    //public int PlayerHealth { get; private set; }
    public int PlayerPoints { get; private set; }
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

        //PlayerHealth = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Health, _defaultHealth);
        PlayerPoints = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, _defaultPoints);
        PlayerGameNumber = PlayerPrefsManager.GetInt(PlayerPrefsKeys.GameNumber, 0);
    }

    //public void SetPlayerHealth(int health)
    //{
    //    PlayerHealth = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Health, health);
    //    PlayerPrefsManager.SetInt(PlayerPrefsKeys.Health, health);
    //}
    public void SetPlayerPoints(int points)
    {
        PlayerPoints = PlayerPrefsManager.GetInt(PlayerPrefsKeys.Points, points);
        PlayerPrefsManager.SetInt(PlayerPrefsKeys.Points, points);
    }

    public void IncrementGameNumber()
    {
        PlayerGameNumber = PlayerPrefsManager.GetInt(PlayerPrefsKeys.GameNumber, 0);
        PlayerPrefsManager.SetInt(PlayerPrefsKeys.GameNumber, PlayerGameNumber + 1);
    }
}
