using TMPro;
using UnityEngine;
using System.Collections;

namespace UI
{
    public class FloatingPointText : MonoBehaviour
    {
        [Header("Text Settings")]
        [SerializeField] private Color textColor = Color.yellow;
        [SerializeField] private int fontSize = 60;
        [SerializeField] private string prefix = "+";
        
        [Header("Animation")]
        [SerializeField] private float floatSpeed = 200f;
        [SerializeField] private float lifetime = 1.5f;
        [SerializeField] private AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private float horizontalRandomRange = 50f; // Random horizontal movement range
        
        private TextMeshProUGUI _textMesh;
        private RectTransform _rectTransform;
        private Vector2 _startPosition;
        private Coroutine _animationCoroutine;
        private float _randomHorizontalOffset;

        private void Awake()
        {
            // Get or create TextMeshProUGUI
            _textMesh = GetComponent<TextMeshProUGUI>();
            if (_textMesh == null)
            {
                _textMesh = gameObject.AddComponent<TextMeshProUGUI>();
            }
            
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            // Setup defaults
            _textMesh.alignment = TextAlignmentOptions.Center;
            _textMesh.fontSize = fontSize;
            _textMesh.color = textColor;
        }

        public void Show(int points)
        {
            // Stop any existing animation
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            
            // Set text
            _textMesh.text = prefix + points;
            _textMesh.color = textColor;
            
            // Store start position
            _startPosition = _rectTransform.anchoredPosition;
            
            // Generate random horizontal offset for this instance
            _randomHorizontalOffset = Random.Range(-horizontalRandomRange, horizontalRandomRange);
            
            // Start animation
            gameObject.SetActive(true);
            _animationCoroutine = StartCoroutine(Animate());
        }
        
        public void ShowAtWorldPosition(int points, Vector3 worldPosition)
        {
            // Find or get canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            
            if (canvas == null)
            {
                Debug.LogError("No Canvas found! FloatingPointText needs a Canvas in the scene.");
                return;
            }
            
            // Make sure we're a child of canvas
            transform.SetParent(canvas.transform, false);
            
            // Convert world position to screen position
            Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.GetComponent<RectTransform>(),
                    screenPos,
                    canvas.worldCamera,
                    out Vector2 localPoint
                );
                _rectTransform.anchoredPosition = localPoint;
            }
            
            Show(points);
        }
        
        public void ShowAtScreenCenter(int points)
        {
            // Find or get canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            
            if (canvas == null)
            {
                Debug.LogError("No Canvas found! FloatingPointText needs a Canvas in the scene.");
                return;
            }
            
            // Make sure we're a child of canvas
            transform.SetParent(canvas.transform, false);
            
            // Set to center
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.anchoredPosition = Vector2.zero;
            
            Show(points);
        }

        private IEnumerator Animate()
        {
            float elapsedTime = 0f;
            float startY = _startPosition.y;
            float startX = _startPosition.x;
            
            while (elapsedTime < lifetime)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / lifetime;
                
                // Animate position using curve
                float curveValue = floatCurve.Evaluate(normalizedTime);
                float newY = startY + (floatSpeed * curveValue);
                
                // Add slight random horizontal movement (sine wave for smooth oscillation)
                float horizontalOffset = _randomHorizontalOffset * Mathf.Sin(normalizedTime * Mathf.PI * 2f);
                float newX = startX + horizontalOffset;
                
                _rectTransform.anchoredPosition = new Vector2(newX, newY);
                
                // Animate fade using curve
                float alpha = fadeCurve.Evaluate(normalizedTime);
                Color color = textColor;
                color.a = alpha;
                _textMesh.color = color;
                
                yield return null;
            }
            
            // Hide when done
            gameObject.SetActive(false);
        }
    }
}
