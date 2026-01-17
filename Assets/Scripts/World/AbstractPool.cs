using UnityEngine;
using System.Collections.Generic;

namespace World
{
    public abstract class AbstractPool<T> : MonoBehaviour where T : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] protected int initialPoolSize = 5;
        [SerializeField] protected int maxPoolSize = 50;

        protected class PoolData
        {
            public Queue<T> available = new Queue<T>();
            public List<T> allItems = new List<T>();
        }

        protected Dictionary<GameObject, PoolData> _pools = new Dictionary<GameObject, PoolData>();
        protected Transform _poolContainer;

        protected abstract string GetContainerName();

        protected virtual void Awake()
        {
            // Create or find pool container under the World object
            // Pools are typically on the World GameObject, so use this transform
            Transform worldTransform = transform;
            
            // If this component is on a child GameObject, use the parent (World object)
            if (transform.parent != null)
            {
                worldTransform = transform.parent;
            }
            
            // Find or create the main PooledObjects container
            Transform pooledObjectsContainer = worldTransform.Find("PooledObjects");
            if (pooledObjectsContainer == null)
            {
                GameObject pooledObjectsObj = new GameObject("PooledObjects");
                pooledObjectsObj.transform.SetParent(worldTransform);
                pooledObjectsContainer = pooledObjectsObj.transform;
            }
            
            // Find or create the specific container for this pool type
            string containerName = GetContainerName();
            _poolContainer = pooledObjectsContainer.Find(containerName);
            if (_poolContainer == null)
            {
                GameObject containerObj = new GameObject(containerName);
                containerObj.transform.SetParent(pooledObjectsContainer);
                _poolContainer = containerObj.transform;
            }
        }

        public abstract T Get(GameObject prefab);
        public abstract void Return(T item);
        
        protected abstract GameObject GetSourcePrefab(T item);
        protected abstract T CreateNew(GameObject prefab, PoolData pool);
        protected abstract void ResetItem(T item);

        protected T GetFromPool(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError($"{GetType().Name}: Cannot get item with null prefab!");
                return null;
            }

            // Get or create pool for this prefab
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new PoolData();
            }

            PoolData pool = _pools[prefab];
            T item = null;

            // Try to get from available pool
            if (pool.available.Count > 0)
            {
                item = pool.available.Dequeue();
                if (item != null) item.gameObject.SetActive(true);
            }
            else
            {
                // Create new item if under max size
                if (pool.allItems.Count < maxPoolSize)
                {
                    item = CreateNew(prefab, pool);
                    if (item != null) item.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"{GetType().Name}: Max pool size reached for prefab {prefab.name}! Consider increasing maxPoolSize.");
                    return null;
                }
            }

            return item;
        }

        protected void ReturnToPool(T item)
        {
            if (item == null) return;

            GameObject sourcePrefab = GetSourcePrefab(item);
            if (sourcePrefab == null)
            {
                Debug.LogWarning($"{GetType().Name}: Cannot return item - source prefab not found!");
                return;
            }

            if (!_pools.ContainsKey(sourcePrefab))
            {
                Debug.LogWarning($"{GetType().Name}: Pool not found for prefab {sourcePrefab.name}!");
                return;
            }

            // Ensure pool container is initialized
            if (_poolContainer == null)
            {
                // Re-initialize if container is missing
                Transform worldTransform = transform;
                if (transform.parent != null)
                {
                    worldTransform = transform.parent;
                }
                
                Transform pooledObjectsContainer = worldTransform.Find("PooledObjects");
                if (pooledObjectsContainer != null)
                {
                    string containerName = GetContainerName();
                    _poolContainer = pooledObjectsContainer.Find(containerName);
                    if (_poolContainer == null)
                    {
                        GameObject containerObj = new GameObject(containerName);
                        containerObj.transform.SetParent(pooledObjectsContainer);
                        _poolContainer = containerObj.transform;
                    }
                }
            }

            PoolData pool = _pools[sourcePrefab];
            ResetItem(item);
            
            // Always reparent to pool container (objects might be unparented or under composite)
            if (_poolContainer != null)
            {
                item.transform.SetParent(_poolContainer);
            }
            
            item.gameObject.SetActive(false);
            pool.available.Enqueue(item);
        }

        public int GetActiveCount()
        {
            int count = 0;
            foreach (var pool in _pools.Values)
            {
                count += pool.allItems.Count - pool.available.Count;
            }
            return count;
        }
    }
}
