using UnityEngine;
using Managers;

namespace World
{
    public class OppositeDynamicObstacleWarningSign : MonoBehaviour
    {
        private float _chunkStartZ = 0f;
        private float _signAppearAtMeters = 0f; // m
        private float _signDisappearAtMeters = 0f; // n
        private bool _isVisible = false;
        private GameObject _trackedObstacle = null;
        private const float _obstaclePassedThreshold = -10f; // Disable sign when obstacle is 10m behind player

        public void Initialize(float chunkStartZ, float chunkSize, float signAppearAtMeters, float signDisappearAtMeters)
        {
            _chunkStartZ = chunkStartZ;
            _signAppearAtMeters = signAppearAtMeters;
            _signDisappearAtMeters = signDisappearAtMeters;
            _isVisible = false;
            
            // Keep GameObject active so Update() can run, but hide the visual components initially
            gameObject.SetActive(true);
            SetVisualsVisible(false);
            
            // Check initial visibility state in case player is already past the appear threshold
            CheckVisibility();
        }
        
        private void SetVisualsVisible(bool visible)
        {
            if (this == null || gameObject == null) return;
            
            // Enable/disable all renderers in this GameObject and children
            try
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                if (renderers != null)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                        {
                            renderer.enabled = visible;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"WarningSign: Error setting renderer visibility: {e.Message}");
            }
            
            // Also enable/disable Canvas components if present
            try
            {
                Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
                if (canvases != null)
                {
                    foreach (Canvas canvas in canvases)
                    {
                        if (canvas != null)
                        {
                            canvas.enabled = visible;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"WarningSign: Error setting canvas visibility: {e.Message}");
            }
        }
        
        private void CheckVisibility()
        {
            var player = GameController.Instance != null ? GameController.Instance.PlayerController : null;
            if (player == null) return;

            float playerZ = player.transform.position.z;

            // Check if obstacle has passed the player (disable sign when obstacle is behind player)
            if (_trackedObstacle != null && _trackedObstacle.activeInHierarchy)
            {
                float obstacleZ = _trackedObstacle.transform.position.z;
                float obstacleRelativeZ = obstacleZ - playerZ;

                // If obstacle has passed the player (z < -10), disable the sign
                if (obstacleRelativeZ < _obstaclePassedThreshold)
                {
                    if (_isVisible)
                    {
                        _isVisible = false;
                        SetVisualsVisible(false);
                    }
                    return; // Don't check chunk visibility if obstacle has passed
                }
            }

            // Get current chunk start Z from parent chunk if available
            WorldChunk parentChunk = GetComponentInParent<WorldChunk>();
            float currentChunkStartZ = parentChunk != null ? parentChunk.StartZ : _chunkStartZ;
            float distanceIntoChunk = playerZ - currentChunkStartZ;

            // Show sign when player is within the chunk (from m to n meters)
            if (!_isVisible && distanceIntoChunk >= _signAppearAtMeters && distanceIntoChunk < _signDisappearAtMeters)
            {
                _isVisible = true;
                SetVisualsVisible(true);
            }
            // Hide sign when player is outside the chunk range (before m or after n meters)
            else if ((_isVisible && distanceIntoChunk >= _signDisappearAtMeters) || 
                     (_isVisible && distanceIntoChunk < _signAppearAtMeters))
            {
                _isVisible = false;
                SetVisualsVisible(false);
            }
        }

        public void UpdateChunkStartZ(float chunkStartZ)
        {
            _chunkStartZ = chunkStartZ;
        }

        public void SetTrackedObstacle(GameObject obstacle)
        {
            _trackedObstacle = obstacle;
        }

        private void Update()
        {
            CheckVisibility();
        }
    }
}
