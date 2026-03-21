using UnityEngine;
using UI;

namespace Utilities
{
    public class FloatingPointTextSpawner : MonoBehaviour
    {
        public static FloatingPointTextSpawner Instance { get; private set; }

        [SerializeField] private GameObject floatingPointTextPrefab;
        [SerializeField] private Canvas targetCanvas;

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
            if (Instance == this)
                Instance = null;
        }

        public static void SpawnFloatingPointText(int points, Vector3 worldPosition)
        {
            if (Instance == null || Instance.floatingPointTextPrefab == null || Instance.targetCanvas == null)
                return;

            var go = Instantiate(Instance.floatingPointTextPrefab, Instance.targetCanvas.transform);
            go.GetComponent<FloatingPointText>()?.ShowAtWorldPosition(points, worldPosition);
        }

        public static void SpawnFloatingPointTextAtScreenCenter(int points)
        {
            if (Instance == null || Instance.floatingPointTextPrefab == null || Instance.targetCanvas == null)
                return;

            var go = Instantiate(Instance.floatingPointTextPrefab, Instance.targetCanvas.transform);
            go.GetComponent<FloatingPointText>()?.ShowAtScreenCenter(points);
        }
    }
}
