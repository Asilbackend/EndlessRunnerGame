using UnityEngine;

/// <summary>
/// Simple touch feedback effect that updates based on swipe movement.
/// Changes color when swipe threshold is reached.
/// Attach this to a GameObject with a SpriteRenderer or Image component.
/// </summary>
public class TouchFeedbackEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Starting scale")]
    [SerializeField] private float startScale = 0.5f;
    [Tooltip("Ending scale when threshold is reached")]
    [SerializeField] private float endScale = 1.2f;
    [Tooltip("Color when swipe starts")]
    [SerializeField] private Color startColor = Color.white;
    [Tooltip("Color when swipe threshold is reached")]
    [SerializeField] private Color thresholdReachedColor = Color.green;
    [Tooltip("Time to fade out after swipe ends")]
    [SerializeField] private float fadeOutDuration = 0.2f;

    private SpriteRenderer _spriteRenderer;
    private UnityEngine.UI.Image _image;
    private float _swipeThreshold = 50f;
    private float _currentSwipeDistance = 0f;
    private bool _movementTriggered = false;
    private bool _isFadingOut = false;
    private float _fadeOutTime = 0f;
    private Color _originalColor;

    public void Initialize(float swipeThreshold)
    {
        _swipeThreshold = swipeThreshold;
        _currentSwipeDistance = 0f;
        _movementTriggered = false;
        _isFadingOut = false;
        _fadeOutTime = 0f;
    }

    public void UpdateSwipeProgress(float swipeDistance)
    {
        if (_isFadingOut)
            return;

        _currentSwipeDistance = swipeDistance;
        float progress = Mathf.Clamp01(swipeDistance / _swipeThreshold);

        // Update scale based on progress
        float scale = Mathf.Lerp(startScale, endScale, progress);
        transform.localScale = Vector3.one * scale;

        // Only change color if movement was actually triggered
        Color currentColor = _movementTriggered ? thresholdReachedColor : startColor;
        currentColor.a = 1f;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = currentColor;
        }
        else if (_image != null)
        {
            _image.color = currentColor;
        }
    }

    public void OnMovementTriggered()
    {
        _movementTriggered = true;
        
        // Immediately update color to show movement was triggered
        Color currentColor = thresholdReachedColor;
        currentColor.a = 1f;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = currentColor;
        }
        else if (_image != null)
        {
            _image.color = currentColor;
        }
    }

    public void StartFadeOut()
    {
        if (_isFadingOut)
            return;

        _isFadingOut = true;
        _fadeOutTime = 0f;

        // Store current color for fade out
        if (_spriteRenderer != null)
        {
            _originalColor = _spriteRenderer.color;
        }
        else if (_image != null)
        {
            _originalColor = _image.color;
        }
    }

    private void Start()
    {
        // Try to get SpriteRenderer first (for world space)
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // If no SpriteRenderer, try Image (for UI)
        if (_spriteRenderer == null)
        {
            _image = GetComponent<UnityEngine.UI.Image>();
        }

        if (_spriteRenderer == null && _image == null)
        {
            Debug.LogWarning("TouchFeedbackEffect: No SpriteRenderer or Image component found. Destroying.");
            Destroy(gameObject);
            return;
        }

        // Set initial color and scale
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = startColor;
        }
        else if (_image != null)
        {
            _image.color = startColor;
        }

        transform.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        if (_isFadingOut)
        {
            _fadeOutTime += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeOutTime / fadeOutDuration);

            // Fade out
            Color currentColor = _originalColor;
            currentColor.a = Mathf.Lerp(1f, 0f, t);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = currentColor;
            }
            else if (_image != null)
            {
                _image.color = currentColor;
            }

            // Destroy when fade out completes
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
