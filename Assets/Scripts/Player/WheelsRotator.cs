using UnityEngine;

public class WheelsRotator : MonoBehaviour
{
    [SerializeField] private Transform[] wheels;
    [SerializeField] private float rotationSpeed = 360f;

    private float _originalRotationSpeed;
    private bool _isReversed = false;

    void Awake()
    {
        _originalRotationSpeed = rotationSpeed;
    }

    void Update()
    {
        float rot = rotationSpeed * Time.deltaTime;
        foreach (var wheel in wheels)
            wheel.Rotate(Vector3.right, rot, Space.Self);
    }

    public void Reverse()
    {
        _isReversed = true;
        rotationSpeed = -Mathf.Abs(_originalRotationSpeed) * GameController.Instance.ReverseMultiplier;
    }

    public void Stop()
    {
        rotationSpeed = 0;
    }

    public void Resume()
    {
        _isReversed = false;
        rotationSpeed = Mathf.Abs(_originalRotationSpeed);
    }

    public bool IsReversed => _isReversed;
}