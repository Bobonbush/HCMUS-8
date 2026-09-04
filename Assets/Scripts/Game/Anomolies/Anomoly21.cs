using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A shallow flood slowly rises across the floor instead of appearing instantly.
public class Anomoly21 : MonoBehaviour, Anomoly
{
    public List<Transform> waterSurfaces = new List<Transform>();
    public float riseHeight = 0.32f;
    public float riseDuration = 12f;
    private readonly Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;
    private readonly List<Vector3> startPositions = new List<Vector3>();
    private Coroutine riseRoutine;

    public void Evaluate()
    {
        CaptureStartPositions();
        if (riseRoutine != null) StopCoroutine(riseRoutine);
        for (int i = 0; i < waterSurfaces.Count; i++)
        {
            Transform surface = waterSurfaces[i];
            if (surface == null) continue;
            surface.localPosition = startPositions[i];
            surface.gameObject.SetActive(true);
        }
        riseRoutine = StartCoroutine(Rise());
    }

    public void Restore()
    {
        if (riseRoutine != null)
        {
            StopCoroutine(riseRoutine);
            riseRoutine = null;
        }
        CaptureStartPositions();
        for (int i = 0; i < waterSurfaces.Count; i++)
        {
            Transform surface = waterSurfaces[i];
            if (surface == null) continue;
            surface.localPosition = startPositions[i];
            surface.gameObject.SetActive(false);
        }
    }

    private IEnumerator Rise()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, riseDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < waterSurfaces.Count; i++)
            {
                Transform surface = waterSurfaces[i];
                if (surface != null) surface.localPosition = startPositions[i] + Vector3.up * (riseHeight * t);
            }
            yield return null;
        }
        riseRoutine = null;
    }

    private void CaptureStartPositions()
    {
        if (startPositions.Count == waterSurfaces.Count) return;
        startPositions.Clear();
        foreach (Transform surface in waterSurfaces)
            startPositions.Add(surface != null ? surface.localPosition : Vector3.zero);
    }

    public Anomoly.EvaluateType getType() { return type; }
}
