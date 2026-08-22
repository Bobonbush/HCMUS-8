using UnityEngine;

// Something on the neighbour floor, seen through the atrium.
// Above the player: a body hangs from the railing of the floor above, dangling
// into the stairwell shaft - you only see it when you look up.
// Below the player: a doll stands on the stair landing, looking up.
// Type is AddUp: AnomolyManager picks +1 or -1 and this Evaluate runs on that
// neighbour floor's instance, not on the player's own floor.
public class Anomoly03 : MonoBehaviour, Anomoly
{

    [Tooltip("The standing figure used when this floor is BELOW the player.")]
    public ShadowFigure figure;

    [Tooltip("The hanged figure (doll + rope off the railing) used when this floor is ABOVE the player.")]
    public ShadowFigure hangedFigure;

    [Tooltip("Where the standing figure goes when this floor is below the player.")]
    public Transform lowerAnchor;

    [Tooltip("Degrees of slow sway for the hanging body.")]
    public float swayDegrees = 1.2f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.AddUp;

    private bool hangedActive;
    private Quaternion hangedBaseRotation;

    public void Evaluate()
    {
        Transform player = GameManager.Instance != null ? GameManager.Instance.GetPlayerTransform() : null;
        bool isAbove = player == null || transform.position.y > player.position.y;

        if (isAbove)
        {
            if (hangedFigure == null) return;
            hangedActive = true;
            hangedBaseRotation = hangedFigure.transform.rotation;
            hangedFigure.facePlayer = false;   // fixed, never turns
            hangedFigure.Show(true);
        }
        else
        {
            if (figure == null || lowerAnchor == null) return;
            figure.transform.position = lowerAnchor.position;
            figure.transform.rotation = lowerAnchor.rotation;
            figure.facePlayer = false;
            figure.Show(true);
        }
    }

    public void Restore()
    {
        hangedActive = false;
        if (figure != null) figure.Show(false);
        if (hangedFigure != null)
        {
            hangedFigure.transform.rotation = hangedBaseRotation == default ? hangedFigure.transform.rotation : hangedBaseRotation;
            hangedFigure.Show(false);
        }
    }

    private void Update()
    {
        if (!hangedActive || hangedFigure == null) return;
        float sway = Mathf.Sin(Time.time * 0.8f) * swayDegrees;
        hangedFigure.transform.rotation = hangedBaseRotation * Quaternion.Euler(sway, 0f, sway * 0.7f);
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
