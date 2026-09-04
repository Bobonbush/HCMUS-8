using System.Collections.Generic;
using UnityEngine;

public class Anomoly34 : MonoBehaviour, Anomoly
{
    public List<GameObject> extraCameras = new List<GameObject>();
    [Tooltip("Rotating heads, in the same order as extraCameras. Mounts remain fixed.")]
    public List<Transform> cameraHeads = new List<Transform>();
    [Range(0.05f, 0.45f)] public float peripheralWidth = 0.2f;
    public float followDegreesPerSecond = 24f;
    public float avertDegreesPerSecond = 420f;
    public float returnDelay = 0.65f;
    readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
    readonly Dictionary<Transform, float> resume = new Dictionary<Transform, float>();
    bool active;
    public void Evaluate()
    {
        if (active) return;
        active = true;
        for (int i = 0; i < extraCameras.Count; i++)
        {
            if (extraCameras[i] == null) continue;
            var head = Head(i);
            rest[head] = head.localRotation;
            resume[head] = 0;
            extraCameras[i].SetActive(true);
        }
    }
    Transform Head(int i) => i < cameraHeads.Count && cameraHeads[i] != null ? cameraHeads[i] : extraCameras[i].transform;
    void LateUpdate()
    {
        var eye = Camera.main;
        if (!active || eye == null) return;
        for (int i = 0; i < extraCameras.Count; i++)
        {
            if (extraCameras[i] == null) continue;
            var head = Head(i);
            var view = eye.WorldToViewportPoint(head.position);
            bool visible = view.z > 0 && view.x >= 0 && view.x <= 1 && view.y >= 0 && view.y <= 1;
            bool central = visible && view.x > peripheralWidth && view.x < 1 - peripheralWidth;
            Quaternion target;
            if (central)
            {
                resume[head] = Time.time + returnDelay;
                target = (head.parent != null ? head.parent.rotation : Quaternion.identity) * rest[head]
                    * Quaternion.Euler(-18, view.x < 0.5f ? 85 : -85, 0);
            }
            else
            {
                if (Time.time < resume[head]) continue;
                Vector3 direction = eye.transform.position - head.position;
                if (direction.sqrMagnitude < 0.001f) continue;
                // Imported CCTV lens points along local +X, not Unity's +Z.
                target = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
            }
            head.rotation = Quaternion.RotateTowards(head.rotation, target,
                (central ? avertDegreesPerSecond : followDegreesPerSecond) * Time.deltaTime);
        }
    }
    public void Restore()
    {
        active = false;
        foreach (var item in rest) if (item.Key != null) item.Key.localRotation = item.Value;
        rest.Clear(); resume.Clear();
        foreach (var cameraObject in extraCameras) if (cameraObject != null) cameraObject.SetActive(false);
    }
    void OnDisable() { Restore(); }
    public Anomoly.EvaluateType getType() => Anomoly.EvaluateType.Single;
}
