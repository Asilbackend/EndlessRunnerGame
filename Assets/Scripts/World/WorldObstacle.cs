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

        [Header("Point Trigger")]
        [SerializeField] private Vector3 pointTriggerCenter = new Vector3(0, 1f, 0);
        [SerializeField] private Vector3 pointTriggerSize = new Vector3(3f, 2f, 2f);
        [SerializeField] private float pointTriggerZ = 2;
        [SerializeField] private GameObject pointBordersPrefab; // Visual borders prefab to show jump area

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
        private bool _wasMovingBeforeReverse = false;
        private bool _isPaused = false;
        private bool _isReversed = false;

        // For opposite direction obstacles
        private float _startMovingAtMeters = 0f; // n meters - when obstacle starts moving
        private float _chunkStartZ = 0f;
        private bool _forceStartMoving = false;
        private ObstaclePointTrigger _pointTrigger;
        private Collider _pointTriggerCollider; // Cached reference for performance

        // Formula = _moveSpeed * (activationDistance / (worldSpeed - _moveSpeed))

        public void ConfigureFromObjectData(ObjectData data, float? overrideSpeed = null)
        {
            if (data == null) return;

            _configuredObjectData = data;

            damage = data.damage;
            // Speed is set by the caller (e.g. ObjectPlacer); no speed on scriptable object
            float speedToUse = overrideSpeed ?? 0f;
            
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
            
            // Create point trigger for dynamic obstacles
            if (_isDynamic)
            {
                CreatePointTrigger();
            }
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
            
            // Ensure trigger is created if obstacle is dynamic
            if (_isDynamic && _pointTrigger == null)
            {
                CreatePointTrigger();
            }
        }
        
        private void CreatePointTrigger()
        {
            // Don't create if already exists
            if (_pointTrigger != null) return;
            
            // Check if trigger already exists as child (e.g. baked into the prefab)
            _pointTrigger = GetComponentInChildren<ObstaclePointTrigger>();
            if (_pointTrigger != null)
            {
                // Also cache the collider so OnTriggerEnter's bounds-check guard works correctly
                // for prefab-embedded triggers (without this, _pointTriggerCollider stays null and
                // the guard is silently skipped, allowing point-trigger touches to deal damage).
                _pointTriggerCollider = _pointTrigger.GetComponent<Collider>();
                return;
            }
            
            // Create trigger GameObject
            GameObject triggerObject = new GameObject("PointTrigger");
            triggerObject.transform.SetParent(transform);
            triggerObject.transform.localRotation = Quaternion.identity;
            
            // Add collider component
            BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            
            // Cache the collider reference for performance
            _pointTriggerCollider = triggerCollider;
            
            // Position trigger based on obstacle direction using serialized fields
            Vector3 triggerPosition = pointTriggerCenter;
            
            if (isOppositeDirection)
            {
                // For opposite direction obstacles, place trigger in front (positive Z)
                triggerPosition.z = pointTriggerCenter.z + pointTriggerZ;
            }
            else
            {
                // For normal dynamic obstacles, place trigger at the back (negative Z)
                triggerPosition.z = pointTriggerCenter.z - pointTriggerZ;
            }
            
            triggerObject.transform.localPosition = triggerPosition;
            
            // Set trigger size using serialized field
            triggerCollider.size = pointTriggerSize;
            triggerCollider.center = Vector3.zero; // Center is relative to the trigger object
            
            // Add ObstaclePointTrigger component
            _pointTrigger = triggerObject.AddComponent<ObstaclePointTrigger>();
            
            // Instantiate visual borders prefab as child of the trigger
            if (pointBordersPrefab != null)
            {
                // First, instantiate as child of the trigger (parent)
                GameObject bordersInstance = Instantiate(pointBordersPrefab, triggerObject.transform, false);
                bordersInstance.transform.localRotation = Quaternion.identity;
                
                float bordersLocalZ = triggerPosition.z < 0 ? -0.5f : 0.5f;
                bordersInstance.transform.localPosition = new Vector3(
                    pointTriggerCenter.x,
                    -0.95f,
                    bordersLocalZ
                );
                
                // Scale: use pointTriggerSize values, with z + 1
                float zSizeWithExtra = pointTriggerSize.z + 1f; // Add 1 to z size
                Vector3 scaleFactor = new Vector3(
                    pointTriggerSize.x,
                    pointTriggerSize.y,
                    zSizeWithExtra
                );
                bordersInstance.transform.localScale = scaleFactor;
            }
        }

        private void Update()
        {
            if (!isActive || _isPaused) return;

            if (_isDynamic)
            {
                // When reversed, skip activation checks and just move (time is going backwards)
                if (!_isReversed)
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
                }

                // Check for obstacles in front before moving (skip when reversed - time is going backwards).
                // Re-evaluate _isBlocked every frame so that if a blocking obstacle moves away or is
                // despawned the obstacle behind it can resume immediately instead of staying frozen.
                if (_isMoving)
                {
                    if (!_isReversed)
                    {
                        _isBlocked = CheckForObstacleInFront();
                    }
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
                // For opposite direction obstacles:
                // Normal: negative speed, moves backward (Vector3.back)
                // Reversed: positive speed, moves forward (Vector3.forward) to reverse time
                if (_isReversed)
                {
                    transform.Translate(Vector3.forward * _moveSpeed * Time.deltaTime, Space.World);
                }
                else
                {
                    transform.Translate(Vector3.back * Mathf.Abs(_moveSpeed) * Time.deltaTime, Space.World);
                }
            }
            else
            {
                // For normal obstacles:
                // Normal: positive speed, moves forward (Vector3.forward)
                // Reversed: negative speed, moves backward (Vector3.back) to reverse time
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

            // Reset point trigger if it exists
            if (_pointTrigger != null)
            {
                _pointTrigger.ResetTrigger();
            }
        }

        /// <summary>
        /// Full state reset used after a checkpoint rewind. Unlike ResetMoving() (which is
        /// called on chunk recycle), this also clears ALL reverse/pause bookkeeping so that
        /// the Resume() call that follows in the coroutine cannot accidentally re-enable
        /// movement. The caller is responsible for teleporting the transform back to the
        /// designed start position before calling this.
        ///
        /// Key mechanic: setting _isReversed = false makes StopReverse() inside Resume()
        /// a no-op, so Resume() ends with _isMoving = _wasMovingBeforePause = false. ✓
        /// </summary>
        public void ResetForCheckpoint()
        {
            _isMoving = false;
            _isBlocked = false;
            _forceStartMoving = false;
            _wasMovingBeforePause = false;      // prevents Resume() from re-enabling motion
            _wasMovingBeforeReverse = false;    // prevents StopReverse() from re-enabling motion
            _isPaused = false;
            _isReversed = false;                // StopReverse() early-returns when false → no-op
            _moveSpeed = _originalMoveSpeed;    // restore speed (was set to reverse speed)

            if (_pointTrigger != null)
            {
                _pointTrigger.ResetTrigger();
            }
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

            // Save whether this obstacle was already moving BEFORE the reverse begins.
            // This is separate from _wasMovingBeforePause so that a Pause() called
            // while reversing does not overwrite the pre-activation state.
            _wasMovingBeforeReverse = _isMoving;

            // For opposite direction obstacles, reverse means moving forward (positive speed)
            // For normal obstacles, reverse means moving backward (negative speed)
            if (isOppositeDirection)
            {
                _moveSpeed = Mathf.Abs(_originalMoveSpeed) * GameController.Instance.ReverseMultiplier;
            }
            else
            {
                _moveSpeed = -Mathf.Abs(_originalMoveSpeed) * GameController.Instance.ReverseMultiplier;
            }

            _isPaused = false;
            _isMoving = true;
        }

        public void StopReverse()
        {
            if (!_isDynamic) return;
            if (!_isReversed) return;
            _isReversed = false;
            _moveSpeed = _originalMoveSpeed;
            // Restore to the state before the reverse started, NOT _wasMovingBeforePause.
            // Using _wasMovingBeforePause here would incorrectly activate obstacles that
            // had not yet reached their activation distance (because Reverse() sets
            // _isMoving = true, which then gets captured into _wasMovingBeforePause by
            // the subsequent Pause() call in the checkpoint-reset coroutine).
            _isMoving = _wasMovingBeforeReverse;
        }

        public void OnDespawn()
        {
            isActive = false;
            
            // Reset point trigger if it exists
            if (_pointTrigger != null)
            {
                _pointTrigger.ResetTrigger();
            }

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
                // Check if player is actually touching the obstacle's main collider, not just the point trigger
                if (_collider != null && _pointTriggerCollider != null)
                {
                    Bounds obstacleBounds = _collider.bounds;
                    Bounds playerBounds = other.bounds;
                    
                    if (!obstacleBounds.Intersects(playerBounds))
                    {
                        return;
                    }
                }
                
                Vector3 impactPosition = other.ClosestPoint(transform.position);
                ParticleEffectSpawner.SpawnParticleEffect(ParticleEffectType.ObstacleImpact, impactPosition);

                // Stop player particle systems
                if (GameController.Instance != null && GameController.Instance.PlayerController != null)
                {
                    GameController.Instance.PlayerController.StopParticleSystems();
                }

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

