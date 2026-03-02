using System.Collections;
using UnityEngine;

namespace World
{
    public class WorldMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseSpeed = 10f;
        [SerializeField] private bool useAcceleration = true;
        [SerializeField] private float accelerationRate = 0.1f;
        [SerializeField] private float maxSpeed = 30f;
        
        private float _currentSpeed;
        private float _totalDistanceTraveled = 0f;
        private bool _isBeginning = true;

        public float CurrentSpeed => _currentSpeed;
        public float BaseSpeed => baseSpeed;
        public float TotalDistanceTraveled => _totalDistanceTraveled;

        private void Awake()
        {
            _currentSpeed = baseSpeed;
        }

        private void Start()
        {
            _currentSpeed = baseSpeed;
        }
        
        // Side bend control
        [Header("Side Bend Control")]
        [SerializeField] private float bendAmplitude = 1f; // max amplitude (will be clamped to [-2.5,2.5])
        [SerializeField] private float bendSmoothTime = 0.35f; // smoothing for runtime updates
        [Tooltip("Chance [0..1] that after a straight period the system will start a bend. Lower = more often straight.")]
        [Range(0f,1f)]
        [SerializeField] private float bendChance = 0.1f;
        [Tooltip("Chance [0..1] that after a hold the next action will be a direct opposite bend instead of returning to straight")]
        [Range(0f,1f)]
        [SerializeField] private float flipToOppositeChance = 0.25f;

        // Timing
        [Header("Timing")]
        [Tooltip("Time between bends (straight) in seconds - randomized between min and max")]
        [SerializeField] private float minStraightDuration = 10f;
        [SerializeField] private float maxStraightDuration = 15f;
        [Tooltip("Time the road stays at bend value (seconds)")]
        [SerializeField] private float minHoldDuration = 5f;
        [SerializeField] private float maxHoldDuration = 12f;
        [Tooltip("Approach duration (seconds) - faster than hold")]
        [SerializeField] private float minApproachDuration = 0.6f;
        [SerializeField] private float maxApproachDuration = 1.2f;
        [Tooltip("Return duration (seconds) - faster than approach")]
        [SerializeField] private float minReturnDuration = 0.4f;
        [SerializeField] private float maxReturnDuration = 0.9f;

        private float _bendCurrent = 0f;
        private float _bendVelocity = 0f;

        private ShaderGlobals _shaderGlobals;

        private enum BendState { Straight, Approach, Hold, Return }
        private BendState _bendState = BendState.Straight;
        private float _stateTimer = 0f;
        private float _stateDuration = 0f;

        // target magnitude for current bend (signed)
        private float _targetBendValue = 0f;
        // when true, Approach will reuse _targetBendValue instead of picking a new one
        private bool _useExistingTargetForApproach = false;

        // optimization: cache last value applied to shader to avoid redundant Set calls
        private float _lastAppliedBend = float.NaN;

        private void OnEnable()
        {
            // cache shader globals once
            _shaderGlobals = FindObjectOfType<ShaderGlobals>();
            if (_shaderGlobals != null)
            {
                _bendCurrent = _shaderGlobals.SideBendStrength; // intentionally use persisted baseline
                _lastAppliedBend = float.NaN; // force first update
            }

            EnterState(BendState.Straight);
        }

        // EnterState optionally can reuse existing target when entering Approach
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
                        // pick direction and magnitude for this bend within requested ranges
                        float mag = Random.Range(1.5f, 2.5f);
                        // randomly choose left or right
                        _targetBendValue = (Random.value < 0.5f ? -1f : 1f) * mag;
                    }
                    // ensure within absolute max
                    _targetBendValue = Mathf.Clamp(_targetBendValue, -2.5f, 2.5f);
                    break;
                case BendState.Hold:
                    _stateDuration = Random.Range(minHoldDuration, maxHoldDuration);
                    // keep previously chosen _targetBendValue
                    break;
                case BendState.Return:
                    _stateDuration = Random.Range(minReturnDuration, maxReturnDuration);
                    break;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float movement = _currentSpeed * deltaTime;

            _totalDistanceTraveled += movement;
            
            if (useAcceleration && _isBeginning)
            {
                _currentSpeed = Mathf.Min(_currentSpeed + accelerationRate * deltaTime, maxSpeed);
                if (_currentSpeed >= maxSpeed)
                {
                    _currentSpeed = maxSpeed;
                    _isBeginning = false;
                    baseSpeed = maxSpeed;
                }
            }

            // If the game is over, stop bending and force zero once
            if (GameController.Instance != null && GameController.Instance.IsGameOver)
            {
                return;
            }

            if (_shaderGlobals == null) return;

            _stateTimer += deltaTime;
            // handle state transitions
            if (_stateTimer >= _stateDuration)
            {
                switch (_bendState)
                {
                    case BendState.Straight:
                        // bias heavily to remain straight: only start a bend with bendChance
                        if (Random.value < bendChance)
                        {
                            EnterState(BendState.Approach);
                        }
                        else
                        {
                            // extend straight period without extra branching: reset straight state
                            EnterState(BendState.Straight);
                        }
                        break;
                    case BendState.Approach:
                        EnterState(BendState.Hold);
                        break;
                    case BendState.Hold:
                        // decide whether to return to zero or go directly to an opposite bend
                        if (Random.value < flipToOppositeChance)
                        {
                            // flip sign and slightly vary magnitude
                            float mag = Mathf.Abs(_targetBendValue);
                            if (mag < 1.5f) mag = Random.Range(1.5f, 2.5f); // ensure magnitude in range
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

            // compute desired target based on state and progress
            float desiredTarget = 0f;
            if (_bendState == BendState.Straight)
            {
                desiredTarget = 0f;
            }
            else if (_bendState == BendState.Approach)
            {
                float p = Mathf.Clamp01(_stateTimer / Mathf.Max(_stateDuration, 0.0001f));
                // ease-out for fast approach:1 - (1-p)^3
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                desiredTarget = _targetBendValue * eased;
            }
            else if (_bendState == BendState.Hold)
            {
                desiredTarget = _targetBendValue;
            }
            else // Return
            {
                float p = Mathf.Clamp01(_stateTimer / Mathf.Max(_stateDuration, 0.0001f));
                // fast return: value = target * (1 - easeFast), use (1-p)^3 to drop quickly
                float eased = Mathf.Pow(1f - p, 3f);
                desiredTarget = _targetBendValue * eased;
            }

            // clamp
            desiredTarget = Mathf.Clamp(desiredTarget, -2.5f, 2.5f);

            // Optimization: if straight and already essentially zero, avoid SmoothDamp and Set calls
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

            // smooth toward target
            _bendCurrent = Mathf.SmoothDamp(_bendCurrent, desiredTarget, ref _bendVelocity, bendSmoothTime);

            // only update shader if changed enough
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
        }
        
        public void ResetSpeed()
        {
            _currentSpeed = baseSpeed;
            _totalDistanceTraveled = 0f;
        }
        
        public void Pause()
        {
            _currentSpeed = 0f;
        }
        
        public void Resume()
        {
            if (_currentSpeed <= 0f) _currentSpeed = baseSpeed;
        }

        public void Reverse()
        {
            _currentSpeed = -1 * baseSpeed * GameController.Instance.ReverseMultiplier;
        }
    }
}
