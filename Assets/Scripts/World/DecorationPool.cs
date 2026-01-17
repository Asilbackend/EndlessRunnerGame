using UnityEngine;

namespace World
{
    public class DecorationPool : AbstractPool<Decoration>
    {
        protected override string GetContainerName()
        {
            return "Decorations";
        }

        public Decoration GetDecoration(GameObject prefab)
        {
            return Get(prefab);
        }

        public void ReturnDecoration(Decoration decoration)
        {
            Return(decoration);
        }

        public override Decoration Get(GameObject prefab)
        {
            return GetFromPool(prefab);
        }

        public override void Return(Decoration item)
        {
            ReturnToPool(item);
        }

        protected override GameObject GetSourcePrefab(Decoration item)
        {
            return item?.SourcePrefab;
        }

        protected override Decoration CreateNew(GameObject prefab, PoolData pool)
        {
            if (prefab == null) return null;

            Decoration decorationComponent = prefab.GetComponent<Decoration>();
            if (decorationComponent == null)
            {
                Debug.LogError($"DecorationPool: Prefab {prefab.name} does not have a Decoration component!");
                return null;
            }

            Decoration decoration = Instantiate(decorationComponent, _poolContainer);
            decoration.gameObject.SetActive(false);
            decoration.name = $"{prefab.name}_Instance_{pool.allItems.Count}";
            decoration.SourcePrefab = prefab;
            pool.allItems.Add(decoration);
            return decoration;
        }

        protected override void ResetItem(Decoration item)
        {
            // Reset decoration if needed
            if (item != null)
            {
                // Add any decoration-specific reset logic here
            }
        }
    }
}
