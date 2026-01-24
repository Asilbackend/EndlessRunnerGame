using System.Collections;
using UnityEngine;
using World;

namespace Managers
{
    public class WorldManager : MonoBehaviour
    {
        [SerializeField] private WorldMover worldMover;
        [SerializeField] private ChunkSpawner chunkSpawner;
        [SerializeField] private ChunkPool chunkPool;


        private readonly Lane _leftLane = new Lane(LaneNumber.Left, -5f, -1.67f);
        private readonly Lane _centerLane = new Lane(LaneNumber.Left, -1.66f, 1.66f);
        private readonly Lane _rightLane = new Lane(LaneNumber.Left, 1.67f, 5f);

        public ChunkSpawner ChunkSpawner => chunkSpawner;

        private void Awake()
        {
            if (worldMover == null)
                worldMover = GetComponent<WorldMover>();
            
            if (chunkSpawner == null)
                chunkSpawner = GetComponent<ChunkSpawner>();
            
            if (chunkPool == null)
                chunkPool = GetComponent<ChunkPool>();
        }
        
        private void Update()
        {   
            MoveWorld();
        }
        
        private void MoveWorld()
        {
            if (worldMover == null || chunkSpawner == null) return;
            
            float movementDelta = worldMover.GetMovementDelta();
            
            var activeChunks = chunkSpawner.GetActiveChunks();
            foreach (var composite in activeChunks)
            {
                if (composite != null && composite.IsActive)
                {
                    composite.MoveComposite(movementDelta);
                }
            }
        }
        
        public void SetWorldSpeed(float speed)
        {
            if (worldMover != null)
            {
                worldMover.SetSpeed(speed);
            }
        }
        
        public void PauseWorld()
        {
            if (worldMover != null)
            {
                worldMover.Pause();
            }
            PauseDynamicObstacles();
        }

        public void PauseDynamicObstacles()
        {
            if (chunkSpawner != null)
            {
                var activeChunks = chunkSpawner.GetActiveChunks();
                foreach (var composite in activeChunks)
                {
                    if (composite != null && composite.IsActive && composite.Chunk != null)
                    {
                        WorldObstacle[] obstacles = composite.Chunk.GetComponentsInChildren<WorldObstacle>(true);
                        foreach (var obstacle in obstacles)
                        {
                            if (obstacle != null)
                            {
                                obstacle.Pause();
                            }
                        }

                        WorldCollectible[] collectibles = composite.Chunk.GetComponentsInChildren<WorldCollectible>(true);
                        foreach (var col in collectibles)
                        {
                            if (col != null)
                            {
                                col.Pause();
                            }
                        }
                    }
                }
            }
        }

        public void ResumeWorld()
        {
            if (worldMover != null)
            {
                worldMover.Resume();
            }
        }

        public void ResumeDynamicObstacles()
        {
            if (chunkSpawner != null)
            {
                var activeChunks = chunkSpawner.GetActiveChunks();
                foreach (var composite in activeChunks)
                {
                    if (composite != null && composite.IsActive && composite.Chunk != null)
                    {
                        WorldObstacle[] obstacles = composite.Chunk.GetComponentsInChildren<WorldObstacle>(true);
                        foreach (var obstacle in obstacles)
                        {
                            if (obstacle != null)
                            {
                                obstacle.Resume();
                            }
                        }

                        WorldCollectible[] collectibles = composite.Chunk.GetComponentsInChildren<WorldCollectible>(true);
                        foreach (var col in collectibles)
                        {
                            if (col != null)
                            {
                                col.Resume();
                            }
                        }
                    }
                }
            }
        }

        public void ReverseDynamicObstacles()
        {
            if (chunkSpawner != null)
            {
                var activeChunks = chunkSpawner.GetActiveChunks();
                foreach (var composite in activeChunks)
                {
                    if (composite != null && composite.IsActive && composite.Chunk != null)
                    {
                        WorldObstacle[] obstacles = composite.Chunk.GetComponentsInChildren<WorldObstacle>(true);
                        foreach (var obstacle in obstacles)
                        {
                            if (obstacle != null)
                            {
                                obstacle.Reverse();
                            }
                        }

                        WorldCollectible[] collectibles = composite.Chunk.GetComponentsInChildren<WorldCollectible>(true);
                        foreach (var col in collectibles)
                        {
                            if (col != null)
                            {
                                col.Reverse();
                            }
                        }
                    }
                }
            }
        }

        public void ResetWorld()
        {
            StartCoroutine(ResetWorldProcess());

            if (chunkSpawner != null)
            {
                chunkSpawner.DespawnAllChunks();
            }
        }

        private IEnumerator ResetWorldProcess()
        {
            yield return new WaitForSeconds(GameController.Instance.StartTime);
            worldMover?.ResetSpeed();
            worldMover?.Resume();
            ResumeDynamicObstacles();
            var player = GameController.Instance != null ? GameController.Instance.PlayerController : null;
            if (player != null)
            {
                player.ResumeParticleSystems();
            }
        }

        public void ResetToLastCheckpoint(float duration)
        {
            StartCoroutine(ResetToLastCheckpointProcess(duration));
        }

        private IEnumerator ResetToLastCheckpointProcess(float duration)
        {
            worldMover?.Reverse();
            ReverseDynamicObstacles();
            var player = GameController.Instance != null ? GameController.Instance.PlayerController : null;
            if (player != null)
            {
                player.ReverseAnimationAndWheels();
            }
            yield return new WaitForSeconds(duration);
            worldMover?.Pause();
            PauseDynamicObstacles();
            if (player != null)
            {
                player.StopAnimationAndWheels();
            }
            yield return new WaitForSeconds(GameController.Instance.StartTime);
            worldMover?.Resume();
            ResumeDynamicObstacles();
            if (player != null)
            {
                player.ResumeAnimationAndWheels();
                player.ResumeParticleSystems();
            }
        }
        
        public float GetCurrentSpeed()
        {
            return worldMover != null ? worldMover.CurrentSpeed : 0f;
        }
        
        public float GetTotalDistance()
        {
            return worldMover != null ? worldMover.TotalDistanceTraveled : 0f;
        }
        
        // New: expose movement delta (meters moved this frame) so external objects can move with the world
        public float GetMovementDelta()
        {
            return worldMover != null ? worldMover.GetMovementDelta() : 0f;
        }
        
        private void OnGUI()
        {
            if (GameManager.Instance.DebugMode) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box("World Manager Debug");
            GUILayout.Label($"Speed: {GetCurrentSpeed():F1} m/s");
            GUILayout.Label($"Distance: {GetTotalDistance():F1} m");
            
            if (chunkSpawner != null)
            {
                var chunks = chunkSpawner.GetActiveChunks();
                GUILayout.Label($"Active Chunks: {chunks.Count}");
            }
            
            if (chunkPool != null)
            {
                GUILayout.Label($"Pool Active: {chunkPool.GetActiveChunkCount()}");
            }
            
            GUILayout.EndArea();
        }

        public float GetLaneXPosition(LaneNumber lane)
        {
            return lane switch
            {
                LaneNumber.Left => _leftLane.Center,
                LaneNumber.Center => _centerLane.Center,
                LaneNumber.Right => _rightLane.Center,
                _ => _centerLane.Center
            };
        }
    }
}

