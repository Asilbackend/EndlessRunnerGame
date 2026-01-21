using UnityEngine;
using UI;

namespace Utilities
{
    public class FloatingPointTextSpawner : MonoBehaviour
    {
        public static FloatingPointTextSpawner Instance { get; private set; }
        
        [Header("Prefab Assignment")]
        [SerializeField] private GameObject floatingPointTextPrefab;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }
        
        public static void SpawnFloatingPointText(int points, Vector3 worldPosition)
        {
            if (Instance == null)
            {
                Debug.LogError("FloatingPointTextSpawner not found in scene! Add it to a GameObject.");
                return;
            }
            
            if (Instance.floatingPointTextPrefab == null)
            {
                Debug.LogError("FloatingPointText prefab is not assigned in FloatingPointTextSpawner!");
                return;
            }
            
            GameObject instance = Instantiate(Instance.floatingPointTextPrefab);
            FloatingPointText floatingText = instance.GetComponent<FloatingPointText>();
            
            if (floatingText != null)
            {
                floatingText.ShowAtWorldPosition(points, worldPosition);
            }
        }
        
        public static void SpawnFloatingPointTextAtScreenCenter(int points)
        {
            if (Instance == null)
            {
                Debug.LogError("FloatingPointTextSpawner not found in scene! Add it to a GameObject.");
                return;
            }
            
            if (Instance.floatingPointTextPrefab == null)
            {
                Debug.LogError("FloatingPointText prefab is not assigned in FloatingPointTextSpawner!");
                return;
            }
            
            GameObject instance = Instantiate(Instance.floatingPointTextPrefab);
            FloatingPointText floatingText = instance.GetComponent<FloatingPointText>();
            
            if (floatingText != null)
            {
                floatingText.ShowAtScreenCenter(points);
            }
        }
    }
}
