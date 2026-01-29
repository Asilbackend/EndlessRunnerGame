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
    }
}

