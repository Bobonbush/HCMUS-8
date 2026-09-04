using System.Collections.Generic;
using UnityEngine;

public class Anomoly35 : MonoBehaviour, Anomoly
{
    public List<GameObject> classroomFaces = new List<GameObject>();
    public Transform doorway;
    [Tooltip("Points into the closed classroom from the door threshold.")]
    public Transform roomAnchor;
    public GameObject physicsRoom;
    public Vector3 roomSize = new Vector3(3.6f, 3f, 4f);
    public float turnSpeed = 65f;
    public float dropInterval = 2.4f;
    public int maxFallenFaces = 12;
    readonly List<GameObject> fallen = new List<GameObject>();
    readonly List<float> droppedAt = new List<float>();
    readonly Dictionary<Transform, Quaternion> rotations = new Dictionary<Transform, Quaternion>();
    bool active;
    float nextDrop;
    public void Evaluate()
    {
        if (active) return;
        active = true; nextDrop = Time.time + 1.5f;
        if (physicsRoom != null) physicsRoom.SetActive(true);
        for (int i = 0; i < classroomFaces.Count; i++)
        {
            var face = classroomFaces[i]; if (face == null) continue;
            if (face.GetComponent<AnomalyFaceVariation>() == null) AnomalyFaceVariation.Apply(face, i);
            rotations[face.transform] = face.transform.rotation; face.SetActive(true);
        }
    }
    void Update()
    {
        if (!active) return;
        var camera = Camera.main;
        if (camera != null)
        {
            foreach (var face in classroomFaces) if (face != null) Look(face.transform, camera.transform.position);
            for (int i = 0; i < fallen.Count; i++)
            {
                var rb = fallen[i].GetComponent<Rigidbody>();
                // Some heads settle at the threshold and lift their gaze; others keep tumbling.
                if (i % 3 == 0 && Time.time - droppedAt[i] > 4.5f && roomAnchor != null)
                {
                    Vector3 p = roomAnchor.InverseTransformPoint(rb.position);
                    if (p.z < 0.8f && p.y < 0.5f)
                    { rb.isKinematic = true; Look(rb.transform, camera.transform.position); }
                }
            }
        }
        if (Application.isPlaying && roomAnchor != null && classroomFaces.Count > 0
            && fallen.Count < maxFallenFaces && Time.time >= nextDrop)
        { Drop(); nextDrop = Time.time + dropInterval * (0.75f + (fallen.Count % 3) * 0.23f); }
    }
    void Look(Transform face, Vector3 target)
    {
        var direction = target - face.position;
        if (direction.sqrMagnitude > 0.001f) face.rotation = Quaternion.RotateTowards(face.rotation,
            Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
    }
    void Drop()
    {
        var source = classroomFaces[fallen.Count % classroomFaces.Count];
        if (source == null) return;
        int i = fallen.Count;
        var face = Instantiate(source, transform);
        face.name = "FallenFace_" + i;
        // Every third head begins near the door; the others scatter deeper in the room.
        float depth = i % 3 == 0 ? 0.65f : 1.1f + (i * 31 % 100) / 100f * (roomSize.z - 1.4f);
        face.transform.position = roomAnchor.TransformPoint(new Vector3((i * 47 % 100 / 100f - 0.5f) * (roomSize.x - 0.8f), roomSize.y - 0.5f, depth));
        foreach (var old in face.GetComponentsInChildren<Collider>()) Destroy(old);
        var collider = face.AddComponent<SphereCollider>();
        var bounds = new Bounds(face.transform.position, Vector3.zero);
        foreach (var renderer in face.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
        collider.center = face.transform.InverseTransformPoint(bounds.center);
        collider.radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) / Mathf.Max(0.001f, Mathf.Abs(face.transform.lossyScale.x));
        var body = face.AddComponent<Rigidbody>();
        body.mass = 1.3f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.angularVelocity = new Vector3(2 + i % 3, 1.5f, -2.3f);
        body.linearVelocity = roomAnchor.TransformDirection(new Vector3(0.1f, -0.2f, i % 3 == 0 ? -0.3f : 0.4f));
        face.SetActive(true); fallen.Add(face); droppedAt.Add(Time.time);
    }
    public void Restore()
    {
        active = false;
        if (physicsRoom != null) physicsRoom.SetActive(false);
        foreach (var face in classroomFaces) if (face != null) face.SetActive(false);
        foreach (var pair in rotations) if (pair.Key != null) pair.Key.rotation = pair.Value;
        rotations.Clear();
        foreach (var face in fallen) if (face != null) { face.SetActive(false); Destroy(face); }
        fallen.Clear(); droppedAt.Clear();
    }
    void OnDisable() { Restore(); }
    public Anomoly.EvaluateType getType() => Anomoly.EvaluateType.Single;
}
