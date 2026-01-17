using UnityEngine;

namespace World
{
    public class ChunkPool : AbstractPool<WorldChunk>
    {
        protected override string GetContainerName()
        {
            return "Chunks";
        }

        public WorldChunk GetChunk(GameObject prefab)
        {
            return Get(prefab);
        }

        public void ReturnChunk(WorldChunk chunk)
        {
            Return(chunk);
        }

        public override WorldChunk Get(GameObject prefab)
        {
            return GetFromPool(prefab);
        }

        public override void Return(WorldChunk item)
            {
            ReturnToPool(item);
        }

        protected override GameObject GetSourcePrefab(WorldChunk item)
        {
            return item?.SourcePrefab;
        }

        protected override WorldChunk CreateNew(GameObject prefab, PoolData pool)
        {
            if (prefab == null) return null;

            WorldChunk chunkComponent = prefab.GetComponent<WorldChunk>();
            if (chunkComponent == null)
            {
                Debug.LogError($"ChunkPool: Prefab {prefab.name} does not have a WorldChunk component!");
                return null;
            }

            WorldChunk chunk = Instantiate(chunkComponent, _poolContainer);
            chunk.gameObject.SetActive(false);
            chunk.name = $"{prefab.name}_Instance_{pool.allItems.Count}";
            chunk.SourcePrefab = prefab;
            pool.allItems.Add(chunk);
            return chunk;
        }

        protected override void ResetItem(WorldChunk item)
        {
            if (item != null)
            {
                item.ResetChunk();
            }
        }

        public int GetActiveChunkCount()
        {
            return GetActiveCount();
        }
    }
}

