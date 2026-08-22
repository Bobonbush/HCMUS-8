using System.Collections.Generic;
using UnityEngine;

// A doppelganger follows right behind you the whole round. It walks in your
// footsteps - literally: it follows the trail of positions you walked through,
// a couple of metres back - so spinning around catches it standing there.
public class Anomoly05 : MonoBehaviour, Anomoly
{

    [Tooltip("The follower (the ghost). Hidden until Evaluate.")]
    public ShadowFigure figure;

    [Tooltip("How far behind along your own path it follows, in metres.")]
    public float followDistance = 2.6f;

    [Tooltip("Its movement speed. Slightly above player sprint speed so it never falls behind.")]
    public float moveSpeed = 6.5f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private bool active;
    private readonly List<Vector3> trail = new List<Vector3>();
    private const float SampleSpacing = 0.3f;
    private const int MaxSamples = 64;

    public void Evaluate()
    {
        active = true;
        trail.Clear();
    }

    public void Restore()
    {
        active = false;
        trail.Clear();
        if (figure != null) figure.Show(false);
    }

    private void Update()
    {
        if (!active || figure == null || GameManager.Instance == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null) return;

        // Only haunt a player on this floor (the host sits under the floor root).
        float floorY = transform.parent != null ? transform.parent.position.y : transform.position.y;
        if (Mathf.Abs(player.position.y - floorY) > 3f)
        {
            if (figure.gameObject.activeSelf) figure.Show(false);
            trail.Clear();
            return;
        }

        // Record the player's path.
        if (trail.Count == 0 || (player.position - trail[trail.Count - 1]).sqrMagnitude > SampleSpacing * SampleSpacing)
        {
            trail.Add(player.position);
            if (trail.Count > MaxSamples) trail.RemoveAt(0);
        }

        // Find the point on the trail followDistance behind the player (walking backwards).
        Vector3 target = trail[0];
        float distance = 0f;
        Vector3 previous = player.position;
        for (int i = trail.Count - 1; i >= 0; i--)
        {
            distance += Vector3.Distance(previous, trail[i]);
            previous = trail[i];
            if (distance >= followDistance) { target = trail[i]; break; }
        }

        // Until you have walked far enough there is nothing to stand on - stay hidden.
        if (distance < followDistance)
        {
            if (figure.gameObject.activeSelf) figure.Show(false);
            return;
        }

        if (!figure.gameObject.activeSelf)
        {
            figure.transform.position = target;
            figure.Show(true);
        }
        figure.transform.position = Vector3.MoveTowards(figure.transform.position, target, moveSpeed * Time.deltaTime);
        figure.facePlayer = true;   // it is always looking at you
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
