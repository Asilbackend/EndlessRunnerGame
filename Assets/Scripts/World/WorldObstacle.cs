using UnityEngine;
using UI;
using UnityEngine.SceneManagement;
using Utilities;
using Powerup;


namespace World
{
    [RequireComponent(typeof(Collider))]
    public class WorldObstacle : MonoBehaviour, IWorldObject
    {
        [Header("Obstacle Settings")]
        [SerializeField] private bool destroyOnDespawn = false;
        [SerializeField] private float damage = 1;
        [SerializeField] private bool isOppositeDirection = false; // If true, moves backward and rotates 180 degrees
        [SerializeField] private GameObject oppositeDirectionSign;

        public bool IsOppositeDirection => isOppositeDirection;
        
        public void SetOppositeDirection(bool value)
        {
            isOppositeDirection = value;
            if (value)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
                if (oppositeDirectionSign != null) oppositeDirectionSign.SetActive(true);

                // Play warning sound when obstacle direction changes
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioEventSFX.DynamicObstacleWarning);
                }
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                if (oppositeDirectionSign != null) oppositeDirectionSign.SetActive(false);
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

        // Rewind clamping — track the local position where the obstacle first started moving
        // so the reverse animation can stop naturally there instead of overshooting.
        private Vector3 _activationLocalPosition;    // local pos at the moment movement began
        private bool _hasActivationPosition = false; // true once the above is captured
        private float _reverseClampDirection = 0f;   // +1 if localZ grew during play, -1 if it shrank

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
            if (!_hasActivationPosition)
            {
                _activationLocalPosition = transform.localPosition;
                _hasActivationPosition = true;
            }
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
                                // Check if player has reached the n meter mark in the chunk.
                                // Use the live chunk StartZ so post-rewind re-activation is correct;
                                // _chunkStartZ is only set once at spawn time and drifts after rewinds.
                                float playerZ = player.transform.position.z;
                                float currentChunkStartZ = _parentChunk != null ? _parentChunk.StartZ : _chunkStartZ;
                                float distanceIntoChunk = playerZ - currentChunkStartZ;

                                if (distanceIntoChunk >= _startMovingAtMeters)
                                {
                                    if (!_hasActivationPosition)
                                    {
                                        _activationLocalPosition = transform.localPosition;
                                        _hasActivationPosition = true;
                                    }
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
                                if (!_hasActivationPosition)
                                {
                                    _activationLocalPosition = transform.localPosition;
                                    _hasActivationPosition = true;
                                }
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
            // When reversing, stop the obstacle the moment it reaches its designed start position
            // so it never overshoots. The clamp direction tells us whether the obstacle's local Z
            // was increasing (+1) or decreasing (-1) during normal play, so we know which side
            // of _activationLocalPosition counts as "back to start."
            if (_isReversed && _hasActivationPosition && _reverseClampDirection != 0f)
            {
                float localZ = transform.localPosition.z;
                bool reachedStart = _reverseClampDirection > 0f
                    ? localZ <= _activationLocalPosition.z   // moved in +Z during play → reverse goes -Z
                    : localZ >= _activationLocalPosition.z;  // moved in -Z during play → reverse goes +Z

                if (reachedStart)
                {
                    transform.localPosition = _activationLocalPosition;
                    _isMoving = false;
                    _forceStartMoving = false;
                    _hasActivationPosition = false; // clear so re-activation captures a fresh position
                    _reverseClampDirection = 0f;
                    if (_pointTrigger != null) _pointTrigger.ResetTrigger();
                    return;
                }
            }

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
            // Prevent multiple collisions after game over
            if (GameController.Instance != null && GameController.Instance.IsGameOver)
            {
                return;
            }

            // Play collision sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioEventSFX.Collision);
            }

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
            _hasActivationPosition = false;  // clear so next run captures a fresh position
            _reverseClampDirection = 0f;

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
            _hasActivationPosition = false;     // clear so re-activation captures a fresh position
            _reverseClampDirection = 0f;
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

            // _isMoving is false for everyone at call-time (PauseDynamicObstacles already ran).
            // Use _wasMovingBeforePause — it captured the pre-death state before Pause() cleared
            // _isMoving, so it correctly identifies which obstacles were actually activated.
            _wasMovingBeforeReverse = _wasMovingBeforePause;

            if (!_wasMovingBeforeReverse)
            {
                // Obstacle was never activated — leave it exactly where it is.
                // _isMoving stays false; no movement occurs during the rewind.
                _isPaused = false;
                return;
            }

            // Record which direction the obstacle moved in local space during normal play.
            // This is used by MoveWithWorld() to know when we've reversed back to start.
            if (_hasActivationPosition)
            {
                float diff = transform.localPosition.z - _activationLocalPosition.z;
                _reverseClampDirection = Mathf.Approximately(diff, 0f) ? 0f : Mathf.Sign(diff);
            }

            // For opposite direction obstacles, reverse means moving forward (positive speed)
            // For normal obstacles, reverse means moving backward (negative speed)
            float reverseMultiplier = GameController.Instance != null ? GameController.Instance.ReverseMultiplier : 2f;
            if (isOppositeDirection)
            {
                _moveSpeed = Mathf.Abs(_originalMoveSpeed) * reverseMultiplier;
            }
            else
            {
                _moveSpeed = -Mathf.Abs(_originalMoveSpeed) * reverseMultiplier;
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
            // Clear activation bookkeeping so the next activation captures a fresh position.
            _hasActivationPosition = false;
            _forceStartMoving = false;
            _reverseClampDirection = 0f;
            if (_pointTrigger != null) _pointTrigger.ResetTrigger();
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

            // Don't process collisions if game is already over
            if (GameController.Instance != null && GameController.Instance.IsGameOver)
            {
                return;
            }

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

                // Invincibility powerup — deflect: show particles + SFX but skip damage
                if (PowerupManager.Instance != null && PowerupManager.Instance.IsInvincible)
                {
                    Vector3 deflectPos = other.ClosestPoint(transform.position);
                    ParticleEffectSpawner.SpawnParticleEffect(ParticleEffectType.ObstacleImpact, deflectPos);
                    AudioManager.Instance?.PlaySFX(AudioEventSFX.PowerupInvincibilityDeflect);
                    return;
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

