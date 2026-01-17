using UnityEngine;

namespace World
{
    public class WorldChunkComposite : MonoBehaviour
    {
        private Road _road;
        private WorldChunk _chunk;
        private Decoration _decoration;
        private float _currentZPosition;
        private bool _isActive = false;

        public Road Road => _road;
        public WorldChunk Chunk => _chunk;
        public Decoration Decoration => _decoration;
        public Difficulty Difficulty => _chunk != null ? _chunk.Difficulty : Difficulty.Easy;
        public float StartZ => _currentZPosition;
        public float EndZ => _currentZPosition + GetMaxLength();
        public bool IsActive => _isActive;

        private float GetMaxLength()
        {
            float maxLength = 0f;
            if (_road != null) maxLength = Mathf.Max(maxLength, _road.RoadLength);
            if (_chunk != null) maxLength = Mathf.Max(maxLength, _chunk.ChunkLength);
            if (_decoration != null) maxLength = Mathf.Max(maxLength, _decoration.DecorationLength);
            return maxLength;
        }

        public void Initialize(Road road, WorldChunk chunk, Decoration decoration, float zPosition)
        {
            _road = road;
            _chunk = chunk;
            _decoration = decoration;
            _currentZPosition = zPosition;
            _isActive = true;

            // Set composite position first
            transform.position = new Vector3(0, 0, zPosition);

            // Set parent and position - components should be at local origin
            if (_road != null)
            {
                _road.transform.SetParent(transform);
                _road.transform.localPosition = Vector3.zero;
                // Update road's internal position tracking for StartZ/EndZ calculations
                _road.UpdatePosition(zPosition);
                _road.gameObject.SetActive(true);
            }

            if (_chunk != null)
            {
                _chunk.transform.SetParent(transform);
                _chunk.transform.localPosition = Vector3.zero;
                // Initialize chunk (this handles randomization and collectible reset)
                _chunk.Initialize(zPosition);
                // Reset position to local origin since parent handles world position
                _chunk.transform.localPosition = Vector3.zero;
            }

            if (_decoration != null)
            {
                _decoration.transform.SetParent(transform);
                _decoration.transform.localPosition = Vector3.zero;
                // Update decoration's internal position tracking for StartZ/EndZ calculations
                _decoration.UpdatePosition(zPosition);
                _decoration.gameObject.SetActive(true);
            }

            gameObject.SetActive(true);
        }

        public void MoveComposite(float deltaMovement)
        {
            if (!_isActive) return;

            _currentZPosition -= deltaMovement;
            transform.position = new Vector3(transform.position.x, transform.position.y, _currentZPosition);

            if (_road != null)
            {
                _road.UpdatePosition(_currentZPosition);
            }

            if (_chunk != null)
            {
                _chunk.UpdatePosition(_currentZPosition);
            }

            if (_decoration != null)
            {
                _decoration.UpdatePosition(_currentZPosition);
            }
        }

        public bool HasPassedPlayer(float playerZPosition, float despawnOffset = 5f)
        {
            return EndZ < playerZPosition - despawnOffset;
        }

        public void ResetComposite()
        {
            _isActive = false;

            if (_road != null)
            {
                _road.ResetRoad();
            }

            if (_chunk != null)
            {
                _chunk.ResetChunk();
            }

            if (_decoration != null)
            {
                _decoration.ResetDecoration();
            }

            gameObject.SetActive(false);
        }
    }
}
