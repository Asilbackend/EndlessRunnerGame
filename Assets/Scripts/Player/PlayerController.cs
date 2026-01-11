using Player;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using World;
using static UnityEngine.Rendering.DebugUI;

// Simple player controller that can move only on three lanes (Left, Center, Right)
public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayableObjectName playableObjectName = PlayableObjectName.Skateboard;

    [Header("Jump")]
    [Tooltip("Initial upward velocity applied when jumping. Tune this value to adjust jump height.")]
    [SerializeField] private float jumpVelocity = 6f;
    [Tooltip("Gravity applied to the player (negative value). Tune to change fall speed.")]
    [SerializeField] private float gravity = -20f;

    [Tooltip("Speed at which velocity values smoothly transition (for velocity-based mode)")]
    [SerializeField] private float velocitySmoothingSpeed = 10f;

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

        // set initial target position to current lane's x
        _targetPosition = new Vector3(_lanePositions[_currentLaneIndex], transform.position.y, transform.position.z);

        // snap player to center lane on start
        transform.position = new Vector3(_lanePositions[_currentLaneIndex], transform.position.y, transform.position.z);

        // record ground Y (assume starting on ground)
        _groundY = transform.position.y;
        _isGrounded = true;
        _verticalVelocity = 0f;
        _animator = _playerModel.GetComponent<Animator>();
    }

    private void Update()
    {
        if (GameController.Instance.IsGameOver)
            return;
        HandleInput();
        SmoothMoveToTargetLane();
        ApplyGravityAndJump();
        
        if (_animatorMode == AnimatorMode.PositionBased)
        {
            Vector2 normalPos = NormalizePosition();
            SetAnimatorXY(normalPos.x, normalPos.y);
        }
        else // VelocityBased
        {
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

    private void TryChangeLane(int direction)
    {
        int desired = Mathf.Clamp(_currentLaneIndex + direction, 0, 2);
        if (desired == _currentLaneIndex) return;

        _currentLaneIndex = desired;
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
        }
    }

    private void ApplyGravityAndJump()
    {
        // apply gravity
        _verticalVelocity += gravity * Time.deltaTime;

        float newY = transform.position.y + _verticalVelocity * Time.deltaTime;

        if (newY <= _groundY)
        {
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
        TryChangeLane(GetDifferenceFromCenterLane());
        StartCoroutine(ResetCollider());
    }

    private IEnumerator ResetCollider()
    {
        SetColliderEnabled(false);
        yield return new WaitForSeconds(_gameController.ReverseTime + _gameController.StartTime + _gameController.PlayerColliderDisabledTime);
        SetColliderEnabled(true);
    }

    public void SetColliderEnabled(bool enabled)
    {
        _playerModel.GetComponent<Collider>().enabled = enabled;
    }
}
