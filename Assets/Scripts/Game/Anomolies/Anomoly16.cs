using System.Collections.Generic;
using UnityEngine;

// The ceiling hangs just above the player's head - almost touching. Slabs and
// lights drop to fixed target heights (the lights sit just under the slabs).
// Colliders on the moved parts are switched off while active so the stairs stay
// walkable and nothing can trap the player.
public class Anomoly16 : MonoBehaviour, Anomoly
{

    [Tooltip("Ceiling slabs and ceiling-mounted lights, wired by the setup tooling.")]
    public List<Transform> ceilingParts = new List<Transform>();

    [Tooltip("Target local height for the ceiling slabs. Player head is at ~1.8.")]
    public float slabHeight = 2.25f;

    [Tooltip("Target local height for the light fixtures, just below the slabs.")]
    public float lightHeight = 2.05f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private readonly List<Vector3> originals = new List<Vector3>();

    public void Evaluate()
    {
        if (originals.Count > 0) return;   // already evaluated, do not stack
        foreach (Transform part in ceilingParts)
        {
            if (part == null) { originals.Add(Vector3.zero); continue; }
            originals.Add(part.localPosition);
            Vector3 p = part.localPosition;
            p.y = part.GetComponent<Light>() != null ? lightHeight : slabHeight;
            part.localPosition = p;
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
