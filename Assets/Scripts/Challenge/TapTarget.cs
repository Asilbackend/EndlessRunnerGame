using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Challenge
{
    /// <summary>
    /// Individual tap target on the UI canvas.
    /// Handles pointer input, color lerp, and spawn/pop animation.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TapTarget : MonoBehaviour, IPointerClickHandler
    {
        [Header("Animation")]
        [SerializeField] private float spawnDuration = 0.15f;
        [SerializeField] private float minRandomScale = 0.9f;
        [SerializeField] private float maxRandomScale = 1.1f;

        private Image _image;
        private RectTransform _rect;
        private Action _onTapped;

        // Spawn animation state
        private float _spawnTimer;
        private float _targetScale;
        private bool _animating;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Initialise the target at a given anchored position with a tap callback.
        /// </summary>
        public void Init(Vector2 anchoredPos, Action onTapped)
        {
            _onTapped = onTapped;
            _rect.anchoredPosition = anchoredPos;

            // Slight random scale for variety
            _targetScale = UnityEngine.Random.Range(minRandomScale, maxRandomScale);
            _rect.localScale = Vector3.zero;

            _spawnTimer = 0f;
            _animating = true;

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_animating) return;

            // Animate scale-in using unscaled time (immune to slow-motion)
            _spawnTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_spawnTimer / spawnDuration);

            // EaseOutBack for a snappy pop
            float ease = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            _rect.localScale = Vector3.one * (_targetScale * ease);

            if (t >= 1f) _animating = false;
        }

        /// <summary>Set the target's color (called every frame by the manager for the global timer gradient).</summary>
        public void SetColor(Color color)
        {
            if (_image != null)
                _image.color = color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onTapped?.Invoke();
        }

        /// <summary>Hide and clean up when no longer needed.</summary>
        public void Kill()
        {
            _onTapped = null;
            gameObject.SetActive(false);
        }
    }
}
