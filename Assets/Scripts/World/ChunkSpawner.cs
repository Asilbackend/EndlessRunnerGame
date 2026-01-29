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
        
        // Level generation tracking
        private int _currentChunkIndex = 0;
        private const int LEVELS_PER_CYCLE = 10;
        private List<Difficulty> _currentCycleDifficulties = new List<Difficulty>();
        private int _currentCycleIndex = 0;
        private HashSet<GameObject> _usedChunksInCycle = new HashSet<GameObject>();

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
            // Update player z position from actual player transform
            if (GameController.Instance != null && GameController.Instance.PlayerController != null)
            {
                playerZPosition = GameController.Instance.PlayerController.transform.position.z;
            }
            
            int chunksBeforeSpawn = _activeChunks.Count;
            while (_activeChunks.Count < chunksToKeepAhead || 
                   (_activeChunks.Count > 0 && _activeChunks[_activeChunks.Count - 1].EndZ < playerZPosition + spawnDistanceAhead))
            {
                SpawnChunk();
                if (_activeChunks.Count == chunksBeforeSpawn)
                {
                    Debug.LogWarning("ChunkSpawner: SpawnChunk() failed to add a chunk, breaking spawn loop");
                    break;
                }
                chunksBeforeSpawn = _activeChunks.Count;
            }

            DespawnPassedChunks();
        }

        private void SpawnInitialChunks()
        {
            InitializeLevelCycle();
            for (int i = 0; i < chunksToKeepAhead; i++)
            {
                SpawnChunk();
            }
        }

        private void InitializeLevelCycle()
        {
            // Pattern: Level 1 = Easy, Levels 2-7 = Random Easy/Medium (3 Easy + 3 Medium), Levels 8-9 = Hard, Level 10 = Extreme
            _currentCycleDifficulties.Clear();
            
            // Level 1: Always Easy
            _currentCycleDifficulties.Add(Difficulty.Easy);
            
            // Levels 2-7: Create a pool of 3 Easy and 3 Medium, then shuffle
            List<Difficulty> easyMediumPool = new List<Difficulty>
            {
                Difficulty.Easy,
                Difficulty.Easy,
                Difficulty.Easy,
                Difficulty.Medium,
                Difficulty.Medium,
                Difficulty.Medium
            };
            
            // Shuffle the pool
            for (int i = 0; i < easyMediumPool.Count; i++)
            {
                Difficulty temp = easyMediumPool[i];
                int randomIndex = Random.Range(i, easyMediumPool.Count);
                easyMediumPool[i] = easyMediumPool[randomIndex];
                easyMediumPool[randomIndex] = temp;
            }
            
            _currentCycleDifficulties.AddRange(easyMediumPool);
            
            // Levels 8-9: Hard
            _currentCycleDifficulties.Add(Difficulty.Hard);
            _currentCycleDifficulties.Add(Difficulty.Hard);
            
            // Level 10: Extreme
            _currentCycleDifficulties.Add(Difficulty.Extreme);
            
            _currentCycleIndex = 0;
            _usedChunksInCycle.Clear();
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

            // Check if this is the first chunk (empty chunk with only road and decoration)
            bool isFirstChunk = _activeChunks.Count == 0;

            // Get road prefab
            GameObject roadPrefab = chosenLayout.roadPrefab;
            if (roadPrefab == null && defaultChunkLayout != null)
            {
                roadPrefab = defaultChunkLayout.roadPrefab;
            }

            // Get random chunk prefab (obstacles & collectibles) - skip for first chunk
            GameObject chunkPrefab = null;
            if (!isFirstChunk)
            {
                // Get difficulty for current level
                Difficulty requiredDifficulty = GetDifficultyForCurrentLevel();
                
                // Try to get chunk with required difficulty from chosen layout (excluding used chunks)
                chunkPrefab = chosenLayout.GetRandomChunkPrefabByDifficulty(requiredDifficulty, _usedChunksInCycle);
                
                // If not found, search all layouts for a chunk with the required difficulty
                if (chunkPrefab == null)
                {
                    var validLayouts = chunkLayouts.FindAll(l => l != null);
                    foreach (var layout in validLayouts)
                    {
                        chunkPrefab = layout.GetRandomChunkPrefabByDifficulty(requiredDifficulty, _usedChunksInCycle);
                        if (chunkPrefab != null) break;
                    }
                }
                
                // If still not found, try default layout
                if (chunkPrefab == null && defaultChunkLayout != null)
                {
                    chunkPrefab = defaultChunkLayout.GetRandomChunkPrefabByDifficulty(requiredDifficulty, _usedChunksInCycle);
                }
                
                // Fallback: if still no chunk found with required difficulty, get any random chunk (excluding used)
                if (chunkPrefab == null)
                {
                    chunkPrefab = chosenLayout.GetRandomChunkPrefabExcluding(_usedChunksInCycle);
                    if (chunkPrefab == null && defaultChunkLayout != null)
                    {
                        chunkPrefab = defaultChunkLayout.GetRandomChunkPrefabExcluding(_usedChunksInCycle);
                    }
                }
                
                // If we found a chunk, mark it as used (isOnlyCars chunks can be reused each cycle, so don't add to set)
                if (chunkPrefab != null)
                {
                    var chunkComponent = chunkPrefab.GetComponent<WorldChunk>();
                    if (chunkComponent == null || !chunkComponent.IsOnlyCars)
                    {
                        _usedChunksInCycle.Add(chunkPrefab);
                    }
                }
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

            // Calculate chunk length from components (for first chunk positioning)
            // Use max of all available lengths (road, chunk, decoration)
            float chunkLength = 0f;
            
            // Check instantiated components first
            if (road != null) chunkLength = Mathf.Max(chunkLength, road.RoadLength);
            if (chunk != null) chunkLength = Mathf.Max(chunkLength, chunk.ChunkLength);
            if (decoration != null) chunkLength = Mathf.Max(chunkLength, decoration.DecorationLength);
            
            // Also check prefabs for any components we don't have yet (to get accurate max length)
            if (road == null && roadPrefab != null)
            {
                Road roadComponent = roadPrefab.GetComponent<Road>();
                if (roadComponent != null) chunkLength = Mathf.Max(chunkLength, roadComponent.RoadLength);
            }
            if (chunk == null && chunkPrefab != null)
            {
                WorldChunk chunkComponent = chunkPrefab.GetComponent<WorldChunk>();
                if (chunkComponent != null) chunkLength = Mathf.Max(chunkLength, chunkComponent.ChunkLength);
            }
            if (decoration == null && decorationPrefab != null)
            {
                Decoration decorationComponent = decorationPrefab.GetComponent<Decoration>();
                if (decorationComponent != null) chunkLength = Mathf.Max(chunkLength, decorationComponent.DecorationLength);
            }

            // Create composite container
            GameObject compositeObj = new GameObject("ChunkComposite");
            compositeObj.transform.SetParent(_compositeContainer.transform);
            WorldChunkComposite composite = compositeObj.AddComponent<WorldChunkComposite>();

            float spawnZ = 0;
            if (isFirstChunk)
            {
                // First chunk starts at -(chunkLength - 60) so player at z=0 is positioned at z=40 within the chunk
                // Example: if chunkLength=100, spawnZ=-(100-60)=-40, so chunk spans from z=-40 to z=60, player at z=0 is 40 units into the chunk
                spawnZ = -(chunkLength - 60f);
            }
            else if (_activeChunks.Count > 0)
            {
                WorldChunkComposite lastComposite = _activeChunks[_activeChunks.Count - 1];
                spawnZ = lastComposite.EndZ;
            }

            composite.Initialize(road, chunk, decoration, spawnZ);
            _activeChunks.Add(composite);
            
            // Increment chunk index for level tracking (only for non-first chunks)
            if (!isFirstChunk)
            {
                _currentChunkIndex++;
                _currentCycleIndex++;
                
                // Reset cycle when we complete 10 levels (after incrementing, so we've processed chunk 10)
                if (_currentCycleIndex >= LEVELS_PER_CYCLE)
                {
                    InitializeLevelCycle();
                }
            }
        }

        private Difficulty GetDifficultyForCurrentLevel()
        {
            // Ensure cycle is initialized
            if (_currentCycleDifficulties.Count == 0)
            {
                InitializeLevelCycle();
            }
            
            int indexInCycle = _currentCycleIndex % LEVELS_PER_CYCLE;
            if (indexInCycle >= 0 && indexInCycle < _currentCycleDifficulties.Count)
            {
                return _currentCycleDifficulties[indexInCycle];
            }
            
            // Fallback to Easy if something goes wrong
            Debug.LogWarning($"ChunkSpawner: Invalid cycle index {indexInCycle}, falling back to Easy difficulty");
            return Difficulty.Easy;
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
            
            _currentChunkIndex = 0;
            InitializeLevelCycle();
        }
    }
}

