using System.Collections.Generic;
using UnityEngine;

// A conference call-for-papers poster is pinned to the green board in the corridor
// by the toilets. On an anomoly round the host logo in its top-right corner is no
// longer HCMUS - it is UIT.
//
// The poster is dense with dates and topics nobody reads, so the small crest is the
// only thing on it a player can actually verify. Two prebuilt materials are swapped
// on the poster renderer; they are identical in every pixel except that logo, so
// there is nothing else to pattern-match on. Nothing is written to the assets.
public class Anomoly22 : MonoBehaviour, Anomoly
{

    [Tooltip("The poster quads pinned to the corridor board.")]
    public List<MeshRenderer> boardRenderers = new List<MeshRenderer>();

    [Tooltip("Normal state: the poster with the HCMUS crest.")]
    public Material normalMaterial;

    [Tooltip("Anomoly state: the same poster with the UIT crest.")]
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
        foreach (MeshRenderer renderer in boardRenderers)
        {
            if (renderer != null) renderer.sharedMaterial = material;
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
