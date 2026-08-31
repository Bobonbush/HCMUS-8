using UnityEngine;

// Looking into the toilet mirror, something is looking back at you. The figure
// starts sunk into the wall behind the glass; when the player walks up to the
// mirror it slowly pushes out through the surface until it sits pressed against
// the glass, framed by the mirror.
public class Anomoly06 : MonoBehaviour, Anomoly
{

    [Tooltip("The figure inside the mirror. Hidden until Evaluate.")]
    public ShadowFigure figure;

    [Header("Emergence")]
    [Tooltip("Player distance to the mirror that triggers the emergence.")]
    public float revealDistance = 2.4f;
    [Tooltip("Seconds for the figure to slide fully out of the mirror.")]
    public float emergeDuration = 4f;
    [Tooltip("Offset (in the figure parent's space) the figure hides at before emerging - just far enough behind the glass to be occluded. Keep it shallow: everything deeper than the glass is invisible travel. Set by the wiring tooling.")]
    public Vector3 emergeOffset = new Vector3(0f, 0f, 0.08f);

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private bool active;
    private bool emerging;      // latched once the player has come close enough
    private float progress;     // 0 = hidden inside the wall, 1 = pressed against the glass
    private Vector3 shownPos;   // authored pose = fully emerged
    private bool shownPosCaptured;

    public void Evaluate()
    {
        if (figure == null) return;
        CaptureShownPos();
        active = true;
        emerging = false;
        progress = 0f;
        // Visible but parked behind the glass; the wall and mirror backing occlude it,
        // so nothing shows until the emergence slides it through the surface.
        figure.transform.localPosition = shownPos + emergeOffset;
        figure.Show(true);
    }

    public void Restore()
    {
        active = false;
        emerging = false;
        progress = 0f;
        if (figure == null) return;
        CaptureShownPos();
        figure.transform.localPosition = shownPos;
        figure.Show(false);
    }

    private void CaptureShownPos()
    {
        if (shownPosCaptured) return;
        shownPos = figure.transform.localPosition;
        shownPosCaptured = true;
    }

    private void Update()
    {
        // 12 floor instances tick this every frame - bail before any real work.
        if (!active || figure == null) return;

        if (!emerging)
        {
            if (GameManager.Instance == null) return;
            Transform player = GameManager.Instance.GetPlayerTransform();
            if (player == null) return;
            if (Vector3.Distance(player.position, figure.transform.position) > revealDistance) return;
            emerging = true;
        }

        if (progress >= 1f) return;
        progress = Mathf.Min(1f, progress + Time.deltaTime / Mathf.Max(0.1f, emergeDuration));
        float eased = progress * progress * (3f - 2f * progress);   // smoothstep: slow start, slow settle

        Vector3 pos = shownPos + emergeOffset * (1f - eased);
        // A faint shudder while it pushes through the glass, gone once it is out.
        pos.x += Mathf.Sin(Time.time * 27f) * 0.004f * Mathf.Sin(progress * Mathf.PI);
        figure.transform.localPosition = pos;
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
