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
        
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float movement = _currentSpeed * deltaTime;
            
            _totalDistanceTraveled += movement;
            
            if (useAcceleration)
            {
                _currentSpeed = Mathf.Min(_currentSpeed + accelerationRate * deltaTime, maxSpeed);
            }
        }
        
        public void SetSpeed(float speed)
        {
            baseSpeed = Mathf.Clamp(speed, 0f, maxSpeed);
            _currentSpeed = baseSpeed;
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
            _currentSpeed = -1 * baseSpeed * 2;
        }
    }
}

