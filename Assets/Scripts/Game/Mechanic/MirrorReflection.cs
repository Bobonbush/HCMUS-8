using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Real planar reflection for a wall mirror (URP). A hidden camera mirrors the main
// camera across the glass plane and renders into a RenderTexture that the
// HCMUS/MirrorPlanar shader samples in screen space.
//
// Cost and stability control:
//  - only ONE mirror may be live at a time (the nearest candidate), because every
//    live reflection is a full extra scene render;
//  - candidates must be on the player's floor, within activeDistance, in front of
//    the glass;
//  - the reflection camera renders with shadows and post-processing forced OFF,
//    re-forced after every CopyFrom: URP's additional-lights shadow pass asserts
//    (and once took the whole editor down) when extra cameras render it against
//    this map's many spotlights.
//
// Note: the reflection is computed from the main camera, so in VR it renders
// mono - acceptable for now, revisit if mirrors matter up close in VR.
public class MirrorReflection : MonoBehaviour
{
    [Tooltip("The glass surface renderer (gets the MirrorPlanar material).")]
    public Renderer glassRenderer;
    [Tooltip("Transform whose forward (+z) points out of the glass toward the room.")]
    public Transform normalSource;
    [Tooltip("Player distance beyond which the reflection turns off.")]
    public float activeDistance = 9f;
    [Tooltip("Vertical resolution of the reflection texture.")]
    public int textureHeight = 512;
    [Tooltip("Nudge along the normal to avoid clipping exactly at the surface.")]
    public float clipPlaneOffset = 0.01f;

    Camera _refCam;
    RenderTexture _rt;
    Material _mat;
    bool _live;

    static readonly int ReflectionTexId = Shader.PropertyToID("_ReflectionTex");
    static readonly int HasReflectionId = Shader.PropertyToID("_HasReflection");

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
        SetLive(false);
    }

    void OnDestroy()
    {
        if (_rt != null) { _rt.Release(); Destroy(_rt); }
        if (_refCam != null) Destroy(_refCam.gameObject);
    }

    void LateUpdate()
    {
        Camera main = Camera.main;
        if (main == null || glassRenderer == null)
        {
            SetLive(false);
            return;
        }

        Transform source = normalSource != null ? normalSource : transform;
        Vector3 normal = source.forward;
        Bounds b = glassRenderer.bounds;
        // The bounds centre sits inside the glass slab; push out to the surface.
        float surfaceExtent = Mathf.Abs(b.extents.x * normal.x) + Mathf.Abs(b.extents.y * normal.y) + Mathf.Abs(b.extents.z * normal.z);
        Vector3 planePoint = b.center + normal * surfaceExtent;

        Vector3 toCamera = main.transform.position - planePoint;
        // Same floor only: mirrors one floor up/down are within 3D range of the
        // stacked floor loop but can never be seen - keep their cameras off.
        // On top of that, only the single nearest mirror may run.
        bool shouldRun = Mathf.Abs(toCamera.y) < 2.5f
            && toCamera.magnitude <= activeDistance
            && Vector3.Dot(toCamera, normal) > 0f
            && ClaimNearest(this, toCamera.sqrMagnitude);
        SetLive(shouldRun);
        if (!shouldRun) return;

        EnsureResources(main);
        // SetLive may have run before the camera/material existed on the first live
        // frame - assert the live state on the real objects every frame we run.
        _refCam.enabled = true;
        _mat.SetFloat(HasReflectionId, 1f);

        // Mirror the main camera across the glass plane.
        float d = -Vector3.Dot(normal, planePoint) - clipPlaneOffset;
        Vector4 plane = new Vector4(normal.x, normal.y, normal.z, d);
        Matrix4x4 reflection = CalculateReflectionMatrix(plane);

        _refCam.CopyFrom(main);
        _refCam.targetTexture = _rt;
        _refCam.depth = main.depth - 1f;
        _refCam.useOcclusionCulling = false;   // mirrored matrices confuse occlusion data
        // CopyFrom can drag URP per-camera settings along; shadows MUST stay off here -
        // see the class comment.
        UniversalAdditionalCameraData data = _refCam.GetComponent<UniversalAdditionalCameraData>();
        if (data == null) data = _refCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        data.renderShadows = false;
        _refCam.worldToCameraMatrix = main.worldToCameraMatrix * reflection;

        // Clip everything behind the glass so geometry inside the wall never leaks in.
        Vector4 clipPlane = CameraSpacePlane(_refCam.worldToCameraMatrix, planePoint, normal);
        _refCam.projectionMatrix = main.CalculateObliqueMatrix(clipPlane);

        // Keep the transform loosely in sync for anything that reads it.
        _refCam.transform.position = reflection.MultiplyPoint(main.transform.position);

        _mat.SetTexture(ReflectionTexId, _rt);
    }

    void EnsureResources(Camera main)
    {
        if (_mat == null)
        {
            _mat = glassRenderer.material;   // per-mirror instance: each glass has its own texture
        }
        if (_rt == null)
        {
            int height = Mathf.Max(128, textureHeight);
            int width = Mathf.Max(128, Mathf.RoundToInt(height * Mathf.Max(0.5f, main.aspect)));
            _rt = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR)
            {
                name = name + "_Reflection",
                useMipMap = false,
                autoGenerateMips = false,
            };
        }
        if (_refCam == null)
        {
            GameObject go = new GameObject(name + "_ReflectionCamera");
            go.hideFlags = HideFlags.HideAndDontSave;
            _refCam = go.AddComponent<Camera>();
            _refCam.enabled = false;
            UniversalAdditionalCameraData data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
        }
    }

    void SetLive(bool value)
    {
        if (_live == value) return;
        _live = value;
        if (_refCam != null) _refCam.enabled = value;
        if (_mat != null) _mat.SetFloat(HasReflectionId, value ? 1f : 0f);
    }

    void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        if (cam != _refCam) return;
        GL.invertCulling = true;                       // mirrored view flips winding order
        if (glassRenderer != null) glassRenderer.enabled = false;   // no recursion
    }

    void OnEndCamera(ScriptableRenderContext context, Camera cam)
    {
        if (cam != _refCam) return;
        GL.invertCulling = false;
        if (glassRenderer != null) glassRenderer.enabled = true;
    }

    // One live mirror at a time: candidates bid with their squared distance each
    // frame and only the closest keeps its camera on.
    static MirrorReflection _nearest;
    static float _nearestSqr = float.MaxValue;
    static int _bidFrame = -1;

    static bool ClaimNearest(MirrorReflection candidate, float sqrDistance)
    {
        if (Time.frameCount != _bidFrame)
        {
            _bidFrame = Time.frameCount;
            _nearest = candidate;
            _nearestSqr = sqrDistance;
            return true;
        }
        if (sqrDistance < _nearestSqr)
        {
            if (_nearest != null && _nearest != candidate) _nearest.SetLive(false);
            _nearest = candidate;
            _nearestSqr = sqrDistance;
            return true;
        }
        return _nearest == candidate;
    }

    static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = 1f - 2f * plane.x * plane.x;
        m.m01 = -2f * plane.x * plane.y;
        m.m02 = -2f * plane.x * plane.z;
        m.m03 = -2f * plane.w * plane.x;
        m.m10 = -2f * plane.y * plane.x;
        m.m11 = 1f - 2f * plane.y * plane.y;
        m.m12 = -2f * plane.y * plane.z;
        m.m13 = -2f * plane.w * plane.y;
        m.m20 = -2f * plane.z * plane.x;
        m.m21 = -2f * plane.z * plane.y;
        m.m22 = 1f - 2f * plane.z * plane.z;
        m.m23 = -2f * plane.w * plane.z;
        return m;
    }

    static Vector4 CameraSpacePlane(Matrix4x4 worldToCamera, Vector3 point, Vector3 normal)
    {
        Vector3 cPoint = worldToCamera.MultiplyPoint(point);
        Vector3 cNormal = worldToCamera.MultiplyVector(normal).normalized;
        return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPoint, cNormal));
    }
}
