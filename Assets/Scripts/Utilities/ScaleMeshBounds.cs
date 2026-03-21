using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class ScaleMeshBounds : MonoBehaviour
{
    [Tooltip("Multiply mesh bounds by this factor")]
    public float multiplier = 2f;

    private MeshFilter _filter;
    private MeshRenderer _renderer;

    void Awake()
    {
        _filter = GetComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying)
            ApplyBounds();
    }
#endif

    void Start() => ApplyBounds();

    void ApplyBounds()
    {
        if (_filter == null) _filter = GetComponent<MeshFilter>();
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        if (_filter == null || _renderer == null || _filter.sharedMesh == null) return;

        Bounds b = _filter.sharedMesh.bounds;
        if (b.size == Vector3.zero) return;

        _renderer.localBounds = new Bounds(b.center, b.size * multiplier);
    }
}
