using System.Collections.Generic;
using UnityEngine;

// One classroom door sits slightly wrong in its frame - shifted sideways with a
// small crooked tilt, leaving a thin gap along one side. Easy to walk past,
// obvious once you actually look.
public class Anomoly27 : MonoBehaviour, Anomoly
{

    [Tooltip("The door leaves to nudge, wired by the setup tooling.")]
    public List<Transform> doorParts = new List<Transform>();

    [Tooltip("Sideways slide along the wall, in metres.")]
    public float slide = 0.14f;

    [Tooltip("Extra crooked yaw on the leaves, in degrees.")]
    public float tilt = 4f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private readonly List<Vector3> originalPositions = new List<Vector3>();
    private readonly List<Quaternion> originalRotations = new List<Quaternion>();

    public void Evaluate()
    {
        if (originalPositions.Count > 0) return;   // already applied
        foreach (Transform part in doorParts)
        {
            if (part == null) { originalPositions.Add(Vector3.zero); originalRotations.Add(Quaternion.identity); continue; }
            originalPositions.Add(part.localPosition);
            originalRotations.Add(part.localRotation);
            part.localPosition += new Vector3(0f, 0f, slide);
            part.localRotation = part.localRotation * Quaternion.Euler(0f, tilt, 0f);
        }
    }

    public void Restore()
    {
        for (int i = 0; i < doorParts.Count && i < originalPositions.Count; i++)
        {
            if (doorParts[i] == null) continue;
            doorParts[i].localPosition = originalPositions[i];
            doorParts[i].localRotation = originalRotations[i];
        }
        originalPositions.Clear();
        originalRotations.Clear();
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
