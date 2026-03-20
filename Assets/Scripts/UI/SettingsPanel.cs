using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        private void OnEnable()
        {
            SyncSliders();
        }

        public void Show()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuOpen);
            gameObject.SetActive(true);
        }

        private void SyncSliders()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;

            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(audio.MasterVolume);
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(audio.MusicVolume);
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(audio.SFXVolume);
        }

        public void Hide()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuClose);
            gameObject.SetActive(false);
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMasterVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXVolume(value);
        }

        private void OnCloseClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
            Hide();
        }
    }
}
