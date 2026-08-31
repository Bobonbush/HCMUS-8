using System.Collections.Generic;
using UnityEngine;

// The corridor props are growing. Not a pop - a slow, eased swell over most of a
// minute, so the bin and the extinguisher are only slightly off when you walk in
// and plainly wrong by the time you walk back. You end up doubting your memory of
// how big they were, which is the whole trick.
public class Anomoly18 : MonoBehaviour, Anomoly
{

    [Tooltip("The corridor props that swell, wired by the setup tooling.")]
    public List<Transform> props = new List<Transform>();

    [Tooltip("Final size multiplier at the end of the growth.")]
    public float targetScale = 1.6f;

    [Tooltip("Seconds to reach the final size.")]
    public float growDuration = 45f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private readonly List<Vector3> originals = new List<Vector3>();
    private bool active;
    private float elapsed;

    public void Evaluate()
    {
        if (originals.Count > 0) return;   // already growing, do not restart from the swollen size
        foreach (Transform prop in props)
        {
            originals.Add(prop != null ? prop.localScale : Vector3.zero);
        }
        elapsed = 0f;
        active = true;
    }

    public void Restore()
    {
        // Stop the growth before writing the sizes back, or Update puts them straight back.
        active = false;
        for (int i = 0; i < props.Count && i < originals.Count; i++)
        {
            if (props[i] != null) props[i].localScale = originals[i];
        }
        originals.Clear();
    }

    private void Update()
    {
        if (!active) return;

        // Every floor runs this Update; only the one the player stands on should act.
        float floorY = transform.parent != null ? transform.parent.position.y : transform.position.y;
        if (GameManager.Instance == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null || Mathf.Abs(player.position.y - floorY) > 3f) return;

        elapsed += Time.deltaTime;
        // SmoothStep eases in, so the first seconds are almost imperceptible.
        float k = Mathf.SmoothStep(1f, targetScale, Mathf.Clamp01(elapsed / growDuration));
        for (int i = 0; i < props.Count && i < originals.Count; i++)
        {
            if (props[i] != null) props[i].localScale = originals[i] * k;
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
