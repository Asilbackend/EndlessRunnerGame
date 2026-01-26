using UnityEngine;

namespace World
{
    [System.Serializable]
    public class OppositeDynamicObstacleConfig
    {
        [Tooltip("Distance in meters before impact where sign appears")]
        public float metersBeforeImpact = 20f;
        
        [Tooltip("Distance in meters from chunk start where obstacle starts moving (n)")]
        public float obstacleStartAtMeters = 10f;
        
        [Tooltip("Lane where the obstacle will spawn")]
        public LaneNumber lane = LaneNumber.Center;

        public bool IsValid()
        {
            return obstacleStartAtMeters >= 0 && metersBeforeImpact > 0;
        }
    }
}
