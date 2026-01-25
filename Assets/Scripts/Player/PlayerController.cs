using Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using World;
using Utilities;
using static UnityEngine.Rendering.DebugUI;

// Simple player controller that can move only on three lanes (Left, Center, Right)
public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayableObjectName playableObjectName = PlayableObjectName.Skateboard;

    [Header("Jump")]
    [Tooltip("Initial upward velocity applied when jumping. Tune this value to adjust jump height.")]
    [SerializeField] private float jumpVelocity = 8f;
    [Tooltip("Gravity applied to the player (negative value). Tune to change fall speed.")]
    [SerializeField] private float gravity = -30f;
    [Tooltip("Additional gravity multiplier when falling (after reaching peak height). Makes falling faster.")]
    [SerializeField] private float fallGravityMultiplier = 4f;

    [Tooltip("Speed at which velocity values smoothly transition (for velocity-based mode)")]
    [SerializeField] private float velocitySmoothingSpeed = 10f;

    [SerializeField] private List<Material> playerMaterials;

    // New: speed used to smoothly drive animator parameters to zero when reversing
    [SerializeField] private float animatorZeroingSpeed = 4f;

    public enum AnimatorMode
    {
        PositionBased,
        VelocityBased
    }
    private AnimatorMode _animatorMode = AnimatorMode.PositionBased;
    private float _laneChangeSpeed = 10f;

    private int _playerHealth = 1;
    public int PlayerHealth
    {
        get => _playerHealth;
        set => _playerHealth = Mathf.Max(0, value);
    }


    private readonly float[] _lanePositions = new float[3];
    private int _currentLaneIndex = 1; //0 = Left,1 = Center,2 = Right

    private Vector3 _targetPosition;
    private GameController _gameController;
    private GameManager _gameManager;
    private Animator _animator;
    private PlayableObjectData _playerData;
    private GameObject _playerModel;

    public PlayableObjectData PlayerData => _playerData;

    // jumping state
    private float _verticalVelocity = 0f;
    private bool _isGrounded = true;
    private float _groundY;

    // smoothed velocity values for animator
    private float _smoothedXVelocity = 0f;
    private float _smoothedYVelocity = 0f;

    // wheels rotators and animator backup
    private WheelsRotator[] _wheelRotators;
    private float _storedAnimatorSpeed = 1f;

    private bool _isReversing = false;

    private void Start()
    {
        _gameManager = GameManager.Instance;
        _gameController = GameController.Instance;

        _playerData = _gameManager.PlayableObjectsSO.GetPlayableObjectDataByName(playableObjectName);
        GameObject playerModel = _playerData.prefab;
        _playerHealth = _playerData.health;
        _gameController.WorldManager.SetWorldSpeed(_playerData.speed);
        _playerModel = Instantiate(playerModel, transform);

        SetAnimatorType();
        SetLaneChangeSpeed();

        // initialize lane positions from serialized values
        _lanePositions[0] = _gameController.WorldManager.GetLaneXPosition(LaneNumber.Left);
        _lanePositions[1] = _gameController.WorldManager.GetLaneXPosition(LaneNumber.Center);
        _lanePositions[2] = _gameController.WorldManager.GetLaneXPosition(LaneNumber.Right);

        // snap player to center lane on start
        transform.position = new Vector3(_lanePositions[_currentLaneIndex], transform.position.y, transform.position.z);

        // set initial target position to current lane's x
        _targetPosition = new Vector3(_lanePositions[_currentLaneIndex], transform.position.y, transform.position.z);

        // record ground Y (assume starting on ground)
        _groundY = transform.position.y;
        _isGrounded = true;
        _verticalVelocity = 0f;
        _animator = _playerModel.GetComponent<Animator>();

        // cache wheels rotators if present
        _wheelRotators = _playerModel.GetComponentsInChildren<WheelsRotator>(true);

        SetMaterialEmmissionDarkness(1);
    }

    private void Update()
    {
        if (GameController.Instance.IsGameOver)
            return;
        SmoothMoveToTargetLane();
        ApplyGravityAndJump();
        
        // Only allow input if the world is moving forward (not paused or reversing)
        if (IsWorldMoving())
        {
            HandleInput();
        }
        
        if (_animatorMode == AnimatorMode.PositionBased)
        {
            if (_isReversing)
            {
                SmoothZeroAnimatorParams();
                return;
            }
            Vector2 normalPos = NormalizePosition();
            SetAnimatorXY(normalPos.x, normalPos.y);
        }
        else // VelocityBased
        {
            if (_isReversing)
            { 
                SmoothZeroAnimatorParams();
                return;
            }
            Vector2 velocity = GetVelocityBasedValues();
            SetAnimatorXY(velocity.x, velocity.y);
        }
    }

    private void HandleInput()
    {
        // move left
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            TryChangeLane(-1);
        }

        // move right
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            TryChangeLane(1);
        }

        // jump (Up arrow or W)
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            TryJump();
        }
    }

    private void SetAnimatorXY(float x, float y)
    {
        if (_animator != null)
        {
            _animator.SetFloat("MoveX", x);
            _animator.SetFloat("MoveY", y);
        }
    }

    private Vector2 GetVelocityBasedValues()
    {
        float targetX = 0f;
        float targetY = 0f;

        // Calculate target X velocity based on horizontal movement direction
        float xDifference = _targetPosition.x - transform.position.x;
        if (Mathf.Abs(xDifference) > 0.01f)
        {
            // Moving right
            if (xDifference > 0)
                targetX = 1f;
            // Moving left
            else
                targetX = -1f;
        }
        else
        {
            targetX = 0f;
        }

        // Calculate target Y velocity based on vertical movement
        if (!_isGrounded)
        {
            // Moving up
            if (_verticalVelocity > 0.01f)
                targetY = 1f;
            // Moving down
            else if (_verticalVelocity < -0.01f)
                targetY = -1f;
            else
                targetY = 0f;
        }
        else
        {
            targetY = 0f;
        }

        // Smoothly interpolate to target values
        _smoothedXVelocity = Mathf.MoveTowards(_smoothedXVelocity, targetX, velocitySmoothingSpeed * Time.deltaTime);
        _smoothedYVelocity = Mathf.MoveTowards(_smoothedYVelocity, targetY, velocitySmoothingSpeed * Time.deltaTime);

        return new Vector2(_smoothedXVelocity, _smoothedYVelocity);
    }

    private void SetAnimatorType()
    {
        if(playableObjectName == PlayableObjectName.Skateboard)
        {
            _animatorMode = AnimatorMode.PositionBased;
        }
        else
        {
            _animatorMode = AnimatorMode.VelocityBased;
        }
    }

    private void SetLaneChangeSpeed()
    {
        _laneChangeSpeed = _playerData.laneChangeSpeed;
    }

    private Vector2 NormalizePosition()
    {
        float normalizedx = Mathf.Clamp(transform.position.x / _lanePositions[2], -1f, 1f);
        float maxJumpHeight = (jumpVelocity * jumpVelocity) / (2 * Mathf.Abs(gravity));
        float normalizedy = Mathf.Clamp((transform.position.y - _groundY) / maxJumpHeight, -1f, 1f);

        return new Vector2(normalizedx, normalizedy);
    }

    private bool IsWorldMoving()
    {
        if (_gameController?.WorldManager == null)
            return false;
        
        // World is moving if speed is positive (forward movement)
        return _gameController.WorldManager.GetCurrentSpeed() > 0f;
    }

    private void TryChangeLane(int direction)
    {
        int desired = Mathf.Clamp(_currentLaneIndex + direction, 0, 2);
        if (desired == _currentLaneIndex) return;

        _currentLaneIndex = desired;
        _targetPosition = new Vector3(_lanePositions[_currentLaneIndex], transform.position.y, transform.position.z);
    }

    private void SetLane(LaneNumber lane)
    {
        switch (lane)
        {
            case LaneNumber.Left:
                _currentLaneIndex = 0;
                break;
            case LaneNumber.Center:
                _currentLaneIndex = 1;
                break;
            case LaneNumber.Right:
                _currentLaneIndex = 2;
                break;
        }
        _targetPosition = new Vector3(_lanePositions[_currentLaneIndex], transform.position.y, transform.position.z);
    }

    private void SmoothMoveToTargetLane()
    {
        Vector3 newPos = transform.position;
        newPos.x = Mathf.MoveTowards(transform.position.x, _targetPosition.x, _laneChangeSpeed * Time.deltaTime);
        newPos.y = transform.position.y;
        newPos.z = transform.position.z;
        transform.position = newPos;
    }

    private void TryJump()
    {
        // Prevent jumping while moving left or right
        if (Mathf.Abs(transform.position.x - _targetPosition.x) > 0.01f && _animatorMode == AnimatorMode.PositionBased)
            return;
        
        if (_isGrounded)
        {
            _verticalVelocity = jumpVelocity;
            _isGrounded = false;
            StopParticleSystems(); // Stop particles when jumping
        }
    }

    private void ApplyGravityAndJump()
    {
        // apply gravity - stronger when falling (after reaching peak height)
        float currentGravity = gravity;
        if (_verticalVelocity < 0f) // falling
        {
            currentGravity *= fallGravityMultiplier;
        }
        _verticalVelocity += currentGravity * Time.deltaTime;

        float newY = transform.position.y + _verticalVelocity * Time.deltaTime;

        if (newY <= _groundY)
        {
            // landing detected
            if (!_isGrounded)
            {
                // Spawn landing particle effect at ground position using centralized system
                Vector3 landingPosition = new Vector3(transform.position.x, _groundY, transform.position.z);
                ParticleEffectSpawner.SpawnParticleEffect(ParticleEffectType.PlayerLanding, landingPosition);

                // call camera landing bounce
                var cam = PlayerCameraController.Instance;
                if (cam != null)
                {
                    cam.TriggerLandingBounce();
                }
                
                ResumeParticleSystems(); // Resume particles when landing
            }
            
            newY = _groundY;
            _verticalVelocity = 0f;
            _isGrounded = true;
        }

        Vector3 p = transform.position;
        p.y = newY;
        transform.position = p;
    }

    public LaneNumber GetCurrentLane()
    {
        return _currentLaneIndex == 0 ? LaneNumber.Left : (_currentLaneIndex == 1 ? LaneNumber.Center : LaneNumber.Right);
    }

    public int GetDifferenceFromCenterLane()
    {
        return -1 * (_currentLaneIndex - 1);
    }

    public void OnDeath()
    {
        Debug.Log(GetDifferenceFromCenterLane());
        SetLane(LaneNumber.Center);
        TryChangeLane(GetDifferenceFromCenterLane());
        StartCoroutine(ResetCollider());
        StartCoroutine(OnDeathFlash());
    }

    private IEnumerator ResetCollider()
    {
        SetColliderEnabled(false);
        yield return new WaitForSeconds(_gameController.ReverseTime + _gameController.StartTime + _gameController.PlayerColliderDisabledTime);
        SetColliderEnabled(true);
    }

    private IEnumerator OnDeathFlash(float beginningValue = 1, float endingValue = .3f, float duration = .5f, int numOfFlashing = 3)
    {
        for (int i = 0; i < numOfFlashing; i++)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float value = Mathf.Lerp(beginningValue, endingValue, t);
                SetMaterialEmmissionDarkness(value);
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float value = Mathf.Lerp(endingValue, beginningValue, t);
                SetMaterialEmmissionDarkness(value);
                yield return null;
            }
        }
    }

    private void SetMaterialEmmissionDarkness(float value)
    {
        foreach (var material in playerMaterials)
        {
            material.SetFloat("_EmmissionDarkLevel", value);
        }
    }

    public void SetColliderEnabled(bool enabled)
    {
        _playerModel.GetComponent<Collider>().enabled = enabled;
    }

    public void ReverseAnimationAndWheels()
    {
        if (_animator != null)
        {
            _storedAnimatorSpeed = _animator.speed;
            _animator.speed = -Mathf.Abs(_storedAnimatorSpeed != 0f ? _storedAnimatorSpeed : 1f);
        }

        if (_wheelRotators != null && _wheelRotators.Length > 0)
        {
            foreach (var w in _wheelRotators)
            {
                if (w != null) w.Reverse();
            }
        }
        _isReversing = true;
    }

    public void StopAnimationAndWheels()
    {
        if (_wheelRotators != null && _wheelRotators.Length > 0)
        {
            foreach (var w in _wheelRotators)
            {
                if (w != null) w.Stop();
            }
        }
    }

    public void StopParticleSystems()
    {
        // Stop all particle systems on the player GameObject
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                var emission = ps.emission;
                emission.enabled = false;
                ps.Stop();
            }
        }
    }

    public void ResumeParticleSystems()
    {
            
        // Resume all particle systems on the player GameObject
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                var emission = ps.emission;
                emission.enabled = true;
                ps.Play();
            }
        }
    }

    public void ResumeAnimationAndWheels()
    {
        if (_animator != null)
        {
            float targetSpeed = Mathf.Abs(_storedAnimatorSpeed != 0f ? _storedAnimatorSpeed : 1f);
            _animator.speed = targetSpeed;
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            _animator.Play(state.shortNameHash, 0, 0f);
        }

        if (_wheelRotators != null && _wheelRotators.Length > 0)
        {
            foreach (var w in _wheelRotators)
            {
                if (w != null) w.Resume();
            }
        }
        _isReversing = false;
    }

    // Smoothly move animator parameters MoveX and MoveY toward zero
    private void SmoothZeroAnimatorParams()
    {
        if (_animator == null) return;

        _smoothedXVelocity = 0f;
        _smoothedYVelocity = 0f;

        float currX = _animator.GetFloat("MoveX");
        float currY = _animator.GetFloat("MoveY");

        float newX = Mathf.MoveTowards(currX, 0f, animatorZeroingSpeed * Time.deltaTime);
        float newY = Mathf.MoveTowards(currY, 0f, animatorZeroingSpeed * Time.deltaTime);

        SetAnimatorXY(newX, newY);
    }
}
