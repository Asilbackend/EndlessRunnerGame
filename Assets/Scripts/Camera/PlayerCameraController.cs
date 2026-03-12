using System.Collections;
using UnityEngine;
using World;

// Simple camera controller that follows the player and provides juiciness:
// - moves horizontally when player changes lanes
// - raises/tilts when player jumps
// - landing bounce and shake on impacts
public class PlayerCameraController : MonoBehaviour
{
    public static PlayerCameraController Instance { get; private set; }
    // external override mode (used when PlayerController drives camera)
    private bool _useExternal = false;
    private Vector3 _externalDesiredPos;
    private Quaternion _externalDesiredRot;
    /// <summary>Set desired position/rotation from external controller. If instant is true, snap immediately.</summary>
    public void SetExternalDesired(Vector3 pos, Quaternion rot, bool instant = false)
    {
        _externalDesiredPos = pos;
        _externalDesiredRot = rot;
        _useExternal = true;
        if (instant)
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }

    /// <summary>Stop using external desired values � camera will use its internal calculations again.</summary>
    public void ClearExternalDesired()
    {
        _useExternal = false;
    }

    [Header("References")]
    public Transform target; // player transform

    [Header("Base Offset")]
    public Vector3 baseOffset = new Vector3(0f, 2f, -6f);
    public float baseRotationX = 0f; // base pitch rotation

    [Header("Lane Offsets")]
    public float laneXOffset = 1f; // amount to offset X when on left/right lane (user requested -1 for left)
    public float laneLerpSpeed = 8f;

    [Header("Lane Tilt")]
    public float laneTiltAngle = 2f; // maximum Z rotation angle when tilting left/right
    public float laneTiltDuration = 0.1f; // time to reach peak tilt and return to 0

    [Header("Jump")]
    public float jumpYOffset = 1f; // extra Y while jumping
    public float jumpZOffset = 1f; // extra X while jumping
    public float jumpRotationX = 15f; // rotation around X during jump
    public float jumpLerpSpeed = 8f; 

    [Header("Landing")]
    public float landingBounceMagnitude = 0.5f;
    public float landingBounceDuration = 0.35f;
    public float landingRotationResetSpeed = 20f; // speed to reset rotation when landing
    public float landingForwardOffset = 0.2f; // slight forward movement on landing for realism

    [Header("Impact / Powerup")]
    public float impactShakeDuration = 0.4f;
    public float impactShakeMagnitude = 0.45f;
    public float powerupBounceDuration = 0.25f;
    public float powerupBounceMagnitude = 0.25f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.08f;
    public float rotationLerpSpeed = 10f;

    // internal state
    private Vector3 _currentVelocity;
    private PlayerController _playerController;
    private float _groundY;
    private bool _wasAirborne = false;

    // smooth interpolation states
    private float _currentLaneOffsetX = 0f;
    private float _currentJumpYOffset = 0f;
    private float _currentJumpZOffset = 0f;
    private float _currentJumpRotationX = 0f;
    private float _currentLaneTiltZ = 0f; // current Z rotation for lane tilt
    private bool _isLanding = false; // flag to quickly reset rotation on landing
    private Coroutine _laneTiltCoroutine = null;

    // shake state
    private Vector3 _shakeOffset = Vector3.zero;
    private Vector3 _landingBounceOffset = Vector3.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (target == null && GameController.Instance != null)
        {
            var pc = GameController.Instance.PlayerController;
            if (pc != null) target = pc.transform;
        }

        if (target != null)
        {
            _playerController = target.GetComponent<PlayerController>();
            _groundY = target.position.y;
        }

        UpdateSmoothStates(instant: true);
        transform.position = GetDesiredPosition();
        transform.rotation = GetDesiredRotation(transform.position);
    }

    private void Update()
    {
        if (target == null) return;

        bool isAirborne = target.position.y > _groundY + 0.01f;
        if (!_wasAirborne && isAirborne)
        {
            StopAllCoroutines();
            _isLanding = false;
        }
        else if (_wasAirborne && !isAirborne)
        {
            // landed - immediately start resetting rotation and trigger bounce
            _isLanding = true;
            LandingBounce();
        }
        _wasAirborne = isAirborne;

        Vector3 desiredPos;
        Quaternion desiredRot;

        if (_useExternal)
        {
            desiredPos = _externalDesiredPos;
            desiredRot = _externalDesiredRot;
        }
        else
        {
            // normal follow + lane + jump offsets (internal calculation)
            // Update smooth interpolation states first
            UpdateSmoothStates();
            // Then calculate desired position and rotation
            desiredPos = GetDesiredPosition();
            desiredRot = GetDesiredRotation(desiredPos);
        }

        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desiredPos + _shakeOffset + _landingBounceOffset, ref _currentVelocity, positionSmoothTime);
        transform.position = smoothed;

        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, rotationLerpSpeed * Time.deltaTime);
    }

    private void UpdateSmoothStates(bool instant = false)
    {
        // Calculate target lane offset
        float targetLaneOffsetX = 0f;
        if (_playerController != null)
        {
            var lane = _playerController.GetCurrentLane();
            if (lane == LaneNumber.Left) targetLaneOffsetX = -laneXOffset;
            else if (lane == LaneNumber.Right) targetLaneOffsetX = laneXOffset;
            else targetLaneOffsetX = 0f;
        }

        // Smooth lane offset transition
        if (instant)
        {
            _currentLaneOffsetX = targetLaneOffsetX;
        }
        else
        {
            _currentLaneOffsetX = Mathf.Lerp(_currentLaneOffsetX, targetLaneOffsetX, laneLerpSpeed * Time.deltaTime);
        }

        // Calculate target jump Y offset
        float targetJumpYOffset = 0f;
        float targetJumpZOffset = 0f;
        bool isAirborne = target.position.y > _groundY + 0.01f;
        if (isAirborne)
        {
            targetJumpYOffset = jumpYOffset;
            targetJumpZOffset = jumpZOffset;
        }

        // Smooth jump Y offset transition
        if (instant)
        {
            _currentJumpYOffset = targetJumpYOffset;
            _currentJumpZOffset = targetJumpZOffset;
        }
        else
        {
            _currentJumpYOffset = Mathf.Lerp(_currentJumpYOffset, targetJumpYOffset, jumpLerpSpeed * Time.deltaTime);
            _currentJumpZOffset = Mathf.Lerp(_currentJumpZOffset, targetJumpZOffset, jumpLerpSpeed * Time.deltaTime);
        }

        // Calculate target jump rotation
        float targetJumpRotationX = 0f;
        if (isAirborne)
        {
            targetJumpRotationX = jumpRotationX;
        }

        // Smooth jump rotation transition
        if (instant)
        {
            _currentJumpRotationX = targetJumpRotationX;
        }
        else
        {
            // If landing, reset rotation quickly to avoid mixing with bounce
            float lerpSpeed = _isLanding ? landingRotationResetSpeed : jumpLerpSpeed;
            _currentJumpRotationX = Mathf.Lerp(_currentJumpRotationX, targetJumpRotationX, lerpSpeed * Time.deltaTime);
            
            // Clear landing flag once rotation is reset
            if (_isLanding && Mathf.Abs(_currentJumpRotationX) < 0.1f)
            {
                _currentJumpRotationX = 0f;
                _isLanding = false;
            }
        }
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 laneOffset = new Vector3(_currentLaneOffsetX, 0f, 0f);
        Vector3 jumpOffset = new Vector3(0f, _currentJumpYOffset, _currentJumpZOffset);
        Vector3 desired = target.position + baseOffset + laneOffset + jumpOffset;
        return desired;
    }

    private Quaternion GetDesiredRotation(Vector3 desiredPos)
    {
        // Calculate desired pitch (base + jump rotation)
        float desiredPitch = baseRotationX + _currentJumpRotationX;

        // Calculate yaw to look at player (horizontal only)
        Vector3 forward = (target.position - desiredPos);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;
        // Multiply forward to look further ahead, reducing rotation sensitivity
        Quaternion lookYaw = Quaternion.LookRotation(forward * 100f, Vector3.up);

        // Combine pitch, yaw, and lane tilt (Z rotation)
        Quaternion desiredRot = Quaternion.Euler(desiredPitch, lookYaw.eulerAngles.y, _currentLaneTiltZ);
        return desiredRot;
    }

    private void LandingBounce()
    {
        // Cancel any existing bounce tweens
        LeanTween.cancel(gameObject);
        
        // Reset offset to start fresh
        _landingBounceOffset = Vector3.zero;
        
        // Use a reduced bounce magnitude for smoother, lower bounce (50% of original)
        float smoothBounceMagnitude = landingBounceMagnitude * 0.5f;
        
        // Animate Y bounce: start at 0, bounce up, then settle smoothly
        // Using easeOutBounce creates a natural bounce that overshoots then settles
        LeanTween.value(gameObject, 0f, smoothBounceMagnitude, landingBounceDuration)
            .setEase(LeanTweenType.easeOutBounce)
            .setOnUpdate((float bounceY) => {
                _landingBounceOffset.y = bounceY;
            })
            .setOnComplete(() => {
                // Smoothly settle to zero after bounce
                LeanTween.value(gameObject, smoothBounceMagnitude, 0f, landingBounceDuration * 0.4f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setOnUpdate((float bounceY) => {
                        _landingBounceOffset.y = bounceY;
                    })
                    .setOnComplete(() => {
                        _landingBounceOffset.y = 0f;
                    });
            });
        
        // Subtle forward movement - smoother and more gentle
        float forwardDuration = landingBounceDuration * 0.5f;
        float forwardAmount = landingForwardOffset * 0.5f; // Reduced forward movement
        LeanTween.value(gameObject, 0f, forwardAmount, forwardDuration)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((float forwardZ) => {
                _landingBounceOffset.z = forwardZ;
            })
            .setOnComplete(() => {
                // Smoothly return forward movement to zero
                LeanTween.value(gameObject, forwardAmount, 0f, forwardDuration * 1.2f)
                    .setEase(LeanTweenType.easeInOutQuad)
                    .setOnUpdate((float forwardZ) => {
                        _landingBounceOffset.z = forwardZ;
                    })
                    .setOnComplete(() => {
                        _landingBounceOffset.z = 0f;
                    });
            });
    }

    public void TriggerLandingBounce()
    {
        LandingBounce();
    }

    public void Impact()
    {
        StartCoroutine(ImpactShake());
    }

    private IEnumerator ImpactShake()
    {
        float elapsed = 0f;
        while (elapsed < impactShakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / impactShakeDuration;
            float damper = 1f - Mathf.Clamp01(t);
            Vector3 shake = Random.insideUnitSphere * impactShakeMagnitude * damper;
            // stronger Y shake
            shake.y = Mathf.Abs(shake.y) * 0.5f;
            _shakeOffset = shake;
            yield return null;
        }
        _shakeOffset = Vector3.zero;
    }

    // public call for powerup bounce
    public void PowerupBounce()
    {
        StartCoroutine(PowerupCoroutine());
    }

    private IEnumerator PowerupCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < powerupBounceDuration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / powerupBounceDuration;
            // simple ease out bounce
            float bounce = Mathf.Sin(p * Mathf.PI) * powerupBounceMagnitude * (1f - p);
            _shakeOffset = new Vector3(0f, bounce, 0f);
            yield return null;
        }
        _shakeOffset = Vector3.zero;
    }

    // Trigger camera tilt when changing lanes
    public void TriggerLaneTilt(int direction)
    {
        // direction: -1 for left, 1 for right
        if (_laneTiltCoroutine != null)
        {
            StopCoroutine(_laneTiltCoroutine);
        }
        _laneTiltCoroutine = StartCoroutine(LaneTiltCoroutine(direction));
    }

    private IEnumerator LaneTiltCoroutine(int direction)
    {
        // Calculate target tilt angle (negative for left, positive for right)
        float targetTilt = direction * laneTiltAngle;

        // Phase 1: Tilt to peak
        float elapsed = 0f;
        float startTilt = _currentLaneTiltZ;
        while (elapsed < laneTiltDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / laneTiltDuration;
            // Use easeOutQuad for smooth deceleration
            t = 1f - (1f - t) * (1f - t);
            _currentLaneTiltZ = Mathf.Lerp(startTilt, targetTilt, t);
            yield return null;
        }
        _currentLaneTiltZ = targetTilt;

        // Phase 2: Return to 0
        elapsed = 0f;
        startTilt = _currentLaneTiltZ;
        while (elapsed < laneTiltDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / laneTiltDuration;
            // Use easeOutQuad for smooth return
            t = 1f - (1f - t) * (1f - t);
            _currentLaneTiltZ = Mathf.Lerp(startTilt, 0f, t);
            yield return null;
        }
        _currentLaneTiltZ = 0f;
        _laneTiltCoroutine = null;
    }
}