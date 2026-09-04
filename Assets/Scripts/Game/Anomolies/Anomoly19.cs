using System.Collections.Generic;
using UnityEngine;

// A corpse is revealed inside the men's toilet.
public class Anomoly19 : MonoBehaviour, Anomoly
{
    public List<GameObject> corpses = new List<GameObject>();
    private readonly Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;
    public void Evaluate() { SetVisible(true); }
    public void Restore() { SetVisible(false); }
    private void SetVisible(bool visible)
    {
        foreach (GameObject corpse in corpses)
            if (corpse != null) corpse.SetActive(visible);
    }
    public Anomoly.EvaluateType getType() { return type; }
}
