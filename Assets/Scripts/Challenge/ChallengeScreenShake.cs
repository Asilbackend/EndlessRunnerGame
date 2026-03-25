using UnityEngine;
using Unity.Cinemachine;

namespace Challenge
{
    /// <summary>
    /// Triggers Cinemachine Impulse for screen shake during the tap challenge.
    /// Attach to any GameObject (e.g. the ChallengeSystem object).
    /// Requires a CinemachineImpulseSource on the same GameObject.
    /// The active Cinemachine camera must have a CinemachineImpulseListener extension.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class ChallengeScreenShake : MonoBehaviour
    {
        public static ChallengeScreenShake Instance { get; private set; }

        [Header("Defaults")]
        [SerializeField] private float defaultForce = 0.5f;

        private CinemachineImpulseSource _impulseSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Trigger a shake impulse with optional custom force.</summary>
        public void Shake(float? force = null)
        {
            if (_impulseSource == null) return;
            _impulseSource.GenerateImpulse(force ?? defaultForce);
        }
    }
}
