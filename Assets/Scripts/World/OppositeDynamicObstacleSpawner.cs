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
        private float _chunkStartZ;
        private float _chunkEndZ;
        private LaneNumber _lane;
        
        private GameObject _spawnedObstacle;
        private GameObject _spawnedWarningSign;
        private OppositeDynamicObstacleWarningSign _warningSignComponent;
        private bool _obstacleSpawned = false;

        public void Initialize(
            OppositeDynamicObstacleConfig config,
            ObjectsContainerSO dynamicObstaclesContainer,
            GameObject warningSignPrefab,
            float oppositeObstacleSpeed,
            float chunkStartZ,
            float chunkEndZ)
        {
            _config = config;
            _dynamicObstaclesContainer = dynamicObstaclesContainer;
            _warningSignPrefab = warningSignPrefab;
            _oppositeObstacleSpeed = oppositeObstacleSpeed;
            _chunkStartZ = chunkStartZ;
            _chunkEndZ = chunkEndZ;
            _lane = config.lane;
            _obstacleSpawned = false;

            // Spawn warning sign immediately (it will show/hide based on player position)
            SpawnWarningSign();
        }

        public void UpdateChunkPosition(float chunkStartZ, float chunkEndZ)
        {
            _chunkStartZ = chunkStartZ;
            _chunkEndZ = chunkEndZ;
            
            // Update warning sign chunk start position if it exists
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

            // Ensure the sign GameObject is active so its Update() method can run
            _spawnedWarningSign.SetActive(true);
            
            _spawnedWarningSign.transform.localPosition = signLocalPosition;
            _spawnedWarningSign.name = $"WarningSign_{_lane}_Middle";

            _warningSignComponent = _spawnedWarningSign.GetComponent<OppositeDynamicObstacleWarningSign>();
            if (_warningSignComponent == null)
            {
                _warningSignComponent = _spawnedWarningSign.AddComponent<OppositeDynamicObstacleWarningSign>();
            }

            // Initialize sign to be visible from beginning (0m) to end (chunkLength) of chunk
            _warningSignComponent.Initialize(_chunkStartZ, chunkLength, 0f, chunkLength);
            
        }

        private void Update()
        {
            // Safety checks
            if (_config == null) return;
            
            var player = GameController.Instance != null ? GameController.Instance.PlayerController : null;
            if (player == null) return;

            float playerZ = player.transform.position.z;
            float distanceIntoChunk = playerZ - _chunkStartZ;

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
            float chunkLength = parentChunk.ChunkLength;
            
            Vector3 obstacleLocalPosition = new Vector3(laneX, 0, chunkLength);

            _spawnedObstacle = Instantiate(objectData.objectPrefab, transform);
            _spawnedObstacle.transform.localPosition = obstacleLocalPosition;
            _spawnedObstacle.name = $"OppositeDynamicObstacle_{_lane}_{_config.obstacleStartAtMeters}m";

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

            _obstacleSpawned = false;
        }
    }
}
