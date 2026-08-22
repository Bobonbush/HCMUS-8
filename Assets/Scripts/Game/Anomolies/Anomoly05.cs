using System.Collections.Generic;
using UnityEngine;

// Walking up to the glass, a doppelganger walks right behind you. The figure shadows
// the player's movement (same heading, fixed distance behind) but only materialises
// while the player is near one of the window panes, so you catch it in the corner of
// your eye through the glass - or when you spin around.
public class Anomoly05 : MonoBehaviour, Anomoly
{

    [Tooltip("The doppelganger silhouette.")]
    public ShadowFigure figure;

    [Tooltip("Window panes that trigger the doppelganger when the player is near.")]
    public List<Transform> windowZones = new List<Transform>();

    [Tooltip("Player-to-window distance that makes the doppelganger appear.")]
    public float triggerDistance = 4.2f;

    [Tooltip("Distance at which it disappears again (hysteresis, keep above triggerDistance).")]
    public float releaseDistance = 6f;

    [Tooltip("How far behind the player it walks.")]
    public float behindDistance = 2.6f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private bool active;
    private bool shown;

    public void Evaluate()
    {
        active = true;
        shown = false;
    }

    public void Restore()
    {
        active = false;
        shown = false;
        if (figure != null) figure.Show(false);
    }

    private void Update()
    {
        if (!active || figure == null || GameManager.Instance == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null) return;

        // Only react to a player on this floor (the host sits under the floor root).
        float floorY = transform.parent != null ? transform.parent.position.y : transform.position.y;
        if (Mathf.Abs(player.position.y - floorY) > 3f)
        {
            if (shown) { shown = false; figure.Show(false); }
            return;
        }

        float nearest = float.MaxValue;
        foreach (Transform zone in windowZones)
        {
            if (zone == null) continue;
            nearest = Mathf.Min(nearest, Vector3.Distance(player.position, zone.position));
        }

        if (!shown && nearest < triggerDistance) { shown = true; figure.Show(true); }
        else if (shown && nearest > releaseDistance) { shown = false; figure.Show(false); }

        if (shown)
        {
            Vector3 forward = player.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 pos = player.position - forward * behindDistance;
            pos.y = player.position.y;
            figure.transform.position = pos;
            // Same heading as the player, like a shadow following in step.
            figure.transform.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
