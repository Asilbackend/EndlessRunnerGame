using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public enum PlayableObjectName
    {
        Skateboard,
        Bicycle,
        Scooter,
    }

    [System.Serializable]
    public class PlayableObjectData
    {
        [Header("Identity")]
        [Tooltip("Unique id for save/load (e.g. \"Skateboard\"). If empty, name enum is used.")]
        public string id;
        public PlayableObjectName name = PlayableObjectName.Skateboard;
        [Tooltip("Display name in menu (e.g. \"Street Skateboard\")")]
        public string displayName;

        [Header("Menu Visuals")]
        public Sprite icon;
        [Tooltip("Optional 3D preview in menu")]
        public GameObject previewPrefab;

        [Header("Stats (for UI, 1-5 stars)")]
        [Range(1, 5)] public int speedStars = 3;
        [Range(1, 5)] public int controlStars = 3;

        [Header("Gameplay")]
        public GameObject prefab;
        public float laneChangeSpeed = 10;
        public float speed = 0;
        public int health = 0;

        [Header("Unlock")]
        public bool lockedByDefault;

        /// <summary>Effective id for save/load: uses id if set, otherwise name enum.</summary>
        public string EffectiveId => string.IsNullOrEmpty(id) ? name.ToString() : id;
    }

    [CreateAssetMenu(fileName = "PlayableObjects", menuName = "World/Playable Objects", order = 1)]
    public class PlayableObjectsSO : ScriptableObject
    {
        public List<PlayableObjectData> PlayableObjectDatas = new List<PlayableObjectData>();

        public PlayableObjectData GetPlayableObjectDataByName(PlayableObjectName playableObjectName)
        {
            return PlayableObjectDatas.Find(obj => obj.name == playableObjectName);
        }

        public PlayableObjectData GetPlayableObjectDataById(string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId)) return null;
            var data = PlayableObjectDatas.Find(obj => obj.EffectiveId == vehicleId);
            if (data != null) return data;
            return PlayableObjectDatas.Find(obj => obj.name.ToString() == vehicleId);
        }
    }
}
