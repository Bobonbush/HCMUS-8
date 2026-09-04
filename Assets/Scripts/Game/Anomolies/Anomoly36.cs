using System.Collections.Generic;
using UnityEngine;

// The eyes on the conference poster follow you down the corridor.
//
// The poster itself is printed with empty whites; the irises are two small quads
// sitting a few millimetres proud of the paper, so they can slide inside the lens
// instead of the whole sheet being swapped for a differently-drawn one. That buys
// continuous tracking rather than three or four canned gaze directions, and makes
// Restore() nothing more than putting them back in the middle.
//
// Normal floors have the same two quads sitting dead centre, so the board looks
// no different until this runs.
public class Anomoly36 : MonoBehaviour, Anomoly
{

    [Tooltip("The iris quads, wired by the setup tooling. Their rest pose is wherever they sit when Evaluate runs.")]
    public List<Transform> eyes = new List<Transform>();

    [Tooltip("How far an iris may slide across the eye, in metres. Sized to the lens it sits in.")]
    public float travelX = 0.052f;

    [Tooltip("How far an iris may slide up or down. The lens is thin, so there is very little room here.")]
    public float travelY = 0.0037f;

    [Tooltip("How quickly the gaze catches up. Lower drags further behind you.")]
    public float followSpeed = 7f;

    [Tooltip("Past this distance the eyes ease back to centre rather than tracking someone who cannot see them.")]
    public float maxDistance = 14f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private readonly List<Vector3> restPositions = new List<Vector3>();
    private bool active;

    public void Evaluate()
    {
        if (restPositions.Count > 0) return;   // already watching

        for (int i = 0; i < eyes.Count; i++)
            restPositions.Add(eyes[i] != null ? eyes[i].position : Vector3.zero);
        active = true;
    }

    public void Restore()
    {
        // Disarm first, or an eye can still be dragged towards the player by a frame
        // that lands between the write-back and the flag.
        active = false;
        for (int i = 0; i < eyes.Count && i < restPositions.Count; i++)
        {
            if (eyes[i] == null) continue;
            eyes[i].position = restPositions[i];
        }
        restPositions.Clear();
    }

    private void Update()
    {
        // All 12 floors tick this every frame - bail before any real work.
        if (!active) return;

        if (GameManager.Instance == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null) return;

        float floorY = transform.parent != null ? transform.parent.position.y : transform.position.y;
        if (Mathf.Abs(player.position.y - floorY) > 3f) return;

        // The camera, not the player root, so the eyes meet you at your own height
        // rather than staring at your feet.
        Camera cam = Camera.main;
        Vector3 target = cam != null ? cam.transform.position : player.position;

        // Framerate-independent smoothing: the gaze lags a little behind you, which
        // is what sells it as something looking rather than something snapping.
        float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        for (int i = 0; i < eyes.Count && i < restPositions.Count; i++)
        {
            Transform eye = eyes[i];
            if (eye == null) continue;
            Vector3 rest = restPositions[i];

            Vector3 want = rest;
            Vector3 toPlayer = target - rest;
            if (toPlayer.magnitude <= maxDistance)
            {
                // The iris quad shares the poster's rotation, so its own axes are the
                // poster's: right runs across the sheet, forward is the face normal.
                float side = Vector3.Dot(toPlayer, eye.right);
                float rise = Vector3.Dot(toPlayer, eye.up);
                float depth = Mathf.Max(0.25f, Vector3.Dot(toPlayer, eye.forward));

                // side/depth is the tangent of the angle off the normal, so the eyes
                // reach full deflection at 45 degrees and simply hold there. Using the
                // raw direction instead would make them flip as you cross the plane of
                // the board.
                want += eye.right * Mathf.Clamp(side / depth, -1f, 1f) * travelX
                      + eye.up * Mathf.Clamp(rise / depth, -1f, 1f) * travelY;
            }

            eye.position = Vector3.Lerp(eye.position, want, k);
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
