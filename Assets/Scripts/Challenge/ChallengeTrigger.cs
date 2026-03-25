using UnityEngine;

namespace Challenge
{
    /// <summary>
    /// Place this on a world object with a trigger collider.
    /// When the player enters, it starts the tap challenge with the configured difficulty.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ChallengeTrigger : MonoBehaviour
    {
        [Header("Difficulty")]
        [Tooltip("Difficulty preset for this challenge zone. Tweak per-trigger in the Inspector.")]
        [SerializeField] private ChallengeDifficulty difficulty = new ChallengeDifficulty();

        private bool _triggered = false;

        // Reset when the parent chunk is recycled and re-activated
        private void OnEnable()
        {
            _triggered = false;
        }

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;

            // Don't trigger during game over
            if (GameController.Instance != null && GameController.Instance.IsGameOver) return;

            var manager = TapChallengeManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("ChallengeTrigger: TapChallengeManager.Instance is null.");
                return;
            }

            // Don't start if a challenge is already active
            if (manager.IsActive) return;

            _triggered = true;
            manager.StartChallenge(difficulty);
        }

        /// <summary>Reset so the trigger can fire again (e.g. after checkpoint rewind).</summary>
        public void ResetTrigger()
        {
            _triggered = false;
        }
    }
}
