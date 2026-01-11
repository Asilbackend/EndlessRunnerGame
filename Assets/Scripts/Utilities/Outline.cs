using UnityEngine;
using System.Linq;

namespace Utilities
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class SmoothOutlineNormals : MonoBehaviour
    {
        private static readonly HashSet<Mesh> Processed = new();

        void Awake()
        {
            ApplyToMeshFilters();
            ApplyToSkinnedMeshes();
        }

        void ApplyToMeshFilters()
        {
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                if (!Processed.Add(mesh)) continue;

                BakeSmoothNormalsIntoVertexColors(mesh);
            }
        }

        void ApplyToSkinnedMeshes()
        {
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh;
                if (mesh == null) continue;
                if (!Processed.Add(mesh)) continue;

                BakeSmoothNormalsIntoVertexColors(mesh);
            }
        }

        static void BakeSmoothNormalsIntoVertexColors(Mesh mesh)
        {
            var verts = mesh.vertices;
            var norms = mesh.normals;

            // If mesh has no normals, compute them once
            if (norms == null || norms.Length != verts.Length)
            {
                mesh.RecalculateNormals();
                norms = mesh.normals;
            }

            // Group by same-position vertices and average their normals
            var groups = verts.Select((v, i) => new KeyValuePair<Vector3, int>(v, i))
                              .GroupBy(p => p.Key);

            var smooth = new Vector3[norms.Length];
            norms.CopyTo(smooth, 0);

            foreach (var g in groups)
            {
                if (g.Count() == 1) continue;

                Vector3 avg = Vector3.zero;
                foreach (var p in g) avg += smooth[p.Value];
                avg.Normalize();

                foreach (var p in g) smooth[p.Value] = avg;
            }

            // Pack [-1..1] normal into [0..1] color
            var colors = new Color[smooth.Length];
            for (int i = 0; i < smooth.Length; i++)
            {
                Vector3 n = smooth[i];
                colors[i] = new Color(n.x * 0.5f + 0.5f,
                                      n.y * 0.5f + 0.5f,
                                      n.z * 0.5f + 0.5f,
                                      1f);
            }

            mesh.colors = colors;
        }
    }

}