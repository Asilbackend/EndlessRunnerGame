using UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] Button playAgainButton;
    [SerializeField] Button lastCheckpointButton;
    [SerializeField] Button mainMenuButton;
    [SerializeField] Button watchAdButton;

    private void Start()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            AddButtonHoverSound(playAgainButton);
        }
        if (lastCheckpointButton != null)
        {
            lastCheckpointButton.onClick.AddListener(OnLastCheckpointClicked);
            AddButtonHoverSound(lastCheckpointButton);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            AddButtonHoverSound(mainMenuButton);
        }
        if (watchAdButton != null)
        {
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
            AddButtonHoverSound(watchAdButton);
        }
    }

    private void AddButtonHoverSound(Button button)
    {
        if (button == null) return;

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        trigger.triggers.Add(entry);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshCheckpointButton();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Disable the checkpoint button when the player has no health left
    private void RefreshCheckpointButton()
    {
        if (lastCheckpointButton == null) return;
        bool hasHealth = GameController.Instance != null && GameController.Instance.GameHealth > 0;
        lastCheckpointButton.interactable = hasHealth;
    }

    private void OnPlayAgainClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        GameController.Instance?.ResetGame();
        Hide();
    }

    private void OnLastCheckpointClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        GameController.Instance?.ResetToLastCheckpoint();
        Hide();
    }

    private void OnMainMenuClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        Hide();
        SceneLoader.Load("MainMenu");
    }

    private void OnWatchAdClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
        AdService.Instance.ShowRewardedAd(OnAdCompleted);
    }

    private void OnAdCompleted()
    {
        GameController.Instance?.AddLife();
        RefreshCheckpointButton();
    }
}
