using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// In-game pause menu: volume settings + Resume + Garage (return to menu) + Close.
    /// </summary>
    public class GameMenuPanel : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button garageButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (garageButton != null) garageButton.onClick.AddListener(OnGarageClicked);
            if (closeButton != null)  closeButton.onClick.AddListener(OnResumeClicked);

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)  musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)    sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        public void Show()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuOpen);
            gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Hide()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuClose);
            gameObject.SetActive(false);
            Time.timeScale = 1f;
        }

        private void OnResumeClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
            Hide();
            UIManager.Instance?.CountdownUI?.PlayCountdown();
        }

        private void OnGarageClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
            Hide();
            SceneLoader.Load("MainMenu");
        }

        private void OnMasterVolumeChanged(float value) => AudioManager.Instance?.SetMasterVolume(value);
        private void OnMusicVolumeChanged(float value)  => AudioManager.Instance?.SetMusicVolume(value);
        private void OnSFXVolumeChanged(float value)    => AudioManager.Instance?.SetSFXVolume(value);
    }
}
