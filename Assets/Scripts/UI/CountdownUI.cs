using System.Collections;
using TMPro;
using UnityEngine;

namespace UI
{
    public class CountdownUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private float displayDuration = 0.8f;

        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void PlayCountdown(System.Action onCountdownComplete = null)
        {
            StartCoroutine(CountdownCoroutine(onCountdownComplete));
        }

        private IEnumerator CountdownCoroutine(System.Action onCountdownComplete)
        {
            if (GameController.Instance != null && GameController.Instance.WorldManager != null)
                GameController.Instance.WorldManager.PauseWorld();

            gameObject.SetActive(true);

            // Show 3
            if (countdownText != null)
                countdownText.text = "3";
            yield return new WaitForSecondsRealtime(displayDuration);

            // Show 2
            if (countdownText != null)
                countdownText.text = "2";
            yield return new WaitForSecondsRealtime(displayDuration);

            // Show 1
            if (countdownText != null)
                countdownText.text = "1";
            yield return new WaitForSecondsRealtime(displayDuration);

            // Show GO
            if (countdownText != null)
                countdownText.text = "GO!";
            yield return new WaitForSecondsRealtime(displayDuration);

            gameObject.SetActive(false);

            if (GameController.Instance != null && GameController.Instance.WorldManager != null)
                GameController.Instance.WorldManager.ResumeWorld();

            onCountdownComplete?.Invoke();
        }
    }
}
