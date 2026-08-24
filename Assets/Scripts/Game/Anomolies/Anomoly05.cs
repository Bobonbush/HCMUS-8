using System.Collections.Generic;
using UnityEngine;

// Walking up to the glass, a figure appears outside and slowly advances toward
// the window - the player watches it come closer through the "reflection".
// It never gets through; it just keeps arriving.
public class Anomoly05 : MonoBehaviour, Anomoly
{

    [Tooltip("The zombie figure. Hidden until the player is near a window.")]
    public ShadowFigure figure;

    [Tooltip("Window panes that can show the approaching figure.")]
    public List<Transform> windowZones = new List<Transform>();

    [Tooltip("Player-to-window distance that makes the figure appear outside.")]
    public float triggerDistance = 5f;

    [Tooltip("Distance at which everything resets (hysteresis).")]
    public float releaseDistance = 7.5f;

    [Tooltip("How far beyond the glass the figure starts.")]
    public float spawnDistance = 7f;

    [Tooltip("How close to the glass it gets before stopping.")]
    public float stopDistance = 0.7f;

    [Tooltip("Approach speed in metres per second. Slow reads scarier.")]
    public float approachSpeed = 0.85f;

    [Tooltip("Degrees of drunken sway while it walks.")]
    public float swayDegrees = 5f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private bool active;
    private bool shown;
    private Transform currentWindow;
    private Vector3 outward;

    public void Evaluate()
    {
        active = true;
        shown = false;
    }

    public void Restore()
    {
        active = false;
        shown = false;
        currentWindow = null;
        if (figure != null) figure.Show(false);
    }

    private void Update()
    {
        if (!active || figure == null || GameManager.Instance == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null) return;

        float floorY = transform.parent != null ? transform.parent.position.y : transform.position.y;
        if (Mathf.Abs(player.position.y - floorY) > 3f) { Hide(); return; }

        // Nearest window to the player.
        Transform nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Transform zone in windowZones)
        {
            if (zone == null) continue;
            float d = Vector3.Distance(player.position, zone.position);
            if (d < nearestDistance) { nearestDistance = d; nearest = zone; }
        }
        if (nearest == null) return;

        if (!shown && nearestDistance < triggerDistance)
        {
            shown = true;
            currentWindow = nearest;
            // Outward = the window pane's normal, away from the floor interior.
            // The pane is thin: its normal is the thinnest horizontal axis of its bounds.
            Bounds zoneBounds = new Bounds(currentWindow.position, Vector3.one * 0.5f);
            Renderer[] renderers = currentWindow.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                zoneBounds = renderers[0].bounds;
                foreach (Renderer r in renderers) zoneBounds.Encapsulate(r.bounds);
            }
            Vector3 interior = (transform.parent != null ? transform.parent.position : transform.position)
                             + new Vector3(19f, 0f, -15f);   // rough centre of the floor plan
            if (zoneBounds.size.x <= zoneBounds.size.z)
                outward = new Vector3(Mathf.Sign(zoneBounds.center.x - interior.x), 0f, 0f);
            else
                outward = new Vector3(0f, 0f, Mathf.Sign(zoneBounds.center.z - interior.z));
            Vector3 spawn = currentWindow.position + outward * spawnDistance;
            spawn.y = floorY;
            figure.transform.position = spawn;
            figure.facePlayer = false;   // it walks its own line, it does not track you
            figure.transform.rotation = Quaternion.LookRotation(-outward);
            figure.Show(true);
        }
        else if (shown && nearestDistance > releaseDistance)
        {
            Hide();
            return;
        }

        if (shown && currentWindow != null)
        {
            Vector3 target = currentWindow.position + outward * stopDistance;
            target.y = floorY;
            Vector3 pos = Vector3.MoveTowards(figure.transform.position, target, approachSpeed * Time.deltaTime);
            figure.transform.position = pos;
            // slow drunken sway sells the walk on a static mesh
            float sway = Mathf.Sin(Time.time * 1.7f) * swayDegrees;
            figure.transform.rotation = Quaternion.LookRotation(-outward) * Quaternion.Euler(0f, 0f, sway);
        }
    }

    private void Hide()
    {
        if (shown || (figure != null && figure.gameObject.activeSelf))
        {
            shown = false;
            currentWindow = null;
            if (figure != null) figure.Show(false);
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
