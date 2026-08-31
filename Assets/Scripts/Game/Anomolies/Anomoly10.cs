using System.Collections.Generic;
using UnityEngine;

// There are more ceiling lights in the corridor than there should be. Extra
// fixtures sit in the gaps between the real ones, prebuilt and disabled by the
// setup tooling, so nothing has to be spawned at runtime. Nobody counts the
// lights on a corridor they walk every day - which is exactly why it works.
public class Anomoly10 : MonoBehaviour, Anomoly
{

    [Tooltip("The extra ceiling fixtures, prebuilt and disabled by the setup tooling.")]
    public List<GameObject> extraLights = new List<GameObject>();

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    public void Evaluate()
    {
        SetAll(true);
    }

    public void Restore()
    {
        SetAll(false);
    }

    // Idempotent, so a double Evaluate or a Restore without Evaluate is harmless.
    private void SetAll(bool on)
    {
        foreach (GameObject light in extraLights)
        {
            if (light != null) light.SetActive(on);
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
