using UnityEngine;

// Unity Quad faces local -Z. Keep the photograph upright and facing the viewer.
public class AnomalyBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null) return;
        var away = transform.position - camera.transform.position;
        away.y = 0;
        if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away, Vector3.up);
    }
}
