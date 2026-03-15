using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI
{
    public class ConfirmationPanel : MonoBehaviour
    {
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private float autoResumeTimeout = 10f;

        private Coroutine _autoResumeCoroutine;

        private void Start()
        {
            if (yesButton != null)
                yesButton.onClick.AddListener(OnYesClicked);
            if (noButton != null)
                noButton.onClick.AddListener(OnNoClicked);

            gameObject.SetActive(false);
        }

        public void Show()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuOpen);
            gameObject.SetActive(true);
            Time.timeScale = 0f;
            StartAutoResume();
        }

        public void Hide()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuClose);
            gameObject.SetActive(false);
            StopAutoResume();
            Time.timeScale = 1f;
        }

        private void OnYesClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
            StopAutoResume();
            SceneLoader.Load("MainMenu");
        }

        private void OnNoClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
            Hide();
            if (UIManager.Instance != null && UIManager.Instance.CountdownUI != null)
                UIManager.Instance.CountdownUI.PlayCountdown();
        }

        private void StartAutoResume()
        {
            StopAutoResume();
            _autoResumeCoroutine = StartCoroutine(AutoResumeCoroutine());
        }

        private void StopAutoResume()
        {
            if (_autoResumeCoroutine != null)
            {
                StopCoroutine(_autoResumeCoroutine);
                _autoResumeCoroutine = null;
            }
        }

        private IEnumerator AutoResumeCoroutine()
        {
            yield return new WaitForSecondsRealtime(autoResumeTimeout);
            OnNoClicked();
        }
    }
}
