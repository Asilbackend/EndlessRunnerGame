using System.Collections.Generic;
using UnityEngine;

namespace World
{
    [System.Serializable]
    public class ObjectData
    {
        [Header("Object Properties")]
        public string objectName = "New Object";
        public GameObject objectPrefab;
        public float damage = 0;
        [Tooltip("Relative spawn weight. Leave at 1 for equal probability. 0 = never selected.")]
        [Min(0f)] public float weight = 1f;
    }

    [CreateAssetMenu(fileName = "ObjectData", menuName = "World/Object Config", order = 2)]
    public class ObjectsContainerSO : ScriptableObject
    {
        public List<ObjectData> objectConfigs = new List<ObjectData>();
        public bool isMoving = false;

        public ObjectData GetRandomObject()
        {
            if (objectConfigs == null || objectConfigs.Count == 0)
            {
                Debug.LogWarning($"ObjectConfigSO '{name}' has no object configurations.");
                return null;
            }

            float totalWeight = 0f;
            foreach (var config in objectConfigs)
                totalWeight += config.weight;

            // All weights are 0: fall back to uniform random
            if (totalWeight <= 0f)
                return objectConfigs[Random.Range(0, objectConfigs.Count)];

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var config in objectConfigs)
            {
                cumulative += config.weight;
                if (roll < cumulative)
                    return config;
            }

            return objectConfigs[objectConfigs.Count - 1];
        }

        public ObjectData GetObjectByName(string name)
        {
            foreach (var config in objectConfigs)
            {
                if (config.objectName == name)
                {
                    return config;
                }
            }
            Debug.LogWarning($"ObjectConfigSO '{this.name}' has no object configuration with the name '{name}'.");
            return null;
        }
    }
}

