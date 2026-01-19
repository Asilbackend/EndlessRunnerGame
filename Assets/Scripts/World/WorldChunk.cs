using UnityEngine;
using System.Collections.Generic;
using Managers;

namespace World
{
    public class WorldChunk : MonoBehaviour
    {
        private enum LaneRandomizationMode
        {
            LeftRight,
            AllThree
        }
        [Header("Chunk Settings")]
        [SerializeField] private float chunkLength = 20f;
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [SerializeField] private Transform LeftLane;
        [SerializeField] private Transform CenterLane;
        [SerializeField] private Transform RightLane;
        [SerializeField] private LaneRandomizationMode laneRandomizationMode = LaneRandomizationMode.LeftRight;

        [Header("Opposite Dynamic Obstacles")]
        [SerializeField] private OppositeDynamicObstacleConfig[] oppositeDynamicObstacleConfigs = new OppositeDynamicObstacleConfig[0];
        [SerializeField] private ObjectsContainerSO dynamicObstaclesContainer;
        [SerializeField] private GameObject warningSignPrefab;
        [Tooltip("Speed for all opposite dynamic obstacles (normalized)")]
        [SerializeField] private float oppositeObstacleSpeed = 10f;


        private Transform chunkStartPoint;
        private Transform chunkEndPoint;
        private readonly List<IWorldObject> _worldObjects = new List<IWorldObject>();
        private float _currentZPosition;
        private bool _isActive = false;
        private Dictionary<Transform, Vector3> _originalObstaclePositions = new Dictionary<Transform, Vector3>();
        private GameObject _sourcePrefab;
        private List<OppositeDynamicObstacleSpawner> _oppositeObstacleSpawners = new List<OppositeDynamicObstacleSpawner>();
        
        public GameObject SourcePrefab
        {
            get => _sourcePrefab;
            set => _sourcePrefab = value;
        }
        
        public float ChunkLength => chunkLength;
        public Difficulty Difficulty => difficulty;
        public float StartZ => _currentZPosition;
        public float EndZ => _currentZPosition + chunkLength;
        public bool IsActive => _isActive;
        
        public float GetRandomizedLaneXPosition(LaneNumber lane)
        {
            Transform targetLane = null;
            switch (lane)
            {
                case LaneNumber.Left:
                    targetLane = LeftLane;
                    break;
                case LaneNumber.Center:
                    targetLane = CenterLane;
                    break;
                case LaneNumber.Right:
                    targetLane = RightLane;
                    break;
            }

            if (targetLane != null)
            {
                return targetLane.localPosition.x;
            }

            // Fallback to WorldManager if lane transforms are not set
            WorldManager worldManager = GameController.Instance != null ? GameController.Instance.WorldManager : null;
            if (worldManager != null)
            {
                return worldManager.GetLaneXPosition(lane);
            }

            return 0f;
        }
        
        private void Awake()
        {
            if (chunkStartPoint == null)
            {
                GameObject startObj = new GameObject("ChunkStart");
                startObj.transform.SetParent(transform);
                startObj.transform.localPosition = Vector3.zero;
                chunkStartPoint = startObj.transform;
            }
            
            if (chunkEndPoint == null)
            {
                GameObject endObj = new GameObject("ChunkEnd");
                endObj.transform.SetParent(transform);
                endObj.transform.localPosition = new Vector3(0, 0, chunkLength);
                chunkEndPoint = endObj.transform;
            }
        }
        
        public void Initialize(float zPosition)
        {
            _currentZPosition = zPosition;
            transform.position = new Vector3(0, 0, zPosition);
            _isActive = true;
            RandomizeLanes();
            gameObject.SetActive(true);
            
            var collectibles = GetComponentsInChildren<WorldCollectible>(true);
            foreach (var c in collectibles)
            {
                if (c != null)
                    c.ResetCollectible();
            }

            var placers = GetComponentsInChildren<ObjectPlacer>(true);
            foreach (var placer in placers)
            {
                if (placer != null)
                {
                    placer.RandomizePlacement();
                }
            }
            
            StoreOriginalObstaclePositions();
            
            // Spawn opposite dynamic obstacles if configured
            SpawnOppositeDynamicObstacles();
        }
        
        private void StoreOriginalObstaclePositions()
        {
            _originalObstaclePositions.Clear();
            WorldObstacle[] obstacles = GetComponentsInChildren<WorldObstacle>(true);
            foreach (WorldObstacle obstacle in obstacles)
            {
                if (obstacle != null && obstacle.transform != null)
                {
                    _originalObstaclePositions[obstacle.transform] = obstacle.transform.localPosition;
                }
            }
        }
        
        public void MoveChunk(float deltaMovement)
        {
            if (!_isActive) return;
            
            _currentZPosition -= deltaMovement;
            transform.position = new Vector3(transform.position.x, transform.position.y, _currentZPosition);

            var objectsCopy = _worldObjects.ToArray();
            foreach (var worldObject in objectsCopy)
            {
                if (worldObject == null) continue;

                var comp = worldObject as Component;
                if (comp != null)
                {
                    if (comp.transform.IsChildOf(transform))
                        continue;
                }
            }
        }
        
        public void AddWorldObject(IWorldObject worldObject)
        {
            if (!_worldObjects.Contains(worldObject))
            {
                _worldObjects.Add(worldObject);
            }
        }
        
        public void RemoveWorldObject(IWorldObject worldObject)
        {
            _worldObjects.Remove(worldObject);
        }
        
        public bool HasPassedPlayer(float playerZPosition, float despawnOffset = 5f)
        {
            return EndZ < playerZPosition - despawnOffset;
        }
        
        public void ResetChunk()
        {
            _isActive = false;

            var objectsCopy = _worldObjects.ToArray();

            _worldObjects.Clear();
            
            // Reset all obstacles to their original positions
            ResetObstaclePositions();
            
            // Clean up spawned opposite obstacles and signs
            CleanupOppositeDynamicObstacles();
            
            gameObject.SetActive(false);
        }

        // Update internal position tracking without moving transform (for use when parent handles movement)
        public void UpdatePosition(float zPosition)
        {
            _currentZPosition = zPosition;
            
            // Update spawner positions
            foreach (var spawner in _oppositeObstacleSpawners)
            {
                if (spawner != null)
                {
                    spawner.UpdateChunkPosition(_currentZPosition, EndZ);
                }
            }
        }
        
        private void ResetObstaclePositions()
        {
            // Iterate over a copy of the dictionary entries to avoid potential modification issues
            foreach (var kvp in _originalObstaclePositions)
            {
                var t = kvp.Key;
                if (t == null)
                    continue;

                t.localPosition = kvp.Value;

                var obstacle = t.GetComponent<WorldObstacle>();
                if (obstacle != null)
                    obstacle.ResetMoving();
            }
        }

        private void RandomizeLanes()
        {
            //50% chance to randomize according to the chosen mode
            float r = Random.value; // value between0 and1
            if (r <=0.5f) return;

            if (laneRandomizationMode == LaneRandomizationMode.AllThree)
            {
                // If any lane is missing, fall back to LeftRight swap when possible
                if (LeftLane == null || RightLane == null || CenterLane == null)
                {
                    // fallback
                    if (LeftLane != null && RightLane != null)
                    {
                        float leftX = LeftLane.localPosition.x;
                        float rightX = RightLane.localPosition.x;
                        LeftLane.localPosition = new Vector3(rightX, LeftLane.localPosition.y, LeftLane.localPosition.z);
                        RightLane.localPosition = new Vector3(leftX, RightLane.localPosition.y, RightLane.localPosition.z);
                    }
                    return;
                }

                // Collect current X positions
                float leftPos = LeftLane.localPosition.x;
                float centerPos = CenterLane.localPosition.x;
                float rightPos = RightLane.localPosition.x;

                float[] positions = new float[] { leftPos, centerPos, rightPos };

                // Simple Fisher-Yates shuffle
                for (int i = positions.Length -1; i >0; i--)
                {
                    int j = Random.Range(0, i +1);
                    float tmp = positions[i];
                    positions[i] = positions[j];
                    positions[j] = tmp;
                }

                // Assign shuffled X positions back, preserving Y and Z
                LeftLane.localPosition = new Vector3(positions[0], LeftLane.localPosition.y, LeftLane.localPosition.z);
                CenterLane.localPosition = new Vector3(positions[1], CenterLane.localPosition.y, CenterLane.localPosition.z);
                RightLane.localPosition = new Vector3(positions[2], RightLane.localPosition.y, RightLane.localPosition.z);
            }
            else // LeftRight
            {
                if (LeftLane != null && RightLane != null)
                {
                    float leftX = LeftLane.localPosition.x;
                    float rightX = RightLane.localPosition.x;
                    LeftLane.localPosition = new Vector3(rightX, LeftLane.localPosition.y, LeftLane.localPosition.z);
                    RightLane.localPosition = new Vector3(leftX, RightLane.localPosition.y, RightLane.localPosition.z);
                }
            }
        }

        private void SpawnOppositeDynamicObstacles()
        {
            if (oppositeDynamicObstacleConfigs == null || oppositeDynamicObstacleConfigs.Length == 0)
            {
                Debug.Log($"WorldChunk '{gameObject.name}': No opposite dynamic obstacle configs. Skipping.");
                return;
            }

            if (dynamicObstaclesContainer == null)
            {
                Debug.LogWarning($"WorldChunk '{gameObject.name}': dynamicObstaclesContainer is not set. Cannot spawn opposite dynamic obstacles.");
                return;
            }

            Debug.Log($"WorldChunk '{gameObject.name}': Spawning {oppositeDynamicObstacleConfigs.Length} opposite dynamic obstacle spawner(s). Warning sign prefab: {(warningSignPrefab != null ? warningSignPrefab.name : "NULL")}");

            foreach (var config in oppositeDynamicObstacleConfigs)
            {
                if (config == null || !config.IsValid())
                {
                    Debug.LogWarning($"WorldChunk '{gameObject.name}': Invalid opposite dynamic obstacle config. Skipping.");
                    continue;
                }

                // Create a spawner GameObject for this obstacle
                GameObject spawnerObj = new GameObject($"OppositeObstacleSpawner_{config.lane}");
                spawnerObj.transform.SetParent(transform);
                spawnerObj.transform.localPosition = Vector3.zero;

                OppositeDynamicObstacleSpawner spawner = spawnerObj.AddComponent<OppositeDynamicObstacleSpawner>();
                spawner.Initialize(
                    config,
                    dynamicObstaclesContainer,
                    warningSignPrefab,
                    oppositeObstacleSpeed,
                    _currentZPosition,
                    EndZ
                );

                _oppositeObstacleSpawners.Add(spawner);
            }
        }

        private void CleanupOppositeDynamicObstacles()
        {
            // Clean up spawners (they will clean up their obstacles and signs)
            foreach (var spawner in _oppositeObstacleSpawners)
            {
                if (spawner != null)
                {
                    spawner.Cleanup();
                    Destroy(spawner.gameObject);
                }
            }
            _oppositeObstacleSpawners.Clear();
        }

        private void OnDrawGizmos()
        {
            if (chunkStartPoint != null && chunkEndPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(
                    transform.position + new Vector3(0, 0, chunkLength / 2),
                    new Vector3(10, 1, chunkLength)
                );
                
                // Only draw lane lines if GameController and WorldManager are available (runtime only)
                if (GameController.Instance != null && GameController.Instance.WorldManager != null)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 start = transform.position;
                    Vector3 end = transform.position + new Vector3(0, 0, chunkLength);
                    
                    WorldManager worldManager = GameController.Instance.WorldManager;
                    Gizmos.DrawLine(start + new Vector3(worldManager.GetLaneXPosition(LaneNumber.Left), 0, 0), end + new Vector3(worldManager.GetLaneXPosition(LaneNumber.Left), 0, 0));
                    Gizmos.DrawLine(start + new Vector3(worldManager.GetLaneXPosition(LaneNumber.Center), 0, 0), end + new Vector3(worldManager.GetLaneXPosition(LaneNumber.Center), 0, 0));
                    Gizmos.DrawLine(start + new Vector3(worldManager.GetLaneXPosition(LaneNumber.Right), 0, 0), end + new Vector3(worldManager.GetLaneXPosition(LaneNumber.Right), 0, 0));
                }
            }
        }
    }
}

