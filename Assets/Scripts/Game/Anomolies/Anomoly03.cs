using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

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

    [SerializeField]
    public List<Transform> Anchor;

    [Tooltip("Degrees of slow sway for the hanging body.")]
    public float swayDegrees = 1.2f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.AddUp;

    private bool hangedActive;
    private bool hangedPoseCaptured;
    private Quaternion hangedBaseRotation;

    public void Evaluate()
    {
        // AddUp bookkeeping instances get Restore without Evaluate; an unwired Anchor
        // list must not throw the whole round away.
        if (Anchor == null || Anchor.Count == 0) return;

        Transform player = GameManager.Instance != null ? GameManager.Instance.GetPlayerTransform() : null;
        bool isAbove = player == null || transform.position.y > player.position.y;

        if (isAbove)
        {
            if (hangedFigure == null) return;
            hangedActive = true;
            hangedPoseCaptured = true;
            hangedBaseRotation = hangedFigure.transform.rotation;
            hangedFigure.transform.position = Anchor[Random.Range(0, Anchor.Count)].transform.position;
            hangedFigure.facePlayer = true;
            hangedFigure.Show(true);
        }
        else
        {
            if (figure == null) return;

            // One pick for both position and rotation, so the figure is not placed at
            // one anchor while borrowing another anchor's facing.
            Transform anchor = Anchor[Random.Range(0, Anchor.Count)];
            figure.transform.position = anchor.position;
            figure.transform.rotation = anchor.rotation;
            figure.facePlayer = true;
            figure.Show(true);
        }
    }

    public void Restore()
    {
        hangedActive = false;
        if (figure != null) figure.Show(false);
        if (hangedFigure != null)
        {
            // Only restore a rotation we actually captured - comparing a quaternion to
            // default never works (Unity's == is a dot-product check), and assigning
            // (0,0,0,0) corrupts the transform.
            if (hangedPoseCaptured) hangedFigure.transform.rotation = hangedBaseRotation;
            hangedFigure.Show(false);
        }
    }

    private void Update()
    {
        if (!hangedActive || hangedFigure == null) return;
        // While facePlayer is on, ShadowFigure owns the rotation; writing the sway too
        // would make two scripts fight over the same transform every frame.
        if (hangedFigure.facePlayer) return;
        float sway = Mathf.Sin(Time.time * 0.8f) * swayDegrees;
        hangedFigure.transform.rotation = hangedBaseRotation * Quaternion.Euler(sway, 0f, sway * 0.7f);
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
