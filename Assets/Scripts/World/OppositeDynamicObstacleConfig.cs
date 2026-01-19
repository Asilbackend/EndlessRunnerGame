using UnityEngine;

namespace World
{
    [System.Serializable]
    public class OppositeDynamicObstacleConfig
    {
        [Tooltip("Distance in meters from chunk start where sign appears (m)")]
        public float signAppearAtMeters = 5f;
        
        [Tooltip("Distance in meters from chunk start where obstacle starts moving (n). Must be > signAppearAtMeters")]
        public float obstacleStartAtMeters = 10f;
        
        [Tooltip("Lane where the obstacle will spawn")]
        public LaneNumber lane = LaneNumber.Center;

        public bool IsValid()
        {
            return obstacleStartAtMeters > signAppearAtMeters && signAppearAtMeters >= 0;
        }
    }
}
