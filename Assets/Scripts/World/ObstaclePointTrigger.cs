using UnityEngine;
using Utilities;
using System.Collections;

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
                if(_parentObstacle.IsOppositeDirection)
                {
                    StartCoroutine(SlowMotionEffect());
                }
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
        private IEnumerator SlowMotionEffect()
        {
            Time.timeScale = 0.5f;

            // Lower music pitch during slow motion (only in-game, not in menu)
            if (GameController.Instance != null && AudioManager.Instance != null)
                AudioManager.Instance.SetMusicPitch(0.7f);

            // WaitForSecondsRealtime is unaffected by Time.timeScale, so the effect
            // lasts exactly 0.5 real seconds.  Using WaitForSeconds(0.5f) here would
            // actually wait 1.0 real second because 0.5 scaled-seconds = 1.0 real second
            // at 0.5× timeScale.
            yield return new WaitForSecondsRealtime(0.5f);
            Time.timeScale = 1f;

            // Restore music pitch after slow motion ends
            if (GameController.Instance != null && AudioManager.Instance != null)
                AudioManager.Instance.SetMusicPitch(1f);
        }
        public void ResetTrigger()
        {
            _hasPlayerInside = false;
            _pointsAwarded = false;
        }
    }
}
