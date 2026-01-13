using UnityEngine;

namespace World
{
    [RequireComponent(typeof(Collider))]
    public class WorldCollectible : MonoBehaviour, IWorldObject
    {
        [Header("Collectible Settings")]
        [SerializeField] private int scoreValue = 10;
        [SerializeField] private float rotationSpeed = 90f;
        
        private WorldChunk _parentChunk;
        private bool _isActive = true;
        private bool _isCollected = false;
        private bool _isPaused = false;

        // New fields to support reverse behavior
        private float _originalRotationSpeed = 0f;
        private bool _isReversed = false;
        
        private void Awake()
        {
            _parentChunk = GetComponentInParent<WorldChunk>();
            if (_parentChunk != null)
            {
                _parentChunk.AddWorldObject(this);
            }

            _originalRotationSpeed = rotationSpeed;
        }
        
        private void Update()
        {
            if (_isActive && !_isCollected)
            {
                if (!_isPaused)
                    transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
        
        public void MoveWithWorld()
        {
            return;
        }
        
        public void OnDespawn()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }
        
        public void OnCollided()
        {
            if (_isCollected) return;
            
            _isCollected = true;

            if (GameController.Instance != null)
            {
                GameController.Instance.SetCoins(GameController.Instance.GameCoins + scoreValue);
            }
            
            OnDespawn();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;
            
            if (other.CompareTag("Player"))
            {
                OnCollided();
            }
        }
        
        private void OnDestroy()
        {
            if (_parentChunk != null)
            {
                _parentChunk.RemoveWorldObject(this);
            }
        }

        public void ResetCollectible()
        {
            _isActive = true;
            _isCollected = false;
            _isPaused = false;
            _isReversed = false;
            
            rotationSpeed = _originalRotationSpeed;
            
            gameObject.SetActive(true);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            if (_parentChunk == null)
            {
                _parentChunk = GetComponentInParent<WorldChunk>();
            }

            if (_parentChunk != null)
            {
                _parentChunk.AddWorldObject(this);
            }
        }
        
        public void Pause()
        {
            _isPaused = true;
        }
        
        public void Resume()
        {
            StopReverse();
            _isPaused = false;
        }

        public void Reverse()
        {
            if (_isReversed) return;
            _isReversed = true;
            _isPaused = false;
            rotationSpeed = -Mathf.Abs(_originalRotationSpeed) * GameController.Instance.ReverseMultiplier;
        }

        public void StopReverse()
        {
            if (!_isReversed) return;
            _isReversed = false;
            rotationSpeed = _originalRotationSpeed;
        }
    }
}

