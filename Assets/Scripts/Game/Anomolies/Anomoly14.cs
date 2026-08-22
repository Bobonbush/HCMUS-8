using UnityEngine;

// Someone stands facing the corner at the far end of the corridor, Exit 8 style.
// It does not move and does not react - which is exactly what makes it wrong.
public class Anomoly14 : MonoBehaviour, Anomoly
{

    [Tooltip("The silhouette in the corner. Hidden until Evaluate.")]
    public ShadowFigure figure;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    public void Evaluate()
    {
        if (figure == null) return;
        figure.facePlayer = false;   // it faces the wall, never you
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
