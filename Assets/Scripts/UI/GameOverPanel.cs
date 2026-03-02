using UI;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] Button playAgainButton;
    [SerializeField] Button lastCheckpointButton;
    [SerializeField] Button mainMenuButton;

    private void Start()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }
        if (lastCheckpointButton != null)
        {
            lastCheckpointButton.onClick.AddListener(OnLastCheckpointClicked);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void OnEnable()
    {
        
    }

    private void OnPlayAgainClicked()
    {
        GameController.Instance?.ResetGame();
        Hide();
    }

    private void OnLastCheckpointClicked()
    {
        GameController.Instance?.ResetToLastCheckpoint();
        Hide();
    }

    private void OnMainMenuClicked()
    {
        Hide();
        SceneLoader.Load("MainMenu");
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
