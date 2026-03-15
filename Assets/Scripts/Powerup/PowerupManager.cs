using System.Collections;
using UnityEngine;
using Utilities;
using World;

namespace Powerup
{
    /// <summary>
    /// Central powerup manager. Owns all active powerup state, timers, and effect logic.
    /// Attach to the same GameObject as GameController.
    /// </summary>
    public class PowerupManager : MonoBehaviour
    {
        public static PowerupManager Instance { get; private set; }

        [Header("Coin Magnet")]
        [SerializeField] private float magnetDuration = 8f;
        [SerializeField] private float magnetRadius = 12f;
        [SerializeField] private float magnetPullSpeed = 40f;
        [SerializeField] private float magnetAutoCollectRadius = 1.5f;

        [Header("Double Lane Change Speed")]
        [SerializeField] private float doubleLaneDuration = 8f;
        [SerializeField] private float doubleLaneSpeedMultiplier = 2.5f;

        [Header("Double Coin")]
        [SerializeField] private float doubleCoinDuration = 10f;

        [Header("Invincibility")]
        [SerializeField] private float invincibilityDuration = 6f;

        // --- Active timers ---
        private float _magnetTimer;
        private float _doubleLaneTimer;
        private float _doubleCoinTimer;
        private float _invincibilityTimer;

        // VFX instances that persist while effects are active
        private GameObject _magnetVFX;
        private GameObject _doubleLaneVFX;
        private GameObject _invincibilityVFX;
        private GameObject _doubleCoinVFX;

        // Public queries
        public bool  IsMagnetActive             => _magnetTimer > 0f;
        public bool  IsDoubleLaneActive         => _doubleLaneTimer > 0f;
        public bool  IsDoubleCoinActive         => _doubleCoinTimer > 0f;
        public bool  IsInvincible               => _invincibilityTimer > 0f;
        public int   CoinMultiplier             => IsDoubleCoinActive ? 2 : 1;
        public float LaneChangeSpeedMultiplier  => IsDoubleLaneActive ? doubleLaneSpeedMultiplier : 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_magnetTimer > 0f)
            {
                _magnetTimer -= dt;
                PullNearbyCoins();
                if (_magnetTimer <= 0f) EndTimedEffect(ref _magnetVFX, ref _magnetTimer);
            }

            if (_doubleLaneTimer > 0f)
            {
                _doubleLaneTimer -= dt;
                if (_doubleLaneTimer <= 0f) EndTimedEffect(ref _doubleLaneVFX, ref _doubleLaneTimer);
            }

            if (_doubleCoinTimer > 0f)
            {
                _doubleCoinTimer -= dt;
                if (_doubleCoinTimer <= 0f) EndTimedEffect(ref _doubleCoinVFX, ref _doubleCoinTimer);
            }

            if (_invincibilityTimer > 0f)
            {
                _invincibilityTimer -= dt;
                if (_invincibilityTimer <= 0f) EndTimedEffect(ref _invincibilityVFX, ref _invincibilityTimer);
            }
        }

        // ===================================================================
        //  PUBLIC: Activate a powerup
        // ===================================================================

        public void Activate(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.CoinMagnet:      ActivateMagnet();        break;
                case PowerupType.ExtraHealth:      ActivateExtraHealth();   break;
                case PowerupType.DoubleLaneChange: ActivateDoubleLane();    break;
                case PowerupType.DoubleCoin:       ActivateDoubleCoin();    break;
                case PowerupType.Invincibility:    ActivateInvincibility(); break;
            }
        }

        /// <summary>Clear all active effects (called on game over / reset).</summary>
        public void ClearAll()
        {
            EndTimedEffect(ref _magnetVFX,       ref _magnetTimer);
            EndTimedEffect(ref _doubleLaneVFX,   ref _doubleLaneTimer);
            EndTimedEffect(ref _doubleCoinVFX,   ref _doubleCoinTimer);
            EndTimedEffect(ref _invincibilityVFX, ref _invincibilityTimer);
        }

        // ===================================================================
        //  COIN MAGNET
        // ===================================================================

        private void ActivateMagnet()
        {
            _magnetTimer = magnetDuration;
            AudioManager.Instance?.PlaySFX(AudioEventSFX.PowerupMagnet);
            SpawnLoopingVFX(ParticleEffectType.PowerupMagnet, ref _magnetVFX);
        }

        private void PullNearbyCoins()
        {
            var player = GameController.Instance?.PlayerController;
            if (player == null) return;

            Vector3 playerPos = player.transform.position;
            var hits = Physics.OverlapSphere(playerPos, magnetRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var collectible = hits[i].GetComponent<WorldCollectible>();
                if (collectible == null) continue;

                Vector3 toPlayer = playerPos - collectible.transform.position;
                float dist = toPlayer.magnitude;

                if (dist < magnetAutoCollectRadius)
                {
                    collectible.OnCollided();
                    if (PlayerCameraController.Instance != null)
                        PlayerCameraController.Instance.PowerupBounce();
                }
                else
                {
                    collectible.transform.position += toPlayer.normalized * magnetPullSpeed * Time.deltaTime;
                }
            }
        }

        // ===================================================================
        //  EXTRA HEALTH
        // ===================================================================

        private void ActivateExtraHealth()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.PowerupHealth);

            var gc = GameController.Instance;
            if (gc != null)
                gc.SetHealth(gc.GameHealth + 1);

            // One-shot burst VFX — lives exactly 1 second
            var player = gc?.PlayerController;
            if (player != null)
            {
                var vfx = ParticleEffectSpawner.SpawnParticleEffect(
                    ParticleEffectType.PowerupHealth, player.transform.position,
                    Quaternion.identity, player.transform);
                if (vfx != null)
                    Destroy(vfx, 1f);
            }
        }

        // ===================================================================
        //  DOUBLE LANE CHANGE SPEED
        // ===================================================================

        private void ActivateDoubleLane()
        {
            _doubleLaneTimer = doubleLaneDuration;
            AudioManager.Instance?.PlaySFX(AudioEventSFX.PowerupDoubleLane);
            SpawnLoopingVFX(ParticleEffectType.PowerupDoubleLane, ref _doubleLaneVFX);
        }

        // ===================================================================
        //  DOUBLE COIN
        // ===================================================================

        private void ActivateDoubleCoin()
        {
            _doubleCoinTimer = doubleCoinDuration;
            AudioManager.Instance?.PlaySFX(AudioEventSFX.PowerupDoubleCoin);
            SpawnLoopingVFX(ParticleEffectType.PowerupDoubleCoin, ref _doubleCoinVFX);
        }

        // ===================================================================
        //  INVINCIBILITY
        // ===================================================================

        private void ActivateInvincibility()
        {
            _invincibilityTimer = invincibilityDuration;
            AudioManager.Instance?.PlaySFX(AudioEventSFX.PowerupInvincibility);
            SpawnLoopingVFX(ParticleEffectType.PowerupInvincibility, ref _invincibilityVFX);
        }

        // ===================================================================
        //  CHECKPOINT INVINCIBILITY
        // ===================================================================

        /// <summary>
        /// Activates real invincibility (obstacles deflect) for <paramref name="duration"/> seconds
        /// with the invincibility VFX. Used after a checkpoint restart.
        /// Does not play the powerup SFX. Does not shorten an already-longer invincibility window.
        /// </summary>
        public void ActivateCheckpointInvincibility(float duration)
        {
            _invincibilityTimer = Mathf.Max(_invincibilityTimer, duration);
            SpawnLoopingVFX(ParticleEffectType.PowerupInvincibility, ref _invincibilityVFX);
        }

        // ===================================================================
        //  VFX helpers
        // ===================================================================

        /// <summary>
        /// Instantiate a looping VFX parented to the player.
        /// Bypasses ParticleEffectSpawner to avoid auto-destroy; we control the lifetime.
        /// Forces loop=true on every ParticleSystem in the hierarchy.
        /// </summary>
        private void SpawnLoopingVFX(ParticleEffectType type, ref GameObject vfxRef)
        {
            // Kill any existing instance immediately (restacking refreshes)
            if (vfxRef != null)
            {
                Destroy(vfxRef);
                vfxRef = null;
            }

            var player = GameController.Instance?.PlayerController;
            if (player == null) return;

            var so = GameController.Instance?.ParticleEffectsSO;
            if (so == null) return;
            var prefab = so.GetParticleEffect(type);
            if (prefab == null) return;

            vfxRef = Instantiate(prefab, player.transform.position, Quaternion.identity, player.transform);

            // Force loop on all child particle systems
            foreach (var ps in vfxRef.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.loop = true;
                main.stopAction = ParticleSystemStopAction.None;
            }
        }

        /// <summary>
        /// Stop emission so existing particles die naturally, then destroy. Sets timer to 0.
        /// </summary>
        private void EndTimedEffect(ref GameObject vfxRef, ref float timer)
        {
            timer = 0f;
            if (vfxRef != null)
                StartCoroutine(FadeOutAndDestroy(vfxRef));
            vfxRef = null;
        }

        private IEnumerator FadeOutAndDestroy(GameObject vfx)
        {
            if (vfx == null) yield break;

            // Stop emission — existing particles finish their lifetime naturally (smooth fade)
            float maxLifetime = 0f;
            foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                float lt = ps.main.startLifetime.constantMax;
                if (lt > maxLifetime) maxLifetime = lt;
            }

            yield return new WaitForSeconds(Mathf.Max(maxLifetime, 0.1f));

            if (vfx != null)
                Destroy(vfx);
        }
    }
}
