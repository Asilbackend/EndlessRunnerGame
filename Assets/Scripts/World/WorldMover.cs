using UnityEngine;

namespace World
{
    public class WorldMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseSpeed = 15f;
        [SerializeField] private float maxSpeed = 50f;
        [Tooltip("Number of chunks (levels) to go from baseSpeed to maxSpeed. Implicit rate = (max - base) / levels.")]
        [SerializeField] private int levelsToMax = 50;

        // Computed in Awake — visible in Inspector debug view as a sanity check.
        private float accelerationRate;

        private float _currentSpeed;
        private float _totalDistanceTraveled = 0f;
        private int _chunksPassedCount = 0;
        private bool _isPaused = false;
        private bool _isReversing = false;

        public float CurrentSpeed => _currentSpeed;
        public float TotalDistanceTraveled => _totalDistanceTraveled;

        /// <summary>
        /// Normalised progress from baseSpeed to maxSpeed (0 = start, 1 = max).
        /// Mapped to 1..2 so existing music pitch code (pitchProgress = multiplier - 1) still works.
        /// </summary>
        public float CurrentSpeedMultiplier
        {
            get
            {
                float t = Mathf.Clamp01((float)_chunksPassedCount / Mathf.Max(1, levelsToMax));
                return Mathf.Lerp(1f, 2f, t);
            }
        }

        private void Awake()
        {
            accelerationRate = (maxSpeed - baseSpeed) / Mathf.Max(1, levelsToMax);
            _currentSpeed = baseSpeed;
        }

        // Side bend control
        [Header("Side Bend Control")]
        [SerializeField] private float bendSmoothTime = 0.35f;
        [Tooltip("Chance [0..1] that after a straight period the system will start a bend. Lower = more often straight.")]
        [Range(0f,1f)]
        [SerializeField] private float bendChance = 0.1f;
        [Tooltip("Chance [0..1] that after a hold the next action will be a direct opposite bend instead of returning to straight")]
        [Range(0f,1f)]
        [SerializeField] private float flipToOppositeChance = 0.25f;

        [Header("Timing")]
        [Tooltip("Time between bends (straight) in seconds - randomized between min and max")]
        [SerializeField] private float minStraightDuration = 10f;
        [SerializeField] private float maxStraightDuration = 15f;
        [Tooltip("Time the road stays at bend value (seconds)")]
        [SerializeField] private float minHoldDuration = 5f;
        [SerializeField] private float maxHoldDuration = 12f;
        [Tooltip("Approach duration (seconds)")]
        [SerializeField] private float minApproachDuration = 0.6f;
        [SerializeField] private float maxApproachDuration = 1.2f;
        [Tooltip("Return duration (seconds)")]
        [SerializeField] private float minReturnDuration = 0.4f;
        [SerializeField] private float maxReturnDuration = 0.9f;

        private float _bendCurrent = 0f;
        private float _bendVelocity = 0f;

        private ShaderGlobals _shaderGlobals;

        private enum BendState { Straight, Approach, Hold, Return }
        private BendState _bendState = BendState.Straight;
        private float _stateTimer = 0f;
        private float _stateDuration = 0f;

        private float _targetBendValue = 0f;
        private bool _useExistingTargetForApproach = false;

        private float _lastAppliedBend = float.NaN;

        private void OnEnable()
        {
            _shaderGlobals = FindObjectOfType<ShaderGlobals>();
            if (_shaderGlobals != null)
            {
                _bendCurrent = _shaderGlobals.SideBendStrength;
                _lastAppliedBend = float.NaN;
            }

            EnterState(BendState.Straight);
        }

        private void EnterState(BendState newState, bool reuseExistingTarget = false)
        {
            _bendState = newState;
            _stateTimer = 0f;
            _useExistingTargetForApproach = reuseExistingTarget;

            switch (_bendState)
            {
                case BendState.Straight:
                    _stateDuration = Random.Range(minStraightDuration, maxStraightDuration);
                    _targetBendValue = 0f;
                    break;
                case BendState.Approach:
                    _stateDuration = Random.Range(minApproachDuration, maxApproachDuration);
                    if (!_useExistingTargetForApproach)
                    {
                        float mag = Random.Range(1.5f, 2.5f);
                        _targetBendValue = (Random.value < 0.5f ? -1f : 1f) * mag;
                    }
                    _targetBendValue = Mathf.Clamp(_targetBendValue, -2.5f, 2.5f);
                    break;
                case BendState.Hold:
                    _stateDuration = Random.Range(minHoldDuration, maxHoldDuration);
                    break;
                case BendState.Return:
                    _stateDuration = Random.Range(minReturnDuration, maxReturnDuration);
                    break;
            }
        }

        private void Update()
        {
            if (GameController.Instance != null && GameController.Instance.IsGameOver)
                return;

            float deltaTime = Time.deltaTime;
            _totalDistanceTraveled += _currentSpeed * deltaTime;

            if (!_isPaused && !_isReversing)
            {
                float t = Mathf.Clamp01((float)_chunksPassedCount / Mathf.Max(1, levelsToMax));
                _currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, t);
            }

            if (_shaderGlobals == null) return;

            _stateTimer += deltaTime;
            if (_stateTimer >= _stateDuration)
            {
                switch (_bendState)
                {
                    case BendState.Straight:
                        EnterState(Random.value < bendChance ? BendState.Approach : BendState.Straight);
                        break;
                    case BendState.Approach:
                        EnterState(BendState.Hold);
                        break;
                    case BendState.Hold:
                        if (Random.value < flipToOppositeChance)
                        {
                            float mag = Mathf.Abs(_targetBendValue);
                            if (mag < 1.5f) mag = Random.Range(1.5f, 2.5f);
                            else mag = Mathf.Clamp(mag * Random.Range(0.9f, 1.1f), 1.5f, 2.5f);
                            _targetBendValue = -Mathf.Sign(_targetBendValue == 0f ? 1f : _targetBendValue) * mag;
                            EnterState(BendState.Approach, true);
                        }
                        else
                        {
                            EnterState(BendState.Return);
                        }
                        break;
                    case BendState.Return:
                        EnterState(BendState.Straight);
                        break;
                }
            }

            float desiredTarget = 0f;
            if (_bendState == BendState.Approach)
            {
                float p = Mathf.Clamp01(_stateTimer / Mathf.Max(_stateDuration, 0.0001f));
                desiredTarget = _targetBendValue * (1f - Mathf.Pow(1f - p, 3f));
            }
            else if (_bendState == BendState.Hold)
            {
                desiredTarget = _targetBendValue;
            }
            else if (_bendState == BendState.Return)
            {
                float p = Mathf.Clamp01(_stateTimer / Mathf.Max(_stateDuration, 0.0001f));
                desiredTarget = _targetBendValue * Mathf.Pow(1f - p, 3f);
            }

            desiredTarget = Mathf.Clamp(desiredTarget, -2.5f, 2.5f);

            if (_bendState == BendState.Straight && Mathf.Abs(_bendCurrent) < 0.0005f)
            {
                _bendCurrent = 0f;
                _bendVelocity = 0f;
                if (!Mathf.Approximately(_lastAppliedBend, 0f))
                {
                    _shaderGlobals.SetSideBendValue(0f);
                    _lastAppliedBend = 0f;
                }
                return;
            }

            _bendCurrent = Mathf.SmoothDamp(_bendCurrent, desiredTarget, ref _bendVelocity, bendSmoothTime);

            if (float.IsNaN(_lastAppliedBend) || Mathf.Abs(_bendCurrent - _lastAppliedBend) > 0.0005f)
            {
                _shaderGlobals.SetSideBendValue(_bendCurrent);
                _lastAppliedBend = _bendCurrent;
            }
        }

        public float GetMovementDelta()
        {
            return _currentSpeed * Time.deltaTime;
        }

        public void SetSpeed(float speed)
        {
            maxSpeed = speed;
            accelerationRate = (maxSpeed - baseSpeed) / Mathf.Max(1, levelsToMax);
        }

        public void ResetSpeed()
        {
            _currentSpeed = baseSpeed;
            _totalDistanceTraveled = 0f;
            _chunksPassedCount = 0;
            _isPaused = false;
            _isReversing = false;
        }

        public void OnChunkPassed()
        {
            _chunksPassedCount++;
        }

        public void Pause()
        {
            _isPaused = true;
            _isReversing = false;
            _currentSpeed = 0f;
        }

        public void Resume()
        {
            _isPaused = false;
            _isReversing = false;
            float t = Mathf.Clamp01((float)_chunksPassedCount / Mathf.Max(1, levelsToMax));
            _currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, t);
        }

        public void Reverse()
        {
            float reverseMultiplier = GameController.Instance != null ? GameController.Instance.ReverseMultiplier : 2f;
            _isPaused = false;
            _isReversing = true;
            _currentSpeed = -maxSpeed * reverseMultiplier;
        }

        public void ResetBend()
        {
            _bendCurrent = 0f;
            _bendVelocity = 0f;
            _lastAppliedBend = float.NaN;
            EnterState(BendState.Straight);
            if (_shaderGlobals != null)
            {
                _shaderGlobals.SetSideBendValue(0f);
                _lastAppliedBend = 0f;
            }
        }
    }
}
