using UnityEngine;

// An exam is in progress. On one desk sits the exam paper and some sheets - and a
// pen hovers over the paper, moving by itself as if an unseen hand is scribbling
// answers. Normally the desk is empty; when the anomaly is active the papers and
// the writing pen appear.
//
// The rig (papers + exam sheet + pen holder) is prebuilt and hidden by the wiring
// tooling. Only the pen holder moves: it sweeps across the exam sheet in a writing
// serpentine while the pen, parented under it tip-down, jitters like it is forming
// letters.
public class Anomoly09 : MonoBehaviour, Anomoly
{
    [Tooltip("Parent of the papers, exam sheet and pen. Hidden until the anomaly is active.")]
    public GameObject rig;

    [Tooltip("The pen holder that sweeps across the paper (pen is its child, tip down).")]
    public Transform penHolder;

    [Tooltip("The exam sheet whose local plane the pen writes over (X across, Z down the page).")]
    public Transform paperSurface;

    [Tooltip("How high the pen hovers above the paper, in metres.")]
    public float hoverHeight = 0.012f;

    [Tooltip("Writing region on the sheet (width across X, length down Z), in metres.")]
    public Vector2 writeArea = new Vector2(0.15f, 0.20f);

    [Tooltip("How fast the pen sweeps left-right (sweeps per second).")]
    public float sweepSpeed = 0.7f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private bool active;
    private float t;
    private bool captured;
    private Vector3 holderRestPos;
    private Quaternion holderRestRot;

    public void Evaluate()
    {
        Capture();
        active = true;
        t = 0f;
        if (rig != null) rig.SetActive(true);
    }

    public void Restore()
    {
        active = false;
        if (rig != null) rig.SetActive(false);
        if (captured && penHolder != null)
        {
            penHolder.localPosition = holderRestPos;
            penHolder.localRotation = holderRestRot;
        }
    }

    private void Capture()
    {
        if (captured || penHolder == null) return;
        holderRestPos = penHolder.localPosition;
        holderRestRot = penHolder.localRotation;
        captured = true;
    }

    private void Update()
    {
        // 12 floor instances tick this every frame - bail unless this one is live.
        if (!active || penHolder == null || paperSurface == null) return;
        t += Time.deltaTime;

        // Serpentine: sweep left-right, drift slowly down the page, restart at the top.
        float u = Mathf.PingPong(t * sweepSpeed, 1f);          // 0..1 across
        float v = Mathf.Repeat(t * 0.11f, 1f);                 // 0..1 down the page
        // Fine jitter so the strokes read as handwriting, not a smooth slide.
        float jitterX = Mathf.Sin(t * 34f) * 0.006f;
        float jitterZ = Mathf.Sin(t * 47f) * 0.004f;

        Vector3 local = new Vector3(
            (u - 0.5f) * writeArea.x + jitterX,
            hoverHeight,
            (0.5f - v) * writeArea.y + jitterZ);

        penHolder.position = paperSurface.TransformPoint(local);
        // Slight tilt wobble as if pressing out letters.
        penHolder.rotation = paperSurface.rotation
            * Quaternion.Euler(Mathf.Sin(t * 22f) * 5f, Mathf.Sin(t * 2.3f) * 12f, Mathf.Cos(t * 19f) * 5f);
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
