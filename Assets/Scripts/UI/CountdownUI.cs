using System.Collections;
using TMPro;
using UnityEngine;

namespace UI
{
    public class CountdownUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private float displayDuration = 1f;
        [SerializeField] private float goDisplayDuration = 0.5f;

        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void PlayCountdown(System.Action onCountdownComplete = null)
        {
            gameObject.SetActive(true);
            StartCoroutine(CountdownCoroutine(onCountdownComplete));
        }

        private IEnumerator CountdownCoroutine(System.Action onCountdownComplete)
        {
            if (GameController.Instance != null && GameController.Instance.WorldManager != null)
                GameController.Instance.WorldManager.PauseWorld();

            // Play the 4-beep SFX — beeps land on each number and GO
            AudioManager.Instance?.PlaySFX(AudioEventSFX.CountdownBeep);

            if (countdownText != null) countdownText.text = "3";
            yield return new WaitForSecondsRealtime(displayDuration);

            if (countdownText != null) countdownText.text = "2";
            yield return new WaitForSecondsRealtime(displayDuration);

            if (countdownText != null) countdownText.text = "1";
            yield return new WaitForSecondsRealtime(displayDuration);

            // 4th beep — show GO and resume world immediately
            if (countdownText != null) countdownText.text = "GO!";
            if (GameController.Instance != null && GameController.Instance.WorldManager != null)
                GameController.Instance.WorldManager.ResumeWorld();

            onCountdownComplete?.Invoke();

            yield return new WaitForSecondsRealtime(goDisplayDuration);
            gameObject.SetActive(false);
        }
    }
}
