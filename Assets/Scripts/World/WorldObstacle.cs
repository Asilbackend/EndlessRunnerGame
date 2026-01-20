using UnityEngine;
using UI;
using UnityEngine.SceneManagement;
using Utilities;


namespace World
{
    [RequireComponent(typeof(Collider))]
    public class WorldObstacle : MonoBehaviour, IWorldObject
    {
        [Header("Obstacle Settings")]
        [SerializeField] private bool destroyOnDespawn = false;
        [SerializeField] private float damage = 1;
        [SerializeField] private bool isOppositeDirection = false; // If true, moves backward and rotates 180 degrees

        public bool IsOppositeDirection => isOppositeDirection;
        
        public void SetOppositeDirection(bool value)
        {
            isOppositeDirection = value;
            if (value)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        [Header("Collision Detection")]
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private LayerMask obstacleLayerMask = -1; // Check all layers by default

        private float activationDistance = 60f;
        private WorldChunk _parentChunk;
        private bool isActive = true;
        private Collider _collider;

        private float _moveSpeed = 0f;
        private float _originalMoveSpeed = 0f; // store configured speed to restore after reverse
        private ObjectData _configuredObjectData;
        private bool _isMoving;
        private bool _isDynamic;
        private bool _isBlocked = false;
        private bool _wasMovingBeforePause = false;
        private bool _isPaused = false;
        private bool _isReversed = false;

        // For opposite direction obstacles
        private float _startMovingAtMeters = 0f; // n meters - when obstacle starts moving
        private float _chunkStartZ = 0f;
        private bool _forceStartMoving = false;

        // Formula = _moveSpeed * (activationDistance / (worldSpeed - _moveSpeed))

        public void ConfigureFromObjectData(ObjectData data, float? overrideSpeed = null)
        {
            if (data == null) return;

            _configuredObjectData = data;

            damage = data.damage;
            // If overrideSpeed is provided, use it; otherwise use data.speed
            // This allows us to overwrite the obstacle's default speed
            float speedToUse = overrideSpeed.HasValue ? overrideSpeed.Value : data.speed;
            
            if (isOppositeDirection)
            {
                // For opposite direction, use negative speed (moves backward)
                _moveSpeed = -Mathf.Abs(speedToUse);
                // Rotate 180 degrees around Y axis
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                _moveSpeed = speedToUse;
            }
            
            _originalMoveSpeed = _moveSpeed;
            _isDynamic = Mathf.Abs(_moveSpeed) > 0f;
        }

        public void SetActivationParameters(float chunkStartZ, float startMovingAtMeters)
        {
            _chunkStartZ = chunkStartZ;
            _startMovingAtMeters = startMovingAtMeters;
        }

        public void ForceStartMoving()
        {
            _forceStartMoving = true;
            _isMoving = true;
        }

        private void Start()
        {
            _parentChunk = GetComponentInParent<WorldChunk>();
            if (_parentChunk != null)
            {
                _parentChunk.AddWorldObject(this);
            }

            _collider = GetComponent<Collider>();
            if (_collider != null && !_collider.isTrigger)
            {
                _collider.isTrigger = true;
            }
            
            // Only configure from data if not already configured (to preserve override speed)
            if (_configuredObjectData != null && _moveSpeed == 0f && !_isDynamic)
            {
                ConfigureFromObjectData(_configuredObjectData);
            }
        }

        private void Update()
        {
            if (!isActive || _isPaused) return;

            if (_isDynamic)
            {
                // Handle opposite direction obstacles
                if (isOppositeDirection)
                {
                    // If forced to start moving, skip player check
                    if (_forceStartMoving)
                    {
                        _isMoving = true;
                    }
                    else
                    {
                        var player = GameController.Instance != null ? GameController.Instance.PlayerController : null;
                        if (player != null && !_isMoving)
                        {
                            // Check if player has reached the n meter mark in the chunk
                            float playerZ = player.transform.position.z;
                            float distanceIntoChunk = playerZ - _chunkStartZ;
                            
                            if (distanceIntoChunk >= _startMovingAtMeters)
                            {
                                _isMoving = true;
                            }
                        }
                    }
                }
                else
                {
                    // Normal forward direction obstacles
                    var player = GameController.Instance != null ? GameController.Instance.PlayerController : null;
                    if (player != null && !_isMoving)
                    {
                        float distanceAhead = transform.position.z - player.transform.position.z;
                        if (distanceAhead <= activationDistance)
                        {
                            _isMoving = true;
                        }
                    }
                }

                // Check for obstacles in front before moving
                if (_isMoving && !_isBlocked)
                {
                    _isBlocked = CheckForObstacleInFront();
                    if (!_isBlocked)
                    {
                        MoveWithWorld();
                    }
                }
            }
        }

        private bool CheckForObstacleInFront()
        {
            Vector3 origin = transform.position + new Vector3(0, 0.5f, 0);
            Vector3 direction = isOppositeDirection ? Vector3.back : Vector3.forward;
            
            // Use Raycast to detect obstacles in front (single line cast)
            RaycastHit hit;
            bool hasHit = Physics.Raycast(
                origin,
                direction,
                out hit,
                lookAheadDistance,
                obstacleLayerMask
            );

            if (hasHit)
            {
                if (hit.collider.gameObject == gameObject)
                {
                    return false;
                }

                // Check if it's another obstacle (static or dynamic)
                WorldObstacle otherObstacle = hit.collider.GetComponent<WorldObstacle>();
                if (otherObstacle != null)
                {
                    return true;
                }

                // Also check for any collider that's not a trigger (static obstacles)
                if (!hit.collider.isTrigger)
                {
                    return true;
                }
            }

            return false;
        }

        public void MoveWithWorld()
        {
            if (isOppositeDirection)
            {
                transform.Translate(Vector3.back * Mathf.Abs(_moveSpeed) * Time.deltaTime, Space.World);
            }
            else
            {
                transform.Translate(Vector3.forward * _moveSpeed * Time.deltaTime, Space.World);
            }
        }

        public void OnCollided()
        {
            if (GameController.Instance != null)
            {
                GameController.Instance.GameOver();
            }
        }

        public void ResetMoving()
        {
            _isMoving = false;
            _isBlocked = false;
            _forceStartMoving = false;
        }

        public void Pause()
        {
            if (_isDynamic)
            {
                _wasMovingBeforePause = _isMoving;
                _isPaused = true;
                _isMoving = false;
            }
        }

        public void Resume()
        {
            if (_isDynamic)
            {
                StopReverse();
                _isPaused = false;
                _isMoving = _wasMovingBeforePause;
            }
        }

        public void Reverse()
        {
            if (!_isDynamic) return;
            if (_isReversed) return;
            _isReversed = true;
            _moveSpeed = -Mathf.Abs(_originalMoveSpeed) * GameController.Instance.ReverseMultiplier;
            _isPaused = false;
            _isMoving = true;
        }

        public void StopReverse()
        {
            if (!_isDynamic) return;
            if (!_isReversed) return;
            _isReversed = false;
            _moveSpeed = _originalMoveSpeed;
            _isMoving = _wasMovingBeforePause;
        }

        public void OnDespawn()
        {
            isActive = false;

            if (destroyOnDespawn)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            if (other.CompareTag("Player"))
            {
                // Spawn impact particle effect at collision point using centralized system
                Vector3 impactPosition = other.ClosestPoint(transform.position);
                ParticleEffectSpawner.SpawnParticleEffect(ParticleEffectType.ObstacleImpact, impactPosition);

                OnCollided();
                if (destroyOnDespawn) Destroy(gameObject);

                // camera impact effect
                if (PlayerCameraController.Instance != null)
                {
                    PlayerCameraController.Instance.Impact();
                }
            }
        }

        private void OnDestroy()
        {
            if (_parentChunk != null)
            {
                _parentChunk.RemoveWorldObject(this);
            }
        }
    }
}

