using System.Collections.Generic;
using UnityEngine;

// Each copy owns its mesh; never deform the shared imported asset.
public class AnomalyFaceVariation : MonoBehaviour
{
    readonly List<Mesh> owned = new List<Mesh>();
    public static void Apply(GameObject root, int seed)
    {
        var owner = root.AddComponent<AnomalyFaceVariation>();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
            var mesh = Instantiate(filter.sharedMesh);
            var vertices = mesh.vertices;
            var b = mesh.bounds;
            for (int i = 0; i < vertices.Length; i++)
            {
                var p = vertices[i];
                float y = (p.y - b.min.y) / Mathf.Max(0.001f, b.size.y);
                float x = (p.x - b.center.x) / Mathf.Max(0.001f, b.extents.x);
                float mouth = Mathf.Exp(-Mathf.Pow((y - 0.30f) * 9f, 2)) * Mathf.Exp(-x * x * 5f);
                // Drooping jaw, asymmetric smile or stretched scream, with uneven brow.
                p.y += b.size.y * mouth * (seed % 3 == 0 ? -0.16f : seed % 3 == 1 ? 0.09f * Mathf.Abs(x) : -0.06f * x);
                p.x += b.size.x * 0.04f * Mathf.Sin(y * 8f + seed);
                vertices[i] = p;
            }
            mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            filter.sharedMesh = mesh; owner.owned.Add(mesh);
        }
        var colors = new[] { new Color(0.72f,0.78f,0.70f), new Color(0.46f,0.55f,0.62f),
            new Color(0.73f,0.53f,0.50f), new Color(0.86f,0.83f,0.72f) };
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", colors[seed % colors.Length]);
            block.SetColor("_Color", colors[seed % colors.Length]);
            renderer.SetPropertyBlock(block);
        }
    }
    void OnDestroy()
    {
        foreach (var mesh in owned) if (mesh != null)
        { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
    }
}
