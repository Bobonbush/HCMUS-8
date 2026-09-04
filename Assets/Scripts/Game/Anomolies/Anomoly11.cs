using UnityEngine;

// The familiar corridor bin is replaced by a visibly different office bin.
public class Anomoly11 : MonoBehaviour, Anomoly
{
    public GameObject normalTrash;
    public GameObject replacementTrash;

    private readonly Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    public void Evaluate()
    {
        if (normalTrash != null) normalTrash.SetActive(false);
        if (replacementTrash != null) replacementTrash.SetActive(true);
    }

    public void Restore()
    {
        if (normalTrash != null) normalTrash.SetActive(true);
        if (replacementTrash != null) replacementTrash.SetActive(false);
    }

    public Anomoly.EvaluateType getType() { return type; }
}
