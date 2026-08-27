using UnityEngine;

// The corridor clock runs backwards. The clock always ticks (fast enough to read at
// a glance); this anomoly just flips its direction.
public class Anomoly23 : MonoBehaviour, Anomoly
{

    [Tooltip("The wall clock's hands driver.")]
    public ClockHands clock;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    public void Evaluate()
    {
        if (clock == null) return;
        clock.direction = -1f;
    }

    public void Restore()
    {
        if (clock == null) return;
        clock.direction = 1f;
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
