using UnityEngine;
using UnityEngine.UI;

namespace Challenge
{
    /// <summary>
    /// Pure UI burst effect — scale punch + fade out.
    /// Spawn on the challenge canvas at the target's position. Self-destructs when done.
    /// All timing uses unscaled time so it plays at full speed during slow-motion.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TapBurstEffect : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private float startScale = 0.5f;
        [SerializeField] private float peakScale = 2.5f;

        private Image _image;
        private RectTransform _rect;
        private float _elapsed;
        private Color _baseColor;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect = GetComponent<RectTransform>();
        }

        /// <summary>Kick off the burst animation with a given color.</summary>
        public void Play(Color color)
        {
            _baseColor = color;
            _image.color = color;
            _rect.localScale = Vector3.one * startScale;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);

            // Scale: ease out — fast expand, then slow
            float scale = Mathf.Lerp(startScale, peakScale, 1f - (1f - t) * (1f - t));
            _rect.localScale = Vector3.one * scale;

            // Fade out alpha
            Color c = _baseColor;
            c.a = 1f - t;
            _image.color = c;

            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
