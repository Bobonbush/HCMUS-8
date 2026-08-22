using UnityEngine;

// Someone hangs from the classroom ceiling. Static, posed limp, facing one fixed
// direction - it never reacts, which is the point. The body sways very slightly
// on the rope.
public class Anomoly20 : MonoBehaviour, Anomoly
{

    [Tooltip("The hanged figure (doll + rope). Hidden until Evaluate.")]
    public ShadowFigure figure;

    [Tooltip("Degrees of slow sway on the rope. Keep tiny.")]
    public float swayDegrees = 1.5f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private bool active;
    private Quaternion baseRotation;

    public void Evaluate()
    {
        if (figure == null) return;
        active = true;
        baseRotation = figure.transform.rotation;
        figure.facePlayer = false;   // fixed direction, never turns to the player
        figure.Show(true);
    }

    public void Restore()
    {
        active = false;
        if (figure == null) return;
        figure.transform.rotation = baseRotation;
        figure.Show(false);
    }

    private void Update()
    {
        if (!active || figure == null) return;
        // pendulum around the rope's pivot axis
        float sway = Mathf.Sin(Time.time * 0.9f) * swayDegrees;
        figure.transform.rotation = baseRotation * Quaternion.Euler(sway, 0f, sway * 0.6f);
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
