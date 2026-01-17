using UnityEngine;

namespace World
{
    public class Decoration : MonoBehaviour
    {
        [Header("Decoration Settings")]
        [SerializeField] private float decorationLength = 20f;

        private float _currentZPosition;
        private bool _isActive = false;
        private GameObject _sourcePrefab;

        public GameObject SourcePrefab
        {
            get => _sourcePrefab;
            set => _sourcePrefab = value;
        }

        public float DecorationLength => decorationLength;
        public float StartZ => _currentZPosition;
        public float EndZ => _currentZPosition + decorationLength;
        public bool IsActive => _isActive;

        public void Initialize(float zPosition)
        {
            _currentZPosition = zPosition;
            transform.position = new Vector3(0, 0, zPosition);
            _isActive = true;
            gameObject.SetActive(true);
        }

        public void MoveDecoration(float deltaMovement)
        {
            if (!_isActive) return;

            _currentZPosition -= deltaMovement;
            transform.position = new Vector3(transform.position.x, transform.position.y, _currentZPosition);
        }

        public bool HasPassedPlayer(float playerZPosition, float despawnOffset = 5f)
        {
            return EndZ < playerZPosition - despawnOffset;
        }

        public void ResetDecoration()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }

        // Update internal position tracking without moving transform (for use when parent handles movement)
        public void UpdatePosition(float zPosition)
        {
            _currentZPosition = zPosition;
        }
    }
}
