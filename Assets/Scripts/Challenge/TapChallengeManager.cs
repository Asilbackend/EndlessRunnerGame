using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace Challenge
{
    /// <summary>
    /// Core manager for the slow-motion tap challenge.
    /// Attach to a persistent scene object (e.g. GameController or its own "ChallengeSystem" object).
    /// Requires a dedicated UI Canvas for spawning targets.
    /// </summary>
    public class TapChallengeManager : MonoBehaviour
    {
        public static TapChallengeManager Instance { get; private set; }

        // ── Target Spawning ─────────────────────────────────────────
        [Header("Target Spawning")]
        [Tooltip("Prefab with TapTarget + Image. Will be instantiated under challengeCanvas.")]
        [SerializeField] private TapTarget targetPrefab;

        [Tooltip("Canvas (Screen Space - Overlay) where targets are spawned.")]
        [SerializeField] private Canvas challengeCanvas;

        // ── UI ──────────────────────────────────────────────────────
        [Header("UI")]
        [Tooltip("Timer bar fill image (Image type = Filled). Optional.")]
        [SerializeField] private Image timerBarFill;

        [Tooltip("Root GameObject of the challenge UI (timer bar, background, etc.). Activated/deactivated.")]
        [SerializeField] private GameObject challengeUIRoot;

        // ── Target Placement ────────────────────────────────────────
        [Header("Target Placement")]
        [Tooltip("RectTransform defining the area where targets can spawn. Targets stay inside this rect. If null, falls back to the full canvas.")]
        [SerializeField] private RectTransform spawnArea;

        [Tooltip("Inward padding from the spawn area edges (pixels). Prevents targets from touching the border.")]
        [SerializeField] private float edgePadding = 60f;

        // ── Timing ──────────────────────────────────────────────────
        [Header("Timing")]
        [Tooltip("Small delay between targets (real seconds).")]
        [SerializeField] private float delayBetweenTargets = 0.08f;

        // ── Color Feedback ──────────────────────────────────────────
        [Header("Color Feedback")]
        [SerializeField] private Color safeColor = Color.green;
        [SerializeField] private Color dangerColor = Color.red;

        // ── VFX ─────────────────────────────────────────────────────
        [Header("VFX")]
        [Tooltip("UI burst prefab with TapBurstEffect + Image. Spawned on the canvas at the tap location.")]
        [SerializeField] private TapBurstEffect tapBurstPrefab;

        // ── Runtime State ───────────────────────────────────────────
        public bool IsActive { get; private set; }

        private ChallengeDifficulty _difficulty;
        private int _targetsRemaining;
        private float _timer;
        private float _maxTime;
        private TapTarget _currentTarget;
        private RectTransform _canvasRect;
        private Camera _uiCamera;

        // Cache the timescale we had before entering the challenge so we can restore it
        private float _previousTimeScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (challengeCanvas != null)
                _canvasRect = challengeCanvas.GetComponent<RectTransform>();

            // Cache camera for world-position conversions (overlay canvas uses null)
            if (challengeCanvas != null && challengeCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _uiCamera = challengeCanvas.worldCamera;

            // Hide UI at start
            if (challengeUIRoot != null) challengeUIRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsActive) return;

            // Tick timer with unscaled time (immune to timeScale)
            _timer -= Time.unscaledDeltaTime;

            // Update timer bar
            if (timerBarFill != null)
                timerBarFill.fillAmount = Mathf.Clamp01(_timer / _maxTime);

            // Update current target color based on global remaining time
            if (_currentTarget != null)
            {
                float normalised = Mathf.Clamp01(_timer / _maxTime);
                Color col = Color.Lerp(dangerColor, safeColor, normalised);
                _currentTarget.SetColor(col);
            }

            // Time ran out → fail
            if (_timer <= 0f)
            {
                EndChallenge(success: false);
            }
        }

        // ── Public API ──────────────────────────────────────────────

        /// <summary>
        /// Start a tap challenge with the given difficulty preset.
        /// </summary>
        public void StartChallenge(ChallengeDifficulty difficulty)
        {
            if (IsActive) return;
            if (difficulty == null)
            {
                Debug.LogError("TapChallengeManager: difficulty is null.");
                return;
            }

            _difficulty = difficulty;
            _maxTime = difficulty.duration;
            _timer = _maxTime;
            _targetsRemaining = difficulty.targetCount;

            IsActive = true;

            // Enter slow motion
            _previousTimeScale = Time.timeScale;
            Time.timeScale = difficulty.slowMotionScale;
            Time.fixedDeltaTime = 0.02f * difficulty.slowMotionScale;

            // Show UI
            if (challengeUIRoot != null) challengeUIRoot.SetActive(true);

            // Spawn first target
            SpawnNextTarget();
        }

        // ── Internal ────────────────────────────────────────────────

        private void SpawnNextTarget()
        {
            if (!IsActive) return;

            if (_targetsRemaining <= 0)
            {
                EndChallenge(success: true);
                return;
            }

            if (targetPrefab == null || _canvasRect == null)
            {
                Debug.LogError("TapChallengeManager: targetPrefab or challengeCanvas not assigned.");
                EndChallenge(success: false);
                return;
            }

            // Instantiate (or reuse) target
            if (_currentTarget == null)
                _currentTarget = Instantiate(targetPrefab, _canvasRect);

            Vector2 pos = GetRandomCanvasPosition();
            _currentTarget.Init(pos, OnTargetTapped);

            // Set initial color
            float normalised = Mathf.Clamp01(_timer / _maxTime);
            _currentTarget.SetColor(Color.Lerp(dangerColor, safeColor, normalised));
        }

        private void OnTargetTapped()
        {
            if (!IsActive) return;

            _targetsRemaining--;

            // Kill current target visual
            if (_currentTarget != null) _currentTarget.Kill();

            // Feedback: screen shake (unscaled)
            if (ChallengeScreenShake.Instance != null)
                ChallengeScreenShake.Instance.Shake();

            // Feedback: VFX
            SpawnTapVFX();

            // Feedback: SFX
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioEventSFX.Collect);

            if (_targetsRemaining <= 0)
            {
                EndChallenge(success: true);
                return;
            }

            // Small delay before next target
            if (delayBetweenTargets > 0f)
                StartCoroutine(DelayedSpawn());
            else
                SpawnNextTarget();
        }

        private IEnumerator DelayedSpawn()
        {
            float waited = 0f;
            while (waited < delayBetweenTargets)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            SpawnNextTarget();
        }

        private void EndChallenge(bool success)
        {
            IsActive = false;

            // Clean up target
            if (_currentTarget != null)
            {
                _currentTarget.Kill();
                Destroy(_currentTarget.gameObject);
                _currentTarget = null;
            }

            // Restore timescale
            Time.timeScale = _previousTimeScale;
            Time.fixedDeltaTime = 0.02f * _previousTimeScale;

            // Hide UI
            if (challengeUIRoot != null) challengeUIRoot.SetActive(false);

            if (success)
            {
                // Reward: base + time bonus scaled by remaining time
                float remainingNormalised = Mathf.Clamp01(_timer / _maxTime);
                int reward = _difficulty.baseReward +
                             Mathf.RoundToInt(_difficulty.timeBonusMultiplier * remainingNormalised);

                // Award coins
                if (GameController.Instance != null)
                {
                    int current = GameController.Instance.GetPoints();
                    GameController.Instance.SetPoints(current + reward);
                }

                // Floating reward text above player
                if (GameController.Instance != null &&
                    GameController.Instance.PlayerController != null)
                {
                    Vector3 playerPos = GameController.Instance.PlayerController.transform.position;
                    FloatingPointTextSpawner.SpawnFloatingPointText(reward, playerPos + Vector3.up * 2f);
                }
            }
            else
            {
                // Failed → game over
                if (GameController.Instance != null)
                    GameController.Instance.GameOver();
            }
        }

        // ── Helpers ─────────────────────────────────────────────────

        private Vector2 GetRandomCanvasPosition()
        {
            // Use the explicit spawn area if assigned, otherwise fall back to canvas
            RectTransform area = spawnArea != null ? spawnArea : _canvasRect;
            if (area == null) return Vector2.zero;

            Rect rect = area.rect;
            float minX = rect.xMin + edgePadding;
            float maxX = rect.xMax - edgePadding;
            float minY = rect.yMin + edgePadding;
            float maxY = rect.yMax - edgePadding;

            // Safety: if padding is larger than the area, clamp to center
            if (minX > maxX) minX = maxX = rect.center.x;
            if (minY > maxY) minY = maxY = rect.center.y;

            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);

            // If spawn area is a child of the canvas (not the canvas itself),
            // convert from the area's local space to the canvas's local space
            if (area != _canvasRect && _canvasRect != null)
            {
                Vector3 worldPoint = area.TransformPoint(new Vector3(x, y, 0f));
                Vector2 canvasLocal;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    RectTransformUtility.WorldToScreenPoint(_uiCamera, worldPoint),
                    _uiCamera,
                    out canvasLocal);
                return canvasLocal;
            }

            return new Vector2(x, y);
        }

        private void SpawnTapVFX()
        {
            if (_currentTarget == null || tapBurstPrefab == null || _canvasRect == null) return;

            RectTransform targetRect = _currentTarget.GetComponent<RectTransform>();
            if (targetRect == null) return;

            // Spawn UI burst at the target's position on the canvas
            TapBurstEffect burst = Instantiate(tapBurstPrefab, _canvasRect);
            burst.GetComponent<RectTransform>().anchoredPosition = targetRect.anchoredPosition;

            // Use the current timer color so the burst matches the target
            float normalised = Mathf.Clamp01(_timer / _maxTime);
            Color col = Color.Lerp(dangerColor, safeColor, normalised);
            burst.Play(col);
        }
    }
}
