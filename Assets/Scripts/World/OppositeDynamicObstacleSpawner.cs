using UnityEngine;
using Managers;

namespace World
{
    public class OppositeDynamicObstacleSpawner : MonoBehaviour
    {
        private OppositeDynamicObstacleConfig _config;
        private ObjectsContainerSO _dynamicObstaclesContainer;
        private GameObject _warningSignPrefab;
        private float _oppositeObstacleSpeed;
        private float _worldSpeed;
        private float _activationDistance;
        private float _chunkStartZ;
        private float _chunkEndZ;
        private LaneNumber _lane;
        private float _calculatedSignAppearAtMeters;
        
        private GameObject _spawnedObstacle;
        private Vector3 _spawnedObstacleOriginalLocalPosition; // local position at spawn time, used for checkpoint reset
        private GameObject _spawnedWarningSign;
        private OppositeDynamicObstacleWarningSign _warningSignComponent;
        private bool _obstacleSpawned = false;
        private bool _signSpawned = false;

        public void Initialize(
            OppositeDynamicObstacleConfig config,
            ObjectsContainerSO dynamicObstaclesContainer,
            GameObject warningSignPrefab,
            float oppositeObstacleSpeed,
            float worldSpeed,
            float activationDistance,
            float chunkStartZ,
            float chunkEndZ)
        {
            _config = config;
            _dynamicObstaclesContainer = dynamicObstaclesContainer;
            _warningSignPrefab = warningSignPrefab;
            _oppositeObstacleSpeed = oppositeObstacleSpeed;
            _worldSpeed = worldSpeed;
            _activationDistance = activationDistance;
            _chunkStartZ = chunkStartZ;
            _chunkEndZ = chunkEndZ;
            _lane = config.lane;
            _obstacleSpawned = false;
            _signSpawned = false;

            // Calculate meeting point and sign appear position
            CalculateSignAppearPosition();
        }
        
        private void CalculateSignAppearPosition()
        {
            // Use the live world speed if available, falling back to the designer-estimate
            // value stored in _worldSpeed.  The designer-estimate is only correct at the
            // moment the chunk is initialized; as the game accelerates the two values
            // diverge, causing the warning sign to appear at the wrong distance from the
            // player.
            float currentWorldSpeed = _worldSpeed;
            if (GameController.Instance != null && GameController.Instance.WorldManager != null)
            {
                float liveSpeed = GameController.Instance.WorldManager.GetCurrentSpeed();
                if (liveSpeed > 0f)
                    currentWorldSpeed = liveSpeed;
            }

            // Calculate meeting point using same formula as gizmos
            float obstacleSpawnZ = _chunkStartZ + _activationDistance + _config.obstacleStartAtMeters;
            float distanceWhenStarts = _activationDistance;
            float relativeSpeed = currentWorldSpeed + _oppositeObstacleSpeed;

            if (relativeSpeed <= 0)
            {
                // They'll never meet, use a default value
                _calculatedSignAppearAtMeters = 0f;
                return;
            }

            float timeToMeet = distanceWhenStarts / relativeSpeed;
            float obstacleTravelDistance = _oppositeObstacleSpeed * timeToMeet;
            float meetingZ = obstacleSpawnZ - obstacleTravelDistance;

            // Sign appears metersBeforeImpact before the meeting point
            float signAppearZ = meetingZ - _config.metersBeforeImpact;

            // Convert to distance from chunk start
            _calculatedSignAppearAtMeters = signAppearZ - _chunkStartZ;
        }

        public void UpdateChunkPosition(float chunkStartZ, float chunkEndZ)
        {
            _chunkStartZ = chunkStartZ;
            _chunkEndZ = chunkEndZ;

            // Recalculate sign appear position for new chunk position
            CalculateSignAppearPosition();

            // Update warning sign chunk start position if it exists.
            // Only call UpdateChunkStartZ (not Initialize) to avoid resetting _isVisible
            // and toggling renderer visibility every frame, which causes unnecessary work
            // and potential flickering.
            if (_warningSignComponent != null)
            {
                _warningSignComponent.UpdateChunkStartZ(_chunkStartZ);
            }
        }

        private void SpawnWarningSign()
        {
            if (_warningSignPrefab == null)
            {
                Debug.LogWarning($"OppositeDynamicObstacleSpawner: Warning sign prefab is null for lane {_lane}. Cannot spawn warning sign.");
                return;
            }

            WorldChunk parentChunk = GetComponentInParent<WorldChunk>();
            if (parentChunk == null)
            {
                Debug.LogWarning($"OppositeDynamicObstacleSpawner: Parent chunk not found. Cannot spawn warning sign.");
                return;
            }

            // Get randomized lane X position (respects lane randomization mode)
            float laneX = parentChunk.GetRandomizedLaneXPosition(_lane);
            float chunkLength = parentChunk.ChunkLength;
            // Position sign at middle of chunk (50m for 100m chunk)
            float signLocalZ = chunkLength / 2f;
            // Position 0.001 higher than road
            Vector3 signLocalPosition = new Vector3(laneX, 0.011f, signLocalZ);

            _spawnedWarningSign = Instantiate(_warningSignPrefab, transform);
            if (_spawnedWarningSign == null)
            {
                Debug.LogError($"OppositeDynamicObstacleSpawner: Failed to instantiate warning sign prefab for lane {_lane}.");
                return;
            }

            _signSpawned = true;

            // Ensure the sign GameObject is active so its Update() method can run
            _spawnedWarningSign.SetActive(true);
            
            _spawnedWarningSign.transform.localPosition = signLocalPosition;
            _spawnedWarningSign.name = $"WarningSign_{_lane}_Middle";

            _warningSignComponent = _spawnedWarningSign.GetComponent<OppositeDynamicObstacleWarningSign>();
            if (_warningSignComponent == null)
            {
                _warningSignComponent = _spawnedWarningSign.AddComponent<OppositeDynamicObstacleWarningSign>();
            }

            // Initialize sign to be visible from calculated appear position to end of chunk
            _warningSignComponent.Initialize(_chunkStartZ, chunkLength, _calculatedSignAppearAtMeters, chunkLength);
            
        }

        private void Update()
        {
            // Safety checks
            if (_config == null) return;
            
            var player = GameController.Instance != null ? GameController.Instance.PlayerController : null;
            if (player == null) return;

            float playerZ = player.transform.position.z;
            float distanceIntoChunk = playerZ - _chunkStartZ;
            if (!_signSpawned)
            {
                SpawnWarningSign();
            }

            // Spawn obstacle when player reaches n meters
            if (!_obstacleSpawned && distanceIntoChunk >= _config.obstacleStartAtMeters)
            {
                SpawnObstacle();
            }
        }

        private void SpawnObstacle()
        {
            if (_obstacleSpawned || _dynamicObstaclesContainer == null || _config == null) return;

            // Get random object from container
            ObjectData objectData = _dynamicObstaclesContainer.GetRandomObject();
            if (objectData == null || objectData.objectPrefab == null)
            {
                Debug.LogWarning($"OppositeDynamicObstacleSpawner: Could not get valid object data from container.");
                return;
            }

            WorldChunk parentChunk = GetComponentInParent<WorldChunk>();
            if (parentChunk == null) return;

            // Get randomized lane X position (respects lane randomization mode)
            float laneX = parentChunk.GetRandomizedLaneXPosition(_lane);
            
            // Spawn obstacle at activationDistance + obstacleStartAtMeters from chunk start
            float obstacleSpawnZ = _activationDistance + _config.obstacleStartAtMeters;
            Vector3 obstacleLocalPosition = new Vector3(laneX, 0, obstacleSpawnZ);

            // Store original local position so checkpoint reset can teleport back here (not destroy)
            _spawnedObstacleOriginalLocalPosition = obstacleLocalPosition;

            _spawnedObstacle = Instantiate(objectData.objectPrefab, transform);
            _spawnedObstacle.transform.localPosition = obstacleLocalPosition;
            _spawnedObstacle.name = $"OppositeDynamicObstacle_{_lane}_{_config.obstacleStartAtMeters}m";
            
            _spawnedObstacle.SetActive(true);

            // Add WorldObstacle component if not present and configure as opposite direction
            WorldObstacle obstacle = _spawnedObstacle.GetComponent<WorldObstacle>();
            if (obstacle == null)
            {
                obstacle = _spawnedObstacle.AddComponent<WorldObstacle>();
            }

            // IMPORTANT: Set as opposite direction BEFORE configuring, so ConfigureFromObjectData uses the correct logic
            obstacle.SetOppositeDirection(true);

            obstacle.ConfigureFromObjectData(objectData, _oppositeObstacleSpeed);
            obstacle.SetActivationParameters(_chunkStartZ, _config.obstacleStartAtMeters);
            
            // Force it to start moving immediately
            obstacle.ForceStartMoving();

            // Tell the warning sign to track this obstacle
            if (_warningSignComponent != null)
            {
                _warningSignComponent.SetTrackedObstacle(_spawnedObstacle);
            }

            _obstacleSpawned = true;
        }

        /// <summary>
        /// Resets the spawned obstacle to its designed start position after a checkpoint rewind.
        /// The obstacle is NOT destroyed — it is teleported back and its state is cleared so it
        /// will re-activate once the player reaches the trigger distance again.
        /// The warning sign is left intact; it manages its own visibility.
        /// </summary>
        public void ResetForCheckpoint()
        {
            if (_spawnedObstacle != null)
            {
                // Teleport back to the local position the obstacle was at when first spawned
                _spawnedObstacle.transform.localPosition = _spawnedObstacleOriginalLocalPosition;

                WorldObstacle obstacle = _spawnedObstacle.GetComponent<WorldObstacle>();
                if (obstacle != null)
                    obstacle.ResetForCheckpoint();

                // Re-register the (same) obstacle with the warning sign so its reference is fresh
                if (_warningSignComponent != null)
                    _warningSignComponent.SetTrackedObstacle(_spawnedObstacle);
            }
            // _obstacleSpawned stays true  — obstacle exists and will self-activate at right distance
            // _signSpawned stays true       — existing warning sign is reused
        }

        public void Cleanup()
        {
            if (_spawnedObstacle != null)
            {
                Destroy(_spawnedObstacle);
                _spawnedObstacle = null;
            }

            if (_spawnedWarningSign != null)
            {
                Destroy(_spawnedWarningSign);
                _spawnedWarningSign = null;
            }

            _warningSignComponent = null;
            _obstacleSpawned = false;
            _signSpawned = false;
        }
    }
}
