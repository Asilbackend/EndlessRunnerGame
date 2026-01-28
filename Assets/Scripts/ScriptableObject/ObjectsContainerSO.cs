using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace World
{
    [System.Serializable]
    public class ObjectData
    {
        [Header("Object Properties")]
        public string objectName = "New Object";
        public GameObject objectPrefab;
        public float speed = 0;
        public float damage = 0;
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

            int randomIndex = Random.Range(0, objectConfigs.Count);
            return objectConfigs[randomIndex];
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

        public ObjectData GetRandomObjectBySpeed(float speed)
        {
            if (isMoving == false)
            {
                return null;
            }
            var matchingConfigs = objectConfigs.Where(config => config.speed == speed);

            if (matchingConfigs.Any())
            {
                int randomIndex = Random.Range(0, matchingConfigs.Count());
                return matchingConfigs.ElementAt(randomIndex);
            }

            Debug.LogWarning($"ObjectConfigSO '{this.name}' has no object configuration with the speed '{speed}'.");
            return null;
        }

        public ObjectData GetRandomObjectBySpeeds(IList<float> speeds)
        {
            if (isMoving == false || speeds == null || speeds.Count == 0)
            {
                return null;
            }
            // Shuffle to try speeds in random order, then return first valid result
            var order = Enumerable.Range(0, speeds.Count).OrderBy(_ => Random.value).ToList();
            foreach (int i in order)
            {
                float s = speeds[i];
                var result = GetRandomObjectBySpeed(s);
                if (result != null)
                    return result;
            }
            Debug.LogWarning($"ObjectConfigSO '{this.name}' has no object configuration for any of the speeds [{string.Join(", ", speeds)}].");
            return null;
        }
    }
}

