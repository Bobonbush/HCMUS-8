using UnityEngine;

// A dark human silhouette built from primitives (Exit 8 style placeholder until we
// get a real character model). Several anomolies share it: the watcher on another
// floor (#3), the doppelganger behind the glass (#5), the guy in the corner (#14).
//
// The silhouette children are created in the Floor prefab by the setup tooling via
// Build(), so at runtime this script only handles visibility and facing the player.
public class ShadowFigure : MonoBehaviour
{
    [Tooltip("Rotate (yaw only) so the figure always faces the player.")]
    public bool facePlayer = false;

    public void Show(bool on)
    {
        gameObject.SetActive(on);
    }

    private void Update()
    {
        if (!facePlayer || GameManager.Instance == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(toPlayer),
            120f * Time.deltaTime);
    }

    // Creates the silhouette. Called from editor tooling when wiring the Floor prefab;
    // safe to call at runtime too. Existing children mean it is already built.
    public static ShadowFigure Build(GameObject host, Material material)
    {
        ShadowFigure figure = host.GetComponent<ShadowFigure>();
        if (figure == null) figure = host.AddComponent<ShadowFigure>();
        if (host.transform.childCount > 0) return figure;

        AddPart(host.transform, PrimitiveType.Capsule, new Vector3(-0.09f, 0.45f, 0f), new Vector3(0.14f, 0.45f, 0.14f), material); // left leg
        AddPart(host.transform, PrimitiveType.Capsule, new Vector3(0.09f, 0.45f, 0f), new Vector3(0.14f, 0.45f, 0.14f), material);  // right leg
        AddPart(host.transform, PrimitiveType.Capsule, new Vector3(0f, 1.12f, 0f), new Vector3(0.36f, 0.34f, 0.22f), material);     // torso
        AddPart(host.transform, PrimitiveType.Capsule, new Vector3(-0.26f, 1.1f, 0f), new Vector3(0.1f, 0.3f, 0.1f), material);     // left arm
        AddPart(host.transform, PrimitiveType.Capsule, new Vector3(0.26f, 1.1f, 0f), new Vector3(0.1f, 0.3f, 0.1f), material);      // right arm
        AddPart(host.transform, PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0f), new Vector3(0.24f, 0.26f, 0.24f), material);      // head
        return figure;
    }

    private static void AddPart(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        // The figure must never block or push the player.
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) DestroyImmediate(collider);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = pos;
        part.transform.localScale = scale;
        if (material != null) part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
