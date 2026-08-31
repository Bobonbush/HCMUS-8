using System.Collections.Generic;
using UnityEngine;

// One ceiling light hangs at the wrong angle - the tube turned across the
// corridor instead of along it, or cocked at forty-five degrees.
//
// Only the fixture mesh is rotated, never the Light component that sits on its
// parent, so the corridor is lit exactly as it always is. There is no lighting
// tell at all; the shape overhead is simply wrong.
public class Anomoly26 : MonoBehaviour, Anomoly
{

    [Tooltip("The fixture meshes (the Led child of each ceiling light), wired by the setup tooling.")]
    public List<Transform> fixtures = new List<Transform>();

    [Tooltip("Extra local rotation applied to the picked fixtures. Retune the axis in the editor.")]
    public Vector3 tiltEuler = new Vector3(0f, 0f, 45f);

    [Tooltip("How many fixtures are turned. One is plenty.")]
    public int howMany = 1;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private readonly List<int> picked = new List<int>();
    private readonly List<Quaternion> originals = new List<Quaternion>();

    public void Evaluate()
    {
        if (picked.Count > 0) return;   // already turned, do not compound the tilt

        // Draw distinct indices so the same fixture is never picked twice.
        List<int> pool = new List<int>();
        for (int i = 0; i < fixtures.Count; i++)
        {
            if (fixtures[i] != null) pool.Add(i);
        }

        int count = Mathf.Min(howMany, pool.Count);
        for (int n = 0; n < count; n++)
        {
            int slot = Random.Range(0, pool.Count);
            int index = pool[slot];
            pool.RemoveAt(slot);

            picked.Add(index);
            originals.Add(fixtures[index].localRotation);
            fixtures[index].localRotation = fixtures[index].localRotation * Quaternion.Euler(tiltEuler);
        }
    }

    public void Restore()
    {
        for (int i = 0; i < picked.Count && i < originals.Count; i++)
        {
            if (picked[i] >= fixtures.Count) continue;
            Transform fixtureTransform = fixtures[picked[i]];
            if (fixtureTransform != null) fixtureTransform.localRotation = originals[i];
        }
        picked.Clear();
        originals.Clear();
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
