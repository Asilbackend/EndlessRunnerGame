using UnityEngine;

namespace World
{
    public class RoadPool : AbstractPool<Road>
    {
        protected override string GetContainerName()
        {
            return "Roads";
        }

        public Road GetRoad(GameObject prefab)
        {
            return Get(prefab);
        }

        public void ReturnRoad(Road road)
        {
            Return(road);
        }

        public override Road Get(GameObject prefab)
        {
            return GetFromPool(prefab);
        }

        public override void Return(Road item)
        {
            ReturnToPool(item);
        }

        protected override GameObject GetSourcePrefab(Road item)
        {
            return item?.SourcePrefab;
        }

        protected override Road CreateNew(GameObject prefab, PoolData pool)
        {
            if (prefab == null) return null;

            Road roadComponent = prefab.GetComponent<Road>();
            if (roadComponent == null)
            {
                Debug.LogError($"RoadPool: Prefab {prefab.name} does not have a Road component!");
                return null;
            }

            Road road = Instantiate(roadComponent, _poolContainer);
            road.gameObject.SetActive(false);
            road.name = $"{prefab.name}_Instance_{pool.allItems.Count}";
            road.SourcePrefab = prefab;
            pool.allItems.Add(road);
            return road;
        }

        protected override void ResetItem(Road item)
        {
            // Reset road if needed
            if (item != null)
            {
                // Add any road-specific reset logic here
            }
        }
    }
}
