using UnityEngine;
using World;

public class ObjectPlacer : MonoBehaviour
{

    [SerializeField] private ObjectsContainerSO objectsContainer;
    [SerializeField] private float speed;
    [SerializeField] private bool randomRotation = false;
    [SerializeField] private float minRotation = 0;
    [SerializeField] private float maxRotation = 360;
    [SerializeField] private bool randomScale = false;
    [SerializeField] private float minScale = 1;
    [SerializeField] private float maxScale = 1.5f;
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
            
            _currentObject = Instantiate(_currentObjectData.objectPrefab, transform.position, Quaternion.identity, transform);
            if (_currentObject == null)
            {
                Debug.LogError("ObjectPlacer: Failed to instantiate object!", this);
                return;
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
        }
        else
        {
            _currentObjectData = objectsContainer.GetRandomObjectBySpeed(speed);
            if (_currentObjectData != null && 
                _currentObjectData.objectPrefab != null &&
                _currentObjectData.objectPrefab.GetComponent<WorldObstacle>() != null)
            {
                _currentObject = Instantiate(_currentObjectData.objectPrefab, transform.position, Quaternion.identity, transform);
                if (_currentObject != null)
                {
                    _currentObject.GetComponent<WorldObstacle>().ConfigureFromObjectData(_currentObjectData);
                }
            }
        }
    }
}
