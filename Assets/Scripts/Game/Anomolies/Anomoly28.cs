using System.Collections.Generic;
using UnityEngine;

// A pale face waits just outside the window beside the toilets.
public class Anomoly28 : MonoBehaviour, Anomoly
{
    public List<GameObject> windowFaces = new List<GameObject>();
    private readonly Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;
    public void Evaluate() { SetVisible(true); }
    public void Restore() { SetVisible(false); }
    private void SetVisible(bool visible)
    {
        foreach (GameObject face in windowFaces)
            if (face != null) face.SetActive(visible);
    }
    public Anomoly.EvaluateType getType() { return type; }
}
