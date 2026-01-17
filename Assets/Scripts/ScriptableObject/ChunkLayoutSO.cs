using UnityEngine;
using System.Collections.Generic;

namespace World
{
    [CreateAssetMenu(fileName = "ChunkLayout", menuName = "World/Chunk Layout", order = 1)]
    public class ChunkLayoutSO : ScriptableObject
    {
        [Header("Layout Settings")]
        public int lanes = 3;

        [Header("Road Settings")]
        public GameObject roadPrefab;

        [Header("Chunk Settings (Obstacles & Collectibles)")]
        public List<GameObject> chunkPrefabs = new List<GameObject>();

        [System.Serializable]
        public class WeightedDecoration
        {
            public GameObject prefab;
            [Range(0.1f, 10f)]
            [Tooltip("Higher weight = more likely to be selected. Weight 5.0 is 5x more likely than weight 1.0")]
            public float weight = 1f;
        }
        
        [Header("Decoration Settings")]
        [Tooltip("Decorations with weighted probabilities. Higher weight = more likely to spawn.")]
        public List<WeightedDecoration> weightedDecorations = new List<WeightedDecoration>();

        // Legacy support - if chunkPrefab is set, add it to chunkPrefabs
        [System.Obsolete("Use chunkPrefabs instead")]
        public GameObject chunkPrefab
        {
            get => chunkPrefabs != null && chunkPrefabs.Count > 0 ? chunkPrefabs[0] : null;
            set
            {
                if (chunkPrefabs == null) chunkPrefabs = new List<GameObject>();
                if (value != null && !chunkPrefabs.Contains(value))
                {
                    chunkPrefabs.Add(value);
                }
            }
        }

        public GameObject GetRandomChunkPrefab()
        {
            if (chunkPrefabs == null || chunkPrefabs.Count == 0) return null;
            var validPrefabs = chunkPrefabs.FindAll(p => p != null);
            if (validPrefabs.Count == 0) return null;
            return validPrefabs[Random.Range(0, validPrefabs.Count)];
        }

        public GameObject GetRandomDecorationPrefab()
        {
            if (weightedDecorations == null || weightedDecorations.Count == 0) return null;
            
            var validDecorations = weightedDecorations.FindAll(wd => wd != null && wd.prefab != null);
            if (validDecorations.Count == 0) return null;
            
            // Calculate total weight
            float totalWeight = 0f;
            foreach (var wd in validDecorations)
            {
                totalWeight += wd.weight;
            }
            
            if (totalWeight <= 0f) return null;
            
            // Random value between 0 and totalWeight
            float randomValue = Random.Range(0f, totalWeight);
            
            // Find which decoration this value falls into
            float currentWeight = 0f;
            foreach (var wd in validDecorations)
            {
                currentWeight += wd.weight;
                if (randomValue <= currentWeight)
                {
                    return wd.prefab;
                }
            }
            
            // Fallback to last item (shouldn't happen, but safety)
            return validDecorations[validDecorations.Count - 1].prefab;
        }
    }
}
