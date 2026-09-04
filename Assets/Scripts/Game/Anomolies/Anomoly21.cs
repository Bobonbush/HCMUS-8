using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// A delayed, shallow leak. This anomaly never submerges or damages the player.
public class Anomoly21 : MonoBehaviour, Anomoly
{
    public List<Transform> waterSurfaces = new List<Transform>();
    [Range(0, 0.04f)] public float riseHeight = 0.035f;
    [Min(1)] public float riseDuration = 240f;
    [Min(0)] public float onsetDelay = 18f;
    [Min(0)] public float spreadSpeed = 0.028f;
    [Min(0.1f)] public float maxSpreadRadius = 4.5f;
    [Range(0, 0.002f)] public float rippleHeight = 0.0015f;
    public List<ParticleSystem> toiletJets = new List<ParticleSystem>();
    public AudioSource waterRoar;
    readonly List<Vector3> startPositions = new List<Vector3>();
    MaterialPropertyBlock waterProperties;
    bool active;
    float elapsed;
    Camera view;
    UniversalAdditionalCameraData cameraData;
    CameraOverrideOption originalOpaque;

    public void Evaluate()
    {
        if (active) return;
        CaptureStartPositions(); elapsed = 0; active = true;
        for (int i = 0; i < waterSurfaces.Count; i++) if (waterSurfaces[i] != null)
        { waterSurfaces[i].localPosition = startPositions[i]; waterSurfaces[i].gameObject.SetActive(false); }
    }
    void AttachView(Camera camera)
    {
        DetachView(); view = camera;
        if (view == null) return;
        cameraData = view.GetUniversalAdditionalCameraData();
        originalOpaque = cameraData.requiresColorOption;
        cameraData.requiresColorOption = CameraOverrideOption.On;
    }
    void LateUpdate()
    {
        if (!active) return;
        elapsed += Time.deltaTime;
        float leakTime = Mathf.Max(0, elapsed - onsetDelay);
        bool leaking = elapsed > onsetDelay;
        float radius = Mathf.Min(maxSpreadRadius, 0.08f + leakTime * spreadSpeed);
        // Hard cap also protects older scene overrides from the previous full-height flood.
        float rise = Mathf.Clamp(riseHeight, 0, 0.04f) * Mathf.Clamp01(leakTime / Mathf.Max(1, riseDuration));
        if (leaking && Application.isPlaying)
        {
            if (Camera.main != view) AttachView(Camera.main);
            if (toiletJets.Count > 0 && toiletJets[0] != null && !toiletJets[0].gameObject.activeSelf)
            { toiletJets[0].gameObject.SetActive(true); toiletJets[0].Play(); }
            if (waterRoar != null && !waterRoar.isPlaying) waterRoar.Play();
        }
        for (int i = 0; i < waterSurfaces.Count; i++)
        {
            var surface = waterSurfaces[i]; if (surface == null) continue;
            surface.position = surface.parent.TransformPoint(startPositions[i]) + Vector3.up * rise;
            var renderer = surface.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (waterProperties == null) waterProperties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(waterProperties);
                var source = toiletJets.Count > 0 && toiletJets[0] != null ? toiletJets[0].transform.position : surface.position;
                waterProperties.SetVector("_FloodSource", source);
                waterProperties.SetFloat("_FloodRadius", leaking ? radius : 0);
                waterProperties.SetFloat("_FloodDepth", rise);
                waterProperties.SetFloat("_WaveHeight", Mathf.Clamp(rippleHeight, 0, 0.002f));
                waterProperties.SetFloat("_FlowSpeed", 0.22f);
                waterProperties.SetFloat("_FoamStrength", 0.025f);
                renderer.SetPropertyBlock(waterProperties);
            }
            surface.gameObject.SetActive(leaking);
        }
    }
    public void Restore()
    {
        active = false; elapsed = 0; CaptureStartPositions();
        for (int i = 0; i < waterSurfaces.Count; i++) if (waterSurfaces[i] != null)
        { waterSurfaces[i].localPosition = startPositions[i]; waterSurfaces[i].gameObject.SetActive(false); }
        foreach (var jet in toiletJets) if (jet != null)
        { jet.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); jet.gameObject.SetActive(false); }
        if (waterRoar != null) waterRoar.Stop();
        DetachView();
    }
    void DetachView()
    {
        if (cameraData != null) cameraData.requiresColorOption = originalOpaque;
        view = null; cameraData = null;
    }
    void OnDisable() { Restore(); }
    void CaptureStartPositions()
    {
        if (startPositions.Count == waterSurfaces.Count) return;
        startPositions.Clear();
        foreach (var surface in waterSurfaces) startPositions.Add(surface != null ? surface.localPosition : Vector3.zero);
    }
    public Anomoly.EvaluateType getType() => Anomoly.EvaluateType.Single;
}
