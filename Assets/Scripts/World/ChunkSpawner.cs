using UnityEngine;
using System.Collections.Generic;
using Managers;

namespace World
{
    public class ChunkSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistanceAhead = 20f;
        [SerializeField] private float despawnDistanceBehind = 10f;
        [SerializeField] private int chunksToKeepAhead = 2;
        [SerializeField] private float playerZPosition = 0f;

        [SerializeField] private List<ChunkLayoutSO> chunkLayouts = new List<ChunkLayoutSO>();
        [SerializeField] private ChunkLayoutSO defaultChunkLayout;

        private ChunkPool _chunkPool;
        private DecorationPool _decorationPool;
        private RoadPool _roadPool;
        private WorldManager worldManager;
        private List<WorldChunkComposite> _activeChunks = new List<WorldChunkComposite>();
        private GameObject _compositeContainer;

        private void Awake()
        {
            _chunkPool = GetComponent<ChunkPool>();
            if (_chunkPool == null)
            {
                Debug.LogError("ChunkSpawner: ChunkPool component not found!");
            }

            _decorationPool = GetComponent<DecorationPool>();
            if (_decorationPool == null)
            {
                Debug.LogError("ChunkSpawner: DecorationPool component not found!");
            }

            _roadPool = GetComponent<RoadPool>();
            if (_roadPool == null)
            {
                Debug.LogError("ChunkSpawner: RoadPool component not found!");
            }

            // Create container for composite objects
            _compositeContainer = new GameObject("ChunkComposites");
            _compositeContainer.transform.SetParent(transform);
        }

        private void Start()
        {
            if (worldManager == null)
            {
                worldManager = GameController.Instance?.WorldManager;
            }
            SpawnInitialChunks();
        }

        private void Update()
        {
            while (_activeChunks.Count < chunksToKeepAhead || 
                   (_activeChunks.Count > 0 && _activeChunks[_activeChunks.Count - 1].EndZ < playerZPosition + spawnDistanceAhead))
            {
                SpawnChunk();
            }

            DespawnPassedChunks();
        }

        private void SpawnInitialChunks()
        {
            for (int i = 0; i < chunksToKeepAhead; i++)
            {
                SpawnChunk();
            }
        }

        private void SpawnChunk()
        {
            if (_chunkPool == null || _decorationPool == null || _roadPool == null) return;

            ChunkLayoutSO chosenLayout = ChooseRandomLayout();
            if (chosenLayout == null)
            {
                Debug.LogWarning("ChunkSpawner: No layout available to spawn chunk!");
                return;
            }

            // Get road prefab
            GameObject roadPrefab = chosenLayout.roadPrefab;
            if (roadPrefab == null && defaultChunkLayout != null)
            {
                roadPrefab = defaultChunkLayout.roadPrefab;
            }

            // Get random chunk prefab (obstacles & collectibles)
            GameObject chunkPrefab = chosenLayout.GetRandomChunkPrefab();
            if (chunkPrefab == null && defaultChunkLayout != null)
            {
                chunkPrefab = defaultChunkLayout.GetRandomChunkPrefab();
            }

            // Get random decoration prefab
            GameObject decorationPrefab = chosenLayout.GetRandomDecorationPrefab();
            if (decorationPrefab == null && defaultChunkLayout != null)
            {
                decorationPrefab = defaultChunkLayout.GetRandomDecorationPrefab();
            }

            // Get road from pool (or create if unique)
            Road road = null;
            if (roadPrefab != null)
            {
                road = _roadPool.GetRoad(roadPrefab);
            }

            // Get chunk from pool
            WorldChunk chunk = null;
            if (chunkPrefab != null)
            {
                chunk = _chunkPool.GetChunk(chunkPrefab);
            }

            // Get decoration from pool
            Decoration decoration = null;
            if (decorationPrefab != null)
            {
                decoration = _decorationPool.GetDecoration(decorationPrefab);
            }

            // Create composite container
            GameObject compositeObj = new GameObject("ChunkComposite");
            compositeObj.transform.SetParent(_compositeContainer.transform);
            WorldChunkComposite composite = compositeObj.AddComponent<WorldChunkComposite>();

            float spawnZ = 0;
            if (_activeChunks.Count > 0)
            {
                WorldChunkComposite lastComposite = _activeChunks[_activeChunks.Count - 1];
                spawnZ = lastComposite.EndZ;
            }

            composite.Initialize(road, chunk, decoration, spawnZ);
            _activeChunks.Add(composite);
        }

        private ChunkLayoutSO ChooseRandomLayout()
        {
            // Filter out null layouts
            var validLayouts = chunkLayouts.FindAll(e => e != null);
            
            if (validLayouts == null || validLayouts.Count == 0)
            {
                // Fallback to default
                if (defaultChunkLayout != null)
                {
                    return defaultChunkLayout;
                }
                return null;
            }

            // Randomly select from valid layouts
            return validLayouts[Random.Range(0, validLayouts.Count)];
        }

        private void DespawnPassedChunks()
        {
            for (int i = _activeChunks.Count - 1; i >= 0; i--)
            {
                if (_activeChunks[i] == null || _activeChunks[i].HasPassedPlayer(playerZPosition, despawnDistanceBehind))
                {
                    WorldChunkComposite compositeToRemove = _activeChunks[i];
                    _activeChunks.RemoveAt(i);
                    
                    if (compositeToRemove != null)
                    {
                        // Return components to their pools
                        if (compositeToRemove.Road != null && _roadPool != null)
                        {
                            _roadPool.ReturnRoad(compositeToRemove.Road);
                        }
                        if (compositeToRemove.Chunk != null && _chunkPool != null)
                        {
                            _chunkPool.ReturnChunk(compositeToRemove.Chunk);
                        }
                        if (compositeToRemove.Decoration != null && _decorationPool != null)
                        {
                            _decorationPool.ReturnDecoration(compositeToRemove.Decoration);
                        }

                        compositeToRemove.ResetComposite();
                        Destroy(compositeToRemove.gameObject);
                }
            }
            }
        }

        public List<WorldChunkComposite> GetActiveChunks()
        {
            return new List<WorldChunkComposite>(_activeChunks);
        }
        
        public void DespawnAllChunks()
        {
            foreach (var composite in _activeChunks)
            {
                if (composite != null)
                {
                    if (composite.Road != null && _roadPool != null)
                    {
                        _roadPool.ReturnRoad(composite.Road);
                    }
                    if (composite.Chunk != null && _chunkPool != null)
                    {
                        _chunkPool.ReturnChunk(composite.Chunk);
                    }
                    if (composite.Decoration != null && _decorationPool != null)
                    {
                        _decorationPool.ReturnDecoration(composite.Decoration);
                    }
                    composite.ResetComposite();
                    Destroy(composite.gameObject);
                }
            }
            _activeChunks.Clear();
        }
    }
}

