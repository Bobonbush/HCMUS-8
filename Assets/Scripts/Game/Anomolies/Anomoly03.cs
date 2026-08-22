using UnityEngine;

// Someone on the upper or lower floor watching us through the stairwell.
// Type is AddUp: AnomolyManager picks +1 or -1 and this Evaluate runs on that
// neighbour floor's instance, not on the player's own floor.
public class Anomoly03 : MonoBehaviour, Anomoly
{

    [Tooltip("The silhouette that does the watching. Hidden until Evaluate.")]
    public ShadowFigure figure;

    [Tooltip("Where the figure stands when this floor is ABOVE the player (visible looking up the stairwell).")]
    public Transform upperAnchor;

    [Tooltip("Where the figure stands when this floor is BELOW the player (on the stair landing, visible looking down).")]
    public Transform lowerAnchor;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.AddUp;

    public void Evaluate()
    {
        if (figure == null) return;

        // This runs on the neighbour floor: are we above or below the player?
        Transform player = GameManager.Instance != null ? GameManager.Instance.GetPlayerTransform() : null;
        Transform anchor = upperAnchor;
        if (player != null && transform.position.y < player.position.y) anchor = lowerAnchor;
        if (anchor == null) anchor = upperAnchor != null ? upperAnchor : lowerAnchor;
        if (anchor == null) return;

        figure.transform.position = anchor.position;
        figure.transform.rotation = anchor.rotation;
        figure.facePlayer = true;
        figure.Show(true);
    }

    public void Restore()
    {
        // Also runs on instances where Evaluate never did (AddUp bookkeeping) - stay defensive.
        if (figure == null) return;
        figure.facePlayer = false;
        figure.Show(false);
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
