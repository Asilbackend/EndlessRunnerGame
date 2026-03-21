using System.Collections;
using TMPro;
using UnityEngine;
using Utilities;

namespace UI
{
    /// <summary>
    /// Attach to any GameObject that shows a coin or gem balance.
    /// Call SetValue(newAmount) instead of setting the text directly.
    /// Plays a scale-punch + color-flash animation on gain or loss.
    ///
    /// Requires a TMP_Text on this GameObject or assigned via Inspector.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CurrencyDisplay : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text label;

        [Header("Gain Animation")]
        [SerializeField] private Color gainColor  = new Color(0.30f, 1.00f, 0.45f); // bright green
        [SerializeField] private float gainScale  = 1.40f;
        [SerializeField] private float gainGrowDur   = 0.10f;
        [SerializeField] private float gainShrinkDur = 0.15f;

        [Header("Loss Animation")]
        [SerializeField] private Color lossColor  = new Color(1.00f, 0.30f, 0.30f); // bright red
        [SerializeField] private float lossScale  = 1.25f;
        [SerializeField] private float lossGrowDur   = 0.08f;
        [SerializeField] private float lossShrinkDur = 0.18f;

        [Header("Color flash")]
        [SerializeField] private float flashHoldDur  = 0.12f; // how long tint stays at peak
        [SerializeField] private float flashFadeDur  = 0.20f; // how long it fades back to white

        private RectTransform _rt;
        private Color         _baseColor = Color.white;
        private int           _currentValue;
        private Coroutine     _scaleCoroutine;
        private Coroutine     _colorCoroutine;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (label == null) label = GetComponent<TMP_Text>();
            if (label != null) _baseColor = label.color;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Update the displayed value and animate if it changed.
        /// </summary>
        public void SetValue(int newValue)
        {
            int previous = _currentValue;
            _currentValue = newValue;

            if (label != null)
                label.text = NumberFormatter.Format(newValue);

            if (newValue > previous)
                Animate(gainColor, gainScale, gainGrowDur, gainShrinkDur);
            else if (newValue < previous)
                Animate(lossColor, lossScale, lossGrowDur, lossShrinkDur);
            // equal → no animation (e.g. first display on Start)
        }

        /// <summary>
        /// Set the value silently on first display (no animation).
        /// </summary>
        public void InitValue(int value)
        {
            _currentValue = value;
            if (label != null)
                label.text = NumberFormatter.Format(value);
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void Animate(Color flashColor, float peakScale, float growDur, float shrinkDur)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            if (_colorCoroutine != null) StopCoroutine(_colorCoroutine);

            _scaleCoroutine = StartCoroutine(ScalePunch(peakScale, growDur, shrinkDur));
            _colorCoroutine = StartCoroutine(ColorFlash(flashColor));
        }

        private IEnumerator ScalePunch(float peakScale, float growDur, float shrinkDur)
        {
            Vector3 peak = new Vector3(peakScale, peakScale, 1f);

            // Grow
            float t = 0f;
            while (t < growDur)
            {
                t += Time.unscaledDeltaTime;
                _rt.localScale = Vector3.Lerp(Vector3.one, peak, t / growDur);
                yield return null;
            }

            // Shrink back
            t = 0f;
            while (t < shrinkDur)
            {
                t += Time.unscaledDeltaTime;
                _rt.localScale = Vector3.Lerp(peak, Vector3.one, t / shrinkDur);
                yield return null;
            }

            _rt.localScale = Vector3.one;
            _scaleCoroutine = null;
        }

        private IEnumerator ColorFlash(Color flashColor)
        {
            if (label == null) yield break;

            // Instant tint to flash color
            label.color = flashColor;

            // Hold briefly
            yield return new WaitForSecondsRealtime(flashHoldDur);

            // Fade back to base color
            float t = 0f;
            while (t < flashFadeDur)
            {
                t += Time.unscaledDeltaTime;
                label.color = Color.Lerp(flashColor, _baseColor, t / flashFadeDur);
                yield return null;
            }

            label.color = _baseColor;
            _colorCoroutine = null;
        }
    }
}
