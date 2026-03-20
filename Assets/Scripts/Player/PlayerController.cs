using Player;
using Powerup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World;
using Utilities;

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
    [Tooltip("Gravity multiplier applied when the player fast-falls (Down input while airborne). Should be larger than fallGravityMultiplier.")]
    [SerializeField] private float fastFallGravityMultiplier = 10f;

    [Tooltip("Speed at which velocity values smoothly transition (for velocity-based mode)")]
    [SerializeField] private float velocitySmoothingSpeed = 10f;

    [SerializeField] private List<Material> playerMaterials;

    // New: speed used to smoothly drive animator parameters to zero when reversing
    [SerializeField] private float animatorZeroingSpeed = 4f;
    [SerializeField] private GameObject resetSign;

    [Header("Touch/Swipe Controls")]
    [Tooltip("Minimum distance in pixels to register a swipe")]
    [SerializeField] private float swipeThreshold = 50f;
    [Tooltip("Maximum time in seconds for a swipe gesture")]
    [SerializeField] private float swipeMaxTime = 0.5f;
    [Tooltip("Optional: Prefab for touch feedback effect (simple sprite that fades out)")]
    [SerializeField] private GameObject touchFeedbackPrefab;

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
    private bool _isFastFalling = false;

    // smoothed velocity values for animator
    private float _smoothedXVelocity = 0f;
    private float _smoothedYVelocity = 0f;

    // wheels rotators and animator backup
    private WheelsRotator[] _wheelRotators;
    private float _storedAnimatorSpeed = 1f;

    private bool _isReversing = false;

    // Touch/Swipe detection
    private Vector2 _touchStartPos;
    private float _touchStartTime;
    private bool _isTouching = false;
    private Camera _mainCamera;
    private TouchFeedbackEffect _currentTouchFeedback;

    private void Start()
    {
        _gameManager = GameManager.Instance;
        _gameController = GameController.Instance;

        string savedVehicleId = _gameManager.GetSelectedVehicleId();
        if (!string.IsNullOrEmpty(savedVehicleId))
            _playerData = _gameManager.PlayableObjectsSO.GetPlayableObjectDataById(savedVehicleId);
        if (_playerData == null)
            _playerData = _gameManager.PlayableObjectsSO.GetPlayableObjectDataByName(playableObjectName);

        if (_playerData == null)
        {
            Debug.LogError("PlayerController: No PlayableObjectData found for selected vehicle or default.");
            return;
        }

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

        // Get main camera for touch feedback positioning
        _mainCamera = Camera.main;
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
            HandleTouchInput();
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
            if (TryChangeLane(-1))
            {
                TriggerLaneTilt(-1);
            }
        }

        // move right
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (TryChangeLane(1))
            {
                TriggerLaneTilt(1);
            }
        }

        // jump (Up arrow or W)
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            TryJump();
        }

        // fast fall (Down arrow or S while airborne)
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            TryFastFall();
        }
    }

    private void HandleTouchInput()
    {
        // Handle touch input (mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                _touchStartPos = touch.position;
                _touchStartTime = Time.time;
                _isTouching = true;
                ShowTouchFeedback(_touchStartPos);
            }
            else if (touch.phase == TouchPhase.Moved && _isTouching)
            {
                // Update feedback during swipe
                UpdateTouchFeedback(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended && _isTouching)
            {
                ProcessSwipe(_touchStartPos, touch.position, Time.time - _touchStartTime);
                EndTouchFeedback();
                _isTouching = false;
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                EndTouchFeedback();
                _isTouching = false;
            }
        }
        // Handle mouse input (for editor testing)
        else if (Input.GetMouseButtonDown(0))
        {
            _touchStartPos = Input.mousePosition;
            _touchStartTime = Time.time;
            _isTouching = true;
            ShowTouchFeedback(_touchStartPos);
        }
        else if (Input.GetMouseButton(0) && _isTouching)
        {
            // Update feedback during swipe
            UpdateTouchFeedback(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && _isTouching)
        {
            ProcessSwipe(_touchStartPos, Input.mousePosition, Time.time - _touchStartTime);
            EndTouchFeedback();
            _isTouching = false;
        }
    }

    private void ProcessSwipe(Vector2 startPos, Vector2 endPos, float duration)
    {
        // Only process if swipe was quick enough
        if (duration > swipeMaxTime)
            return;

        Vector2 delta = endPos - startPos;
        float distance = delta.magnitude;

        // Only process if swipe distance is sufficient
        if (distance < swipeThreshold)
            return;

        // Determine swipe direction
        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        bool movementTriggered = false;

        // Horizontal swipe (left or right)
        if (absX > absY)
        {
            if (delta.x > 0)
            {
                // Swipe right
                if (TryChangeLane(1))
                {
                    TriggerLaneTilt(1);
                    movementTriggered = true;
                }
            }
            else
            {
                // Swipe left
                if (TryChangeLane(-1))
                {
                    TriggerLaneTilt(-1);
                    movementTriggered = true;
                }
            }
        }
        // Vertical swipe
        else if (absY > absX)
        {
            if (delta.y > 0)
            {
                // Swipe up → jump
                movementTriggered = TryJump();
            }
            else
            {
                // Swipe down → fast fall
                movementTriggered = TryFastFall();
            }
        }

        // Only change color if movement was actually triggered
        if (movementTriggered && _currentTouchFeedback != null)
        {
            _currentTouchFeedback.OnMovementTriggered();
        }
    }

    private void ShowTouchFeedback(Vector2 screenPosition)
    {
        if (touchFeedbackPrefab == null || _mainCamera == null)
            return;

        // Clean up any existing feedback
        if (_currentTouchFeedback != null)
        {
            Destroy(_currentTouchFeedback.gameObject);
        }

        // Convert screen position to world position at a fixed distance from camera
        // Using a distance that ensures visibility (2-3 units from camera)
        float distanceFromCamera = 3f;
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));
        
        // Make sure the feedback faces the camera
        Vector3 directionToCamera = (_mainCamera.transform.position - worldPos).normalized;
        Quaternion rotation = Quaternion.LookRotation(-directionToCamera);
        
        // Instantiate feedback effect
        GameObject feedback = Instantiate(touchFeedbackPrefab, worldPos, rotation);
        _currentTouchFeedback = feedback.GetComponent<TouchFeedbackEffect>();
        
        // Initialize with swipe threshold
        if (_currentTouchFeedback != null)
        {
            _currentTouchFeedback.Initialize(swipeThreshold);
        }
        // Auto-destroy after animation (the TouchFeedbackEffect script handles this, but add a safety timeout)
        // If the prefab doesn't have the script, destroy after a short time
        else if (feedback.GetComponent<ParticleSystem>() == null)
        {
            Destroy(feedback, 0.5f);
        }
    }

    private void UpdateTouchFeedback(Vector2 currentScreenPosition)
    {
        if (_currentTouchFeedback == null || !_isTouching || _mainCamera == null)
            return;

        // Calculate current swipe distance
        Vector2 delta = currentScreenPosition - _touchStartPos;
        float distance = delta.magnitude;

        // Update feedback position to follow touch
        float distanceFromCamera = 3f;
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(currentScreenPosition.x, currentScreenPosition.y, distanceFromCamera));
        _currentTouchFeedback.transform.position = worldPos;

        // Update feedback with current swipe progress
        _currentTouchFeedback.UpdateSwipeProgress(distance);
    }

    private void EndTouchFeedback()
    {
        if (_currentTouchFeedback != null)
        {
            _currentTouchFeedback.StartFadeOut();
            _currentTouchFeedback = null;
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
        if (_playerData.name == PlayableObjectName.Skateboard)
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

    private void TriggerLaneTilt(int direction)
    {
        // Trigger camera tilt when changing lanes (direction: -1 for left, 1 for right)
        if (PlayerCameraController.Instance != null)
        {
            PlayerCameraController.Instance.TriggerLaneTilt(direction);
        }
    }

    private bool TryChangeLane(int direction)
    {
        int desired = Mathf.Clamp(_currentLaneIndex + direction, 0, 2);
        if (desired == _currentLaneIndex) return false;

        _currentLaneIndex = desired;
        _targetPosition = new Vector3(_lanePositions[_currentLaneIndex], transform.position.y, transform.position.z);

        // Play lane change sound
        if (AudioManager.Instance != null)
        {
            if (direction > 0)
                AudioManager.Instance.PlaySFX(AudioEventSFX.LaneChangeRight);
            else
                AudioManager.Instance.PlaySFX(AudioEventSFX.LaneChangeLeft);
        }

        return true;
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
        float speedMult = PowerupManager.Instance != null ? PowerupManager.Instance.LaneChangeSpeedMultiplier : 1f;
        Vector3 newPos = transform.position;
        newPos.x = Mathf.MoveTowards(transform.position.x, _targetPosition.x, _laneChangeSpeed * speedMult * Time.deltaTime);
        newPos.y = transform.position.y;
        newPos.z = transform.position.z;
        transform.position = newPos;
    }

    private bool TryJump()
    {
        // Prevent jumping while moving left or right
        if (Mathf.Abs(transform.position.x - _targetPosition.x) > 0.01f && _animatorMode == AnimatorMode.PositionBased)
            return false;

        if (_isGrounded)
        {
            _verticalVelocity = jumpVelocity;
            _isGrounded = false;
            StopParticleSystems(); // Stop particles when jumping

            // Play jump sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioEventSFX.Jump);
            }

            return true;
        }
        return false;
    }

    private bool TryFastFall()
    {
        if (!_isGrounded && !_isFastFalling)
        {
            _isFastFalling = true;
            // Cancel any remaining upward velocity so the drop feels immediate
            if (_verticalVelocity > 0f)
                _verticalVelocity = 0f;
            return true;
        }
        return false;
    }

    private void ApplyGravityAndJump()
    {
        // apply gravity
        float currentGravity = gravity;
        if (_isFastFalling)
            currentGravity *= fastFallGravityMultiplier;
        else if (_verticalVelocity < 0f) // normal fall after peak
            currentGravity *= fallGravityMultiplier;
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

                // Play land sound
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioEventSFX.Land);
                }

                ResumeParticleSystems(); // Resume particles when landing
            }
            
            newY = _groundY;
            _verticalVelocity = 0f;
            _isGrounded = true;
            _isFastFalling = false;
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
        SetLane(LaneNumber.Center);
        StartCoroutine(ResetCollider());
        StartCoroutine(OnDeathFlash());
    }

    private IEnumerator ResetCollider()
    {
        SetColliderEnabled(false);
        yield return new WaitForSeconds(_gameController.ReverseTime + _gameController.StartTime);
        SetColliderEnabled(true);
        if (PowerupManager.Instance != null)
            PowerupManager.Instance.ActivateCheckpointInvincibility(_gameController.PlayerColliderDisabledTime);
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
