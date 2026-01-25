using UnityEngine;
using World;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObjectPlacer : MonoBehaviour
{

    [SerializeField] private ObjectsContainerSO objectsContainer;
    [SerializeField] private bool isDynamic = false;
    [SerializeField] private float speed;
    
    [Header("Gizmo Settings")]
    [SerializeField] private bool showMeetingPointGizmos = true;
    [SerializeField] private float activationDistance = 60f;
    [SerializeField] private float worldSpeed = 20f;
    [SerializeField] private bool randomRotation = false;
    [SerializeField] private float minRotation = 0;
    [SerializeField] private float maxRotation = 360;
    [SerializeField] private bool randomScale = false;
    [SerializeField] private float minScale = 1;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private bool randomPosition = false;
    [SerializeField] private float minXOffset = -1f;
    [SerializeField] private float maxXOffset = 1f;
    [SerializeField] private float minZOffset = -1f;
    [SerializeField] private float maxZOffset = 1f;
    [SerializeField] private bool hasExtraPoints = false;
    [SerializeField] private GameObject extraPointsPrefab;
    [SerializeField] private float extraPointsDistance = 1f;
    private ObjectData _currentObjectData;
    private GameObject _currentObject;


    private void Awake()
    {
        if (_currentObject == null)
        {
            RandomizePlacement();
        }
    }

    // Public method so external code (e.g., WorldChunk.Initialize) can re-roll placement when chunk is spawned
    public void RandomizePlacement()
    {
        // Destroy previous object if present
        if (_currentObject != null)
        {
            // DestroyImmediate when in editor might be useful, but use regular Destroy for runtime safety
            Destroy(_currentObject);
            _currentObject = null;
            _currentObjectData = null;
        }

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
            
        if (objectsContainer == null)
        {
            Debug.LogError("ObjectPlacer: objectsContainer is not assigned!", this);
            return;
        }
        
        if (speed == 0)
        {
            _currentObjectData = objectsContainer.GetRandomObject();
            if (_currentObjectData == null || _currentObjectData.objectPrefab == null)
            {
                Debug.LogWarning("ObjectPlacer: Could not get valid object data or prefab is null!", this);
                return;
            }
            
            // If dynamic and speed is 0, set speed to the randomly picked object's speed
            if (isDynamic && _currentObjectData.speed > 0)
            {
                speed = _currentObjectData.speed;
            }
            
            Vector3 spawnPosition = transform.position;
            if (randomPosition)
            {
                float randomX = Random.Range(minXOffset, maxXOffset);
                float randomZ = Random.Range(minZOffset, maxZOffset);
                spawnPosition += new Vector3(randomX, 0f, randomZ);
            }
            _currentObject = Instantiate(_currentObjectData.objectPrefab, spawnPosition, Quaternion.identity, transform);
            if (_currentObject == null)
            {
                Debug.LogError("ObjectPlacer: Failed to instantiate object!", this);
                return;
            }
            
            // Configure obstacle if it's dynamic
            if (isDynamic && _currentObject.GetComponent<WorldObstacle>() != null)
            {
                _currentObject.GetComponent<WorldObstacle>().ConfigureFromObjectData(_currentObjectData);
            }
            
            if (randomRotation)
            {
                float randomYRotation = Random.Range(minRotation, maxRotation);
                _currentObject.transform.Rotate(0, randomYRotation, 0);
            }
            if (randomScale)
            {
                float randomScaleValue = Random.Range(minScale, maxScale);
                _currentObject.transform.localScale = new Vector3(randomScaleValue, randomScaleValue, randomScaleValue);
            }
            
            // Spawn extra points prefab if enabled
            if (hasExtraPoints && extraPointsPrefab != null)
            {
                Vector3 extraPointsPosition = _currentObject.transform.position + _currentObject.transform.forward * extraPointsDistance;
                GameObject extraPoints = Instantiate(extraPointsPrefab, extraPointsPosition, Quaternion.identity, _currentObject.transform);
            }
        }
        else
        {
            _currentObjectData = objectsContainer.GetRandomObjectBySpeed(speed);
            if (_currentObjectData != null && 
                _currentObjectData.objectPrefab != null &&
                _currentObjectData.objectPrefab.GetComponent<WorldObstacle>() != null)
            {
                Vector3 spawnPosition = transform.position;
                if (randomPosition)
                {
                    float randomX = Random.Range(minXOffset, maxXOffset);
                    float randomZ = Random.Range(minZOffset, maxZOffset);
                    spawnPosition += new Vector3(randomX, 0f, randomZ);
                }
                _currentObject = Instantiate(_currentObjectData.objectPrefab, spawnPosition, Quaternion.identity, transform);
                if (_currentObject != null)
                {
                    _currentObject.GetComponent<WorldObstacle>().ConfigureFromObjectData(_currentObjectData);
                    
                    // Spawn extra points prefab if enabled
                    if (hasExtraPoints && extraPointsPrefab != null)
                    {
                        Vector3 extraPointsPosition = _currentObject.transform.position + _currentObject.transform.forward * extraPointsDistance;
                        GameObject extraPoints = Instantiate(extraPointsPrefab, extraPointsPosition, Quaternion.identity, _currentObject.transform);
                    }
                }
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        // Draw dynamic obstacle meeting point gizmos
        if (isDynamic && showMeetingPointGizmos)
        {
            float[] obstacleSpeeds;
            Color[] gizmoColors;
            
            if (speed == 0f)
            {
                // If speed is 0, show gizmos for obstacle speeds 7, 8, 9
                obstacleSpeeds = new float[] { 7f, 8f, 9f };
                gizmoColors = new Color[] { Color.red, Color.green, Color.blue };
            }
            else
            {
                // If speed is not 0, use that speed
                obstacleSpeeds = new float[] { speed };
                gizmoColors = new Color[] { Color.cyan };
            }
            
            for (int i = 0; i < obstacleSpeeds.Length; i++)
            {
                float obstacleSpeed = obstacleSpeeds[i];
                
                // Skip if obstacle speed >= world speed (they'll never meet)
                if (obstacleSpeed >= worldSpeed) continue;
                
                // Formula: obstacleSpeed * (activationDistance / (worldSpeed - obstacleSpeed))
                float meetingDistance = obstacleSpeed * (activationDistance / (worldSpeed - obstacleSpeed));
                
                // Calculate meeting point position (ahead of obstacle spawn in Z direction)
                Vector3 meetingPoint = transform.position + Vector3.forward * meetingDistance;
                float actualZ = meetingPoint.z;
                
                // Draw gizmo
                Gizmos.color = gizmoColors[i];
                Gizmos.DrawWireSphere(meetingPoint, 1f);
                
                // Draw line from obstacle spawn to meeting point
                Gizmos.DrawLine(transform.position, meetingPoint);
                
                // Draw label (only in Scene view)
                #if UNITY_EDITOR
                if (speed == 0f)
                {
                    Handles.Label(meetingPoint + Vector3.up * 2f, $"Obstacle Speed {obstacleSpeed}: Z = {actualZ:F1}");
                }
                else
                {
                    Handles.Label(meetingPoint + Vector3.up * 2f, $"Z = {actualZ:F1}");
                }
                #endif
            }
        }
        
        // Draw spawn position marker
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        // Draw extra points gizmo if enabled
        if (hasExtraPoints && extraPointsPrefab != null)
        {
            // Calculate extra points position (using forward direction, accounting for potential rotation)
            Vector3 forwardDirection = transform.forward;
            Vector3 extraPointsPosition = transform.position + forwardDirection * extraPointsDistance;
            
            // Draw golden gizmo
            Gizmos.color = new Color(1f, 0.84f, 0f, 1f); // Golden color
            Gizmos.DrawWireSphere(extraPointsPosition, 0.5f);
            
            // Draw line from spawn position to extra points position
            Gizmos.DrawLine(transform.position, extraPointsPosition);
            
            #if UNITY_EDITOR
            Handles.Label(extraPointsPosition + Vector3.up * 1f, "Extra Points");
            #endif
        }
    }
}
