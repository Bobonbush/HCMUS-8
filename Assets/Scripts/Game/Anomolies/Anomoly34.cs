using System.Collections.Generic;
using UnityEngine;

public class Anomoly34 : MonoBehaviour, Anomoly
{
    [Tooltip("Permanent school cameras. The anomaly changes their movement, not their visibility.")]
    public List<GameObject> extraCameras = new List<GameObject>();
    [Tooltip("Rotating heads, in the same order as extraCameras. Mounts remain fixed.")]
    public List<Transform> cameraHeads = new List<Transform>();
    public float followDegreesPerSecond = 24f;
    readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
    readonly Dictionary<Transform, Vector3> anchors = new Dictionary<Transform, Vector3>();
    bool active;

    void Awake() { ShowCameras(); }

    void ShowCameras()
    {
        foreach (var cameraObject in extraCameras)
            if (cameraObject != null) cameraObject.SetActive(true);
    }

    public void Evaluate()
    {
        if (active) return;
        ShowCameras();
        for (int i = 0; i < extraCameras.Count; i++)
        {
            if (extraCameras[i] == null) continue;
            var head = Head(i);
            if (!anchors.ContainsKey(head))
            {
                AnchorTrackingHead(head);
                anchors[head] = head.localPosition;
            }
            rest[head] = head.localRotation;
        }
        active = true;
    }

    Transform Head(int i) => i < cameraHeads.Count && cameraHeads[i] != null ? cameraHeads[i] : extraCameras[i].transform;

    // Rebase the pivot without moving the authored geometry. Only indexed housing
    // vertices count: the split mesh still contains unused bracket vertices.
    public static void AnchorTrackingHead(Transform head)
    {
        if (head == null || head.name != "TrackingHead") return;
        var filter = head.GetComponentInChildren<MeshFilter>(true);
        if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) return;
        var vertices = filter.sharedMesh.vertices;
        var indices = filter.sharedMesh.triangles;
        if (indices.Length == 0) return;
        var bounds = new Bounds(vertices[indices[0]], Vector3.zero);
        foreach (int index in indices) bounds.Encapsulate(vertices[index]);
        // The housing sits on the top of the bracket, at its lower central joint.
        var joint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 anchor = filter.transform.TransformPoint(joint);
        var children = new Transform[head.childCount];
        var positions = new Vector3[children.Length];
        for (int i = 0; i < children.Length; i++)
        {
            children[i] = head.GetChild(i);
            positions[i] = children[i].position;
        }
        head.position = anchor;
        for (int i = 0; i < children.Length; i++) children[i].position = positions[i];
    }

    void LateUpdate()
    {
        var eye = Camera.main;
        if (eye != null) TrackPlayer(eye.transform.position, Time.deltaTime);
    }

    void TrackPlayer(Vector3 position, float deltaTime)
    {
        if (!active) return;
        for (int i = 0; i < extraCameras.Count; i++)
        {
            if (extraCameras[i] == null) continue;
            var head = Head(i);
            // The elevator camera is a fixed reference, including any nested head.
            bool fixedCamera = false;
            for (var parent = head; parent != null; parent = parent.parent)
                if (parent.name == "Camera_Elevator") { fixedCamera = true; break; }
            if (fixedCamera || extraCameras[i].name == "Camera_Elevator") continue;
            if (anchors.TryGetValue(head, out var anchor)) head.localPosition = anchor;
            Vector3 direction = position - head.position;
            if (direction.sqrMagnitude < 0.001f) continue;
            // Imported CCTV lens points along local +X, not Unity's +Z.
            var target = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
            head.rotation = Quaternion.RotateTowards(head.rotation, target,
                Mathf.Max(0, followDegreesPerSecond) * deltaTime);
        }
    }

    public void Restore()
    {
        active = false;
        foreach (var item in rest)
            if (item.Key != null)
            {
                item.Key.localRotation = item.Value;
                if (anchors.TryGetValue(item.Key, out var anchor)) item.Key.localPosition = anchor;
            }
        rest.Clear();
        ShowCameras();
    }
    void OnDisable() { Restore(); }
    public Anomoly.EvaluateType getType() => Anomoly.EvaluateType.Single;
}
