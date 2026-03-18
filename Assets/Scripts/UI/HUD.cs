using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text runPointsText;
        [SerializeField] private TMP_Text mapName;
        [SerializeField] private TMP_Text distanceMeterText;
        [SerializeField] private Button menuButton;

        [Header("Streak UI")]
        [SerializeField] private GameObject streakContainer;
        [SerializeField] private TMP_Text streakText;

        private Coroutine _streakWiggleCoroutine;
        private Coroutine _streakCommitCoroutine;

        private void OnEnable()
        {
            RefreshFromGameController();
        }

        private void Start()
        {
            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);

            if (streakContainer != null)
                streakContainer.SetActive(false);
        }

        private void OnMenuClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);
            if (GameController.Instance != null && GameController.Instance.WorldManager != null)
                GameController.Instance.WorldManager.PauseWorld();
            if (UIManager.Instance != null && UIManager.Instance.GameMenuPanel != null)
                UIManager.Instance.GameMenuPanel.Show();
        }

        public void SetHealth(int health)
        {
            if (healthText != null)
                healthText.text = health.ToString();
        }

        public void SetMapName(string name)
        {
            if (mapName != null)
                mapName.text = name;
        }

        public void SetRunPoints(int points)
        {
            if (runPointsText != null)
                runPointsText.text = points.ToString();
        }

        /// <summary>
        /// Sets points and plays a scale-pulse animation to show the addition.
        /// </summary>
        public void SetRunPointsWithAnimation(int points, int pointsAdded)
        {
            SetRunPoints(points);
            if (pointsAdded > 0)
                StartCoroutine(AnimatePointsAddition());
        }

        public void AddRunPoints(int amount)
        {
            if (runPointsText == null) return;

            if (int.TryParse(runPointsText.text, out int current))
            {
                current += amount;
                runPointsText.text = current.ToString();
            }
            else
            {
                runPointsText.text = amount.ToString();
            }
        }

        public void SetDistanceMeter(float distance)
        {
            if (distanceMeterText != null)
                distanceMeterText.text = $"{distance:F1}m";
        }

        // ===================================================================
        //  STREAK UI
        // ===================================================================

        /// <summary>
        /// Called on every coin collected during a streak. Shows/updates "+N" with a wiggle.
        /// </summary>
        public void ShowStreak(int streak)
        {
            // Stop any in-progress commit animation and restore scale
            if (_streakCommitCoroutine != null)
            {
                StopCoroutine(_streakCommitCoroutine);
                _streakCommitCoroutine = null;
                if (streakText != null)
                    streakText.rectTransform.localScale = Vector3.one;
            }

            if (streakContainer != null) streakContainer.SetActive(true);
            if (streakText != null)     streakText.text = $"+{streak}";

            if (_streakWiggleCoroutine != null) StopCoroutine(_streakWiggleCoroutine);
            _streakWiggleCoroutine = StartCoroutine(WiggleStreak());
        }

        /// <summary>
        /// Called when the streak expires naturally. Plays a commit animation then hides.
        /// The bonus coins are already added to the score by GameController before this call.
        /// </summary>
        public void CommitStreak(int bonus)
        {
            if (_streakWiggleCoroutine != null)
            {
                StopCoroutine(_streakWiggleCoroutine);
                _streakWiggleCoroutine = null;
            }

            if (_streakCommitCoroutine != null) StopCoroutine(_streakCommitCoroutine);
            _streakCommitCoroutine = StartCoroutine(CommitStreakAnimation(bonus));
        }

        /// <summary>
        /// Immediately hides the streak display without animating (game over / reset).
        /// </summary>
        public void HideStreak()
        {
            if (_streakWiggleCoroutine != null) { StopCoroutine(_streakWiggleCoroutine); _streakWiggleCoroutine = null; }
            if (_streakCommitCoroutine != null) { StopCoroutine(_streakCommitCoroutine); _streakCommitCoroutine = null; }
            if (streakText != null) streakText.rectTransform.localScale = Vector3.one;
            if (streakContainer != null) streakContainer.SetActive(false);
        }

        // ===================================================================
        //  STREAK ANIMATIONS
        // ===================================================================

        /// <summary>
        /// Quick scale-punch: grow to 1.45× then spring back to 1×.
        /// </summary>
        private IEnumerator WiggleStreak()
        {
            if (streakText == null) yield break;

            RectTransform rt = streakText.rectTransform;
            const float growDur   = 0.10f;
            const float shrinkDur = 0.12f;
            Vector3 peak = new Vector3(1.45f, 1.45f, 1f);

            float t = 0f;
            while (t < growDur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(Vector3.one, peak, t / growDur);
                yield return null;
            }

            t = 0f;
            while (t < shrinkDur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(peak, Vector3.one, t / shrinkDur);
                yield return null;
            }

            rt.localScale = Vector3.one;
            _streakWiggleCoroutine = null;
        }

        /// <summary>
        /// Brief hold → scale-up pulse → shrink to zero → hide.
        /// </summary>
        private IEnumerator CommitStreakAnimation(int bonus)
        {
            if (streakText == null)
            {
                if (streakContainer != null) streakContainer.SetActive(false);
                yield break;
            }

            RectTransform rt = streakText.rectTransform;
            rt.localScale = Vector3.one;

            // Hold briefly so the player can read the final number
            yield return new WaitForSeconds(0.25f);

            // Pulse up
            const float pulseDur = 0.12f;
            Vector3 pulse = new Vector3(1.35f, 1.35f, 1f);
            float t = 0f;
            while (t < pulseDur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(Vector3.one, pulse, t / pulseDur);
                yield return null;
            }

            // Shrink to zero
            const float shrinkDur = 0.22f;
            t = 0f;
            while (t < shrinkDur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(pulse, Vector3.zero, t / shrinkDur);
                yield return null;
            }

            rt.localScale = Vector3.one;
            if (streakContainer != null) streakContainer.SetActive(false);
            _streakCommitCoroutine = null;
        }

        // ===================================================================
        //  POINTS ANIMATION
        // ===================================================================

        /// <summary>
        /// Scale-pulse animation to highlight points addition.
        /// </summary>
        private IEnumerator AnimatePointsAddition()
        {
            if (runPointsText == null) yield break;

            RectTransform rt = runPointsText.rectTransform;
            Vector3 baseScale = Vector3.one;
            const float duration = 0.18f;
            Vector3 peak = new Vector3(1.35f, 1.35f, 1f);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(baseScale, peak, t / duration);
                yield return null;
            }

            t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(peak, baseScale, t / duration);
                yield return null;
            }

            rt.localScale = baseScale;
        }

        // ===================================================================
        //  MISC
        // ===================================================================

        public void RefreshFromGameController()
        {
            if (GameController.Instance == null) return;

            SetHealth(GameController.Instance.GameHealth);
            SetRunPoints(GameController.Instance.GamePoints);
        }

        public void ForceRefresh()
        {
            RefreshFromGameController();
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
}
