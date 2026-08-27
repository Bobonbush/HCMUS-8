using System.Collections.Generic;
using UnityEngine;

// The ceiling is subtly WRONG: the whole plane tilts, sagging lower and lower
// toward the far end of the corridor. Prebuilt solid slanted slabs (with
// thickness, no see-through) appear and the light fixtures drop to hang just
// beneath the slanted plane at their own position along the slope.
public class Anomoly16 : MonoBehaviour, Anomoly
{

    [Tooltip("The slanted false-ceiling slabs, prebuilt and disabled by the setup tooling.")]
    public List<GameObject> ceilingBoxes = new List<GameObject>();

    [Tooltip("Ceiling-mounted light fixtures that drop onto the slanted plane.")]
    public List<Transform> lights = new List<Transform>();

    [Tooltip("Plane height (local y) at local z = 0.")]
    public float planeHeightAtZero = 4.9f;

    [Tooltip("Height change per metre of local z. Negative slope sinks toward -z (the far end).")]
    public float slopePerMeter = 0.043f;

    [Tooltip("How far below the plane the light fixtures hang.")]
    public float lightDrop = 0.2f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private readonly List<Vector3> originals = new List<Vector3>();

    public void Evaluate()
    {
        if (originals.Count > 0) return;   // already evaluated, do not stack
        foreach (GameObject box in ceilingBoxes)
        {
            if (box != null) box.SetActive(true);
        }
        foreach (Transform light in lights)
        {
            if (light == null) { originals.Add(Vector3.zero); continue; }
            originals.Add(light.localPosition);
            Vector3 p = light.localPosition;
            p.y = planeHeightAtZero + slopePerMeter * p.z - lightDrop;
            light.localPosition = p;
        }
    }

    public void Restore()
    {
        foreach (GameObject box in ceilingBoxes)
        {
            if (box != null) box.SetActive(false);
        }
        for (int i = 0; i < lights.Count && i < originals.Count; i++)
        {
            if (lights[i] != null) lights[i].localPosition = originals[i];
        }
        originals.Clear();
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
