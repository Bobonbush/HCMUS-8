using System.Collections.Generic;
using UnityEngine;

// Every desk in one classroom has been dragged into the middle of the room and
// stacked in a heap, as though something needed the floor cleared. The edges of
// the room are bare.
//
// Desks fill a disc in a sunflower spiral, which packs a round area evenly instead
// of stacking them all on one spot. Once a layer is full the next desks go on top
// of it, so the pile grows upward instead of spreading across the floor. Each desk
// is spun a random amount so the heap reads as shoved rather than arranged.
public class Anomoly12 : MonoBehaviour, Anomoly
{

    [Tooltip("The desks of the target classroom, wired by the setup tooling.")]
    public List<Transform> seats = new List<Transform>();

    [Tooltip("Where the heap forms, in the classroom's local space. The middle of the desks.")]
    public Vector3 poolCentre;

    [Tooltip("Corner of the floor area the desks may occupy, in the classroom's local space.")]
    public Vector3 areaMin;

    [Tooltip("Opposite corner of that area.")]
    public Vector3 areaMax;

    [Tooltip("How wide the heap is allowed to spread before it starts stacking upward.")]
    public float poolRadius = 1.1f;

    [Tooltip("Spacing between desks within a layer. Smaller packs them tighter.")]
    public float spacing = 0.55f;

    [Tooltip("How high each stacked layer sits above the one below, in metres.")]
    public float layerHeight = 0.7f;

    [Tooltip("How far a desk may be spun as it is shoved onto the pile, in degrees.")]
    public float spinDegrees = 180f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private readonly List<Vector3> originalPositions = new List<Vector3>();
    private readonly List<Quaternion> originalRotations = new List<Quaternion>();

    // 137.5 degrees. Successive desks land in the gaps left by the previous ones,
    // so a layer fills at an even density instead of forming spokes.
    private const float GoldenAngle = 2.39996323f;

    public void Evaluate()
    {
        if (originalPositions.Count > 0) return;   // already piled, do not pile the pile

        // How many fit in one layer of the disc before the heap has to grow upward.
        float step = Mathf.Max(0.05f, spacing);
        int perLayer = Mathf.Max(1, Mathf.FloorToInt(Mathf.PI * poolRadius * poolRadius / (step * step)));

        // An unwired area would clamp every desk onto the same point and bury the lot
        // inside a wall, so a degenerate box means "do not clamp" rather than "clamp
        // to zero".
        bool clamp = areaMax.x > areaMin.x && areaMax.z > areaMin.z;

        for (int i = 0; i < seats.Count; i++)
        {
            Transform seat = seats[i];
            if (seat == null)
            {
                originalPositions.Add(Vector3.zero);
                originalRotations.Add(Quaternion.identity);
                continue;
            }
            originalPositions.Add(seat.localPosition);
            originalRotations.Add(seat.localRotation);

            int layer = i / perLayer;
            int inLayer = i % perLayer;

            float radius = Mathf.Min(step * Mathf.Sqrt(inLayer), poolRadius);
            float angle = inLayer * GoldenAngle;

            Vector3 spot;
            spot.x = poolCentre.x + radius * Mathf.Cos(angle);
            spot.z = poolCentre.z + radius * Mathf.Sin(angle);
            if (clamp)
            {
                spot.x = Mathf.Clamp(spot.x, areaMin.x, areaMax.x);
                spot.z = Mathf.Clamp(spot.z, areaMin.z, areaMax.z);
            }
            spot.y = seat.localPosition.y + layer * layerHeight;

            seat.localPosition = spot;
            seat.localRotation = seat.localRotation
                               * Quaternion.Euler(0f, Random.Range(-spinDegrees, spinDegrees), 0f);
        }
    }

    public void Restore()
    {
        for (int i = 0; i < seats.Count && i < originalPositions.Count; i++)
        {
            if (seats[i] == null) continue;
            seats[i].localPosition = originalPositions[i];
            seats[i].localRotation = originalRotations[i];
        }
        originalPositions.Clear();
        originalRotations.Clear();
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
