using System.Collections.Generic;
using UnityEngine;

// The school crest by the lifts belongs to a different university - the HCMUS
// logo is quietly replaced by UEH's. Same plaque, same size, same place on the
// wall; only the crest is wrong.
public class Anomoly24 : MonoBehaviour, Anomoly
{

    [Tooltip("The logo plaque quads on the lobby wall.")]
    public List<MeshRenderer> logoRenderers = new List<MeshRenderer>();

    [Tooltip("Normal state: the HCMUS crest.")]
    public Material normalMaterial;

    [Tooltip("Anomoly state: the UEH crest.")]
    public Material anomolyMaterial;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    public void Evaluate()
    {
        SwapTo(anomolyMaterial);
    }

    public void Restore()
    {
        SwapTo(normalMaterial);
    }

    private void SwapTo(Material material)
    {
        if (material == null) return;
        foreach (MeshRenderer renderer in logoRenderers)
        {
            if (renderer != null) renderer.sharedMaterial = material;
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
