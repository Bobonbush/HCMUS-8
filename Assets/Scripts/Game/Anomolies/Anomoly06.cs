using UnityEngine;

// Looking into the toilet mirror, someone is looking back at you. A flattened
// half-body silhouette sits against the mirror glass like a reflection that
// should not be there.
public class Anomoly06 : MonoBehaviour, Anomoly
{

    [Tooltip("The silhouette pressed against the mirror. Hidden until Evaluate.")]
    public ShadowFigure figure;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    public void Evaluate()
    {
        if (figure == null) return;
        figure.Show(true);
    }

    public void Restore()
    {
        if (figure == null) return;
        figure.Show(false);
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
