using System.Collections.Generic;
using UnityEngine;

// The ceiling sits slightly lower than it should. The whole ceiling - slabs and the
// lights hanging from it - drops by a hand's width. Nothing blocks your path; the
// room just feels wrong until you consciously check the height.
public class Anomoly16 : MonoBehaviour, Anomoly
{

    [Tooltip("Ceiling slabs and ceiling-mounted lights, wired by the setup tooling.")]
    public List<Transform> ceilingParts = new List<Transform>();

    [Tooltip("How far the ceiling drops, in metres. Keep it subtle.")]
    public float lowerBy = 0.35f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private readonly List<Vector3> originals = new List<Vector3>();

    public void Evaluate()
    {
        if (originals.Count > 0) return;   // already evaluated, do not stack
        foreach (Transform part in ceilingParts)
        {
            if (part == null) { originals.Add(Vector3.zero); continue; }
            originals.Add(part.localPosition);
            part.localPosition += Vector3.down * lowerBy;
        }
    }

    public void Restore()
    {
        for (int i = 0; i < ceilingParts.Count && i < originals.Count; i++)
        {
            if (ceilingParts[i] != null) ceilingParts[i].localPosition = originals[i];
        }
        originals.Clear();
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
