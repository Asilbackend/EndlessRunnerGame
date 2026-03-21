using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Persistent full-screen loading overlay. Lives on the GameManager
    /// DontDestroyOnLoad hierarchy so it survives scene transitions.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("Image with type set to Filled (Radial 360). Used as the circular spinner.")]
        [SerializeField] private Image spinnerImage;

        [Header("Spinner")]
        [SerializeField] private float spinSpeed = 360f;

        [Header("Timing")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float minimumDisplayTime = 0.5f;

        private bool _isLoading;
        private RectTransform _spinnerRect;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (spinnerImage != null)
                _spinnerRect = spinnerImage.rectTransform;

            // Start hidden
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!_isLoading || _spinnerRect == null) return;
            _spinnerRect.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Load a scene with a fade-to-black transition and async loading.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading || string.IsNullOrWhiteSpace(sceneName)) return;
            StartCoroutine(LoadCoroutine(sceneName));
        }

        private IEnumerator LoadCoroutine(string sceneName)
        {
            _isLoading = true;

            // Restore timeScale in case we're coming from a paused state
            Time.timeScale = 1f;

            // Fade in the overlay
            yield return FadeCanvasGroup(0f, 1f, fadeDuration);

            // Start async load but don't allow activation yet
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            float elapsed = 0f;

            // Wait for load to reach 0.9 (Unity's "ready" threshold) and minimum display time
            while (op.progress < 0.9f || elapsed < minimumDisplayTime)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Activate the loaded scene
            op.allowSceneActivation = true;

            // Wait one frame for the new scene to initialize
            yield return null;

            // Fade out the overlay
            yield return FadeCanvasGroup(1f, 0f, fadeDuration);

            _isLoading = false;
        }

        private IEnumerator FadeCanvasGroup(float from, float to, float duration)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = to;

            if (to <= 0f)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }
    }
}
