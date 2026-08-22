using System.Collections.Generic;
using UnityEngine;

// The classroom banner changes from APCS (Advanced Program in Computer Science)
// to TPCS (Terrible Program in Computer Science). Two prebuilt materials are
// swapped on the banner renderers, so nothing is ever written to the assets.
public class Anomoly07 : MonoBehaviour, Anomoly
{

    [Tooltip("The banner quads above the classroom boards.")]
    public List<MeshRenderer> bannerRenderers = new List<MeshRenderer>();

    [Tooltip("Normal state: the APCS banner.")]
    public Material normalMaterial;

    [Tooltip("Anomoly state: the TPCS banner.")]
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
        foreach (MeshRenderer renderer in bannerRenderers)
        {
            if (renderer != null) renderer.sharedMaterial = material;
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
