using System.Collections.Generic;
using UnityEngine;

// The ceiling hangs far lower than it should. A set of solid box slabs (prebuilt,
// hidden) appears at the low height and the light fixtures drop just beneath them.
// Boxes are real geometry with thickness, visible from below and colliding, so the
// player can neither see through nor jump through the false ceiling.
public class Anomoly16 : MonoBehaviour, Anomoly
{

    [Tooltip("The false low-ceiling slabs, prebuilt and disabled by the setup tooling.")]
    public List<GameObject> ceilingBoxes = new List<GameObject>();

    [Tooltip("Ceiling-mounted light fixtures that drop with the false ceiling.")]
    public List<Transform> lights = new List<Transform>();

    [Tooltip("Target local height for the dropped light fixtures, just below the boxes.")]
    public float lightHeight = 2.9f;

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
            p.y = lightHeight;
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
