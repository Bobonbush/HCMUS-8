using System.Collections.Generic;
using UnityEngine;

public class Anomoly28 : MonoBehaviour, Anomoly
{
    public List<GameObject> windowFaces = new List<GameObject>();
    [Min(1)] public int faceCount = 11;
    [Min(1)] public int columns = 3;
    public Vector2 spacing = new Vector2(0.42f, 0.50f);
    public bool arrangeAsGrid = true;
    [Tooltip("Optional glass plane. Its forward points towards the viewer.")]
    public Transform glassPlane;
    public float glassClearance = 0.35f;
    readonly List<GameObject> generated = new List<GameObject>();
    bool active;
    public void Evaluate()
    {
        if (active) return;
        active = true;
        var originals = new List<GameObject>();
        foreach (var face in windowFaces) if (face != null && !originals.Contains(face)) originals.Add(face);
        if (originals.Count == 0) return;
        // Authored originals never move. Extra faces are distributed around the first anchor.
        foreach (var face in originals) face.SetActive(true);
        var anchor = originals[0].transform;
        int count = Mathf.Clamp(Mathf.Max(faceCount, windowFaces.Count), originals.Count, 64);
        for (int i = originals.Count; i < count; i++)
        {
            int index = i - originals.Count;
            GameObject copy;
            if (index < generated.Count) copy = generated[index];
            else
            {
                copy = Instantiate(originals[0], anchor.parent);
                copy.name = "WindowFace_Cluster_" + index;
                copy.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                generated.Add(copy);
                AnomalyFaceVariation.Apply(copy, i);
            }
            // Golden-angle packing avoids rows and retains the user's anchor as the centre.
            float angle = i * 2.399963f;
            float radius = Mathf.Sqrt(i) * 0.37f;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius * spacing.x / 0.42f,
                Mathf.Sin(angle) * radius * spacing.y / 0.50f, -0.08f - (i % 3) * 0.07f);
            copy.transform.SetPositionAndRotation(anchor.position + anchor.rotation * offset,
                anchor.rotation * Quaternion.Euler((i % 3 - 1) * 7, (i % 5 - 2) * 5, (i % 7 - 3) * 9));
            copy.transform.localScale = anchor.localScale * (0.55f + (i * 37 % 71) / 100f);
            if (glassPlane != null)
            {
                float depth = Vector3.Dot(copy.transform.position - glassPlane.position, glassPlane.forward);
                if (depth > -glassClearance) copy.transform.position -= glassPlane.forward * (depth + glassClearance);
            }
            copy.SetActive(true);
        }
    }
    public void Restore()
    {
        active = false;
        foreach (var face in windowFaces) if (face != null) face.SetActive(false);
        foreach (var face in generated) if (face != null) face.SetActive(false);
        if (!Application.isPlaying)
        {
            foreach (var face in generated) if (face != null) DestroyImmediate(face);
            generated.Clear();
        }
    }
    void OnDisable() { Restore(); }
    void OnDestroy()
    {
        foreach (var face in generated) if (face != null)
        { if (Application.isPlaying) Destroy(face); else DestroyImmediate(face); }
    }
    public Anomoly.EvaluateType getType() => Anomoly.EvaluateType.Single;
}
