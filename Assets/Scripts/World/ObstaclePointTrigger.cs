using UnityEngine;
using Utilities;

namespace World
{
    [RequireComponent(typeof(Collider))]
    public class ObstaclePointTrigger : MonoBehaviour
    {
        [Header("Point Settings")]
        [SerializeField] private int normalDynamicPoints = 5;
        [SerializeField] private int oppositeDynamicPoints = 15;
        
        private WorldObstacle _parentObstacle;
        private Collider _triggerCollider;
        private bool _hasPlayerInside = false;
        private bool _pointsAwarded = false;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider>();
            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
            
            _parentObstacle = GetComponentInParent<WorldObstacle>();
            if (_parentObstacle == null)
            {
                Debug.LogWarning($"ObstaclePointTrigger on {gameObject.name} could not find parent WorldObstacle component.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_pointsAwarded && other.CompareTag("Player"))
            {
                _hasPlayerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_pointsAwarded && other.CompareTag("Player") && _hasPlayerInside)
            {
                AwardPoints(other);
            }
        }

        private void AwardPoints(Collider playerCollider)
        {
            if (_pointsAwarded || _parentObstacle == null) return;
            
            _pointsAwarded = true;
            _hasPlayerInside = false;

            int pointsToAward = _parentObstacle.IsOppositeDirection 
                ? oppositeDynamicPoints 
                : normalDynamicPoints;

            // Spawn floating point text at the exit point
            Vector3 spawnPosition = playerCollider.transform.position;
            if (_triggerCollider != null)
            {
                spawnPosition = _triggerCollider.ClosestPoint(playerCollider.transform.position);
            }
            FloatingPointTextSpawner.SpawnFloatingPointText(pointsToAward, spawnPosition);

            if (GameController.Instance != null)
            {
                GameController.Instance.SetPoints(
                    GameController.Instance.GamePoints + pointsToAward
                );
            }
        }

        public void ResetTrigger()
        {
            _hasPlayerInside = false;
            _pointsAwarded = false;
        }
    }
}
