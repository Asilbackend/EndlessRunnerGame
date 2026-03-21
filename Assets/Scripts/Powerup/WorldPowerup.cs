using UnityEngine;
using Utilities;
using World;

namespace Powerup
{
    /// <summary>
    /// In-world powerup pickup. Rotates like a coin, collected on player trigger.
    /// Place on a GameObject with a trigger Collider inside a WorldChunk.
    /// Set the powerupType field and assign a visual model child.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldPowerup : MonoBehaviour, IWorldObject
    {
        [Header("Powerup")]
        [SerializeField] private PowerupType powerupType;

        [Header("Visual")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobAmplitude = 0.25f;
        [SerializeField] private float bobFrequency = 2f;

        private WorldChunk _parentChunk;
        private bool _isActive = true;
        private bool _isCollected = false;
        private bool _isPaused = false;
        private bool _isReversed = false;
        private float _originalRotationSpeed;
        private float _bobPhase;
        private Vector3 _baseLocalPos;

        public PowerupType Type => powerupType;
        public bool IsCollected => _isCollected;

        private void Awake()
        {
            _parentChunk = GetComponentInParent<WorldChunk>();
            if (_parentChunk != null)
                _parentChunk.AddWorldObject(this);

            _originalRotationSpeed = rotationSpeed;
            _baseLocalPos = transform.localPosition;
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()  => PowerupManager.Instance?.RegisterPowerup(this);
        private void OnDisable() => PowerupManager.Instance?.UnregisterPowerup(this);

        private void Update()
        {
            if (!_isActive || _isCollected || _isPaused) return;

            // Rotate around Y
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // Bob up and down
            float yOffset = Mathf.Sin(Time.time * bobFrequency + _bobPhase) * bobAmplitude;
            Vector3 pos = transform.localPosition;
            pos.y = _baseLocalPos.y + yOffset;
            transform.localPosition = pos;
        }

        // IWorldObject — powerups are static in the chunk (chunk moves them)
        public void MoveWithWorld() { }

        public void OnDespawn()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }

        public void OnCollided()
        {
            if (_isCollected) return;
            _isCollected = true;

            // Apply the powerup effect
            if (PowerupManager.Instance != null)
                PowerupManager.Instance.Activate(powerupType, this);

            OnDespawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            if (other.CompareTag("Player"))
            {
                // Spawn collect VFX burst at pickup position (no coin text)
                ParticleEffectSpawner.SpawnParticleEffect(ParticleEffectType.PowerupCollect, transform.position);

                OnCollided();

                if (PlayerCameraController.Instance != null)
                    PlayerCameraController.Instance.PowerupBounce();
            }
        }

        // --- Pause / Resume / Reverse (same pattern as WorldCollectible) ---

        public void Pause() => _isPaused = true;

        public void Resume()
        {
            StopReverse();
            _isPaused = false;
        }

        public void Reverse()
        {
            if (_isReversed) return;
            _isReversed = true;
            _isPaused = false;
            float mult = GameController.Instance != null ? GameController.Instance.ReverseMultiplier : 2f;
            rotationSpeed = -Mathf.Abs(_originalRotationSpeed) * mult;
        }

        public void StopReverse()
        {
            if (!_isReversed) return;
            _isReversed = false;
            rotationSpeed = _originalRotationSpeed;
        }

        public void ResetPowerup()
        {
            _isActive = true;
            _isCollected = false;
            _isPaused = false;
            _isReversed = false;
            rotationSpeed = _originalRotationSpeed;
            gameObject.SetActive(true);

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            if (_parentChunk == null)
                _parentChunk = GetComponentInParent<WorldChunk>();
            if (_parentChunk != null)
                _parentChunk.AddWorldObject(this);
        }

        private void OnDestroy()
        {
            if (_parentChunk != null)
                _parentChunk.RemoveWorldObject(this);
        }
    }
}
