using System.Collections.Generic;
using UnityEngine;

// Extra CCTV cameras appear throughout the corridor. The permanent cameras are
// decoration; this component owns only the additional anomaly cameras.
public class Anomoly34 : MonoBehaviour, Anomoly
{
    [Tooltip("Extra security camera roots. They must be inactive in the normal state.")]
    public List<GameObject> extraCameras = new List<GameObject>();
    private readonly Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;
    public void Evaluate() { SetVisible(true); }
    public void Restore() { SetVisible(false); }
    private void SetVisible(bool visible)
    {
        foreach (GameObject cameraObject in extraCameras)
            if (cameraObject != null) cameraObject.SetActive(visible);
    }
    public Anomoly.EvaluateType getType() { return type; }
}
