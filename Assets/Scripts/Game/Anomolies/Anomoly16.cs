using System.Collections.Generic;
using UnityEngine;

// The ceiling hangs far lower than it should - just above what you can reach
// with a jump. The slabs and the lights drop together. Colliders on the moved
// parts are switched off while the anomoly is active so the stairs stay
// walkable and nothing can trap the player.
public class Anomoly16 : MonoBehaviour, Anomoly
{

    [Tooltip("Ceiling slabs and ceiling-mounted lights, wired by the setup tooling.")]
    public List<Transform> ceilingParts = new List<Transform>();

    [Tooltip("How far the ceiling drops, in metres. 2.7 puts it just above jump reach.")]
    public float lowerBy = 2.7f;

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
            SetColliders(part, false);
        }
    }

    public void Restore()
    {
        for (int i = 0; i < ceilingParts.Count && i < originals.Count; i++)
        {
            if (ceilingParts[i] == null) continue;
            ceilingParts[i].localPosition = originals[i];
            SetColliders(ceilingParts[i], true);
        }
        originals.Clear();
    }

    private static void SetColliders(Transform part, bool on)
    {
        foreach (Collider c in part.GetComponentsInChildren<Collider>()) c.enabled = on;
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
