using UnityEngine;

namespace Challenge
{
    /// <summary>
    /// Serializable difficulty preset for a tap challenge.
    /// Assigned on ChallengeTrigger to define per-zone difficulty.
    /// </summary>
    [System.Serializable]
    public class ChallengeDifficulty
    {
        [Header("Targets")]
        [Tooltip("Number of targets the player must tap.")]
        [Range(1, 10)]
        public int targetCount = 3;

        [Header("Timing")]
        [Tooltip("Total challenge duration in real (unscaled) seconds.")]
        public float duration = 5f;

        [Tooltip("Time scale during the challenge (lower = slower).")]
        [Range(0.05f, 1f)]
        public float slowMotionScale = 0.3f;

        [Header("Reward")]
        [Tooltip("Base coin reward for completing the challenge.")]
        public int baseReward = 50;

        [Tooltip("Bonus coins multiplied by remaining-time factor (0–1).")]
        public float timeBonusMultiplier = 100f;
    }
}
