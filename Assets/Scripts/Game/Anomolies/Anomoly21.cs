using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Anomoly21 : MonoBehaviour, Anomoly
{
    public List<Transform> waterSurfaces = new List<Transform>();
    public float riseHeight = 2.8f;
    public float riseDuration = 8f;
    public List<ParticleSystem> toiletJets = new List<ParticleSystem>();
    public AudioSource waterRoar;
    public Material underwaterMaterial;
    readonly List<Vector3> startPositions = new List<Vector3>();
    bool active;
    float elapsed;
    GameObject overlay;
    Camera view;
    UniversalAdditionalCameraData cameraData;
    CameraOverrideOption originalOpaque;
    AudioLowPassFilter muffler;
    bool ownedMuffler;
    bool originalFilterEnabled;
    float originalCutoff;
    readonly MaterialPropertyBlock waterProperties = new MaterialPropertyBlock();
    public void Evaluate()
    {
        if (active) return;
        CaptureStartPositions(); elapsed = 0; active = true;
        for (int i = 0; i < waterSurfaces.Count; i++) if (waterSurfaces[i] != null)
        { waterSurfaces[i].localPosition = startPositions[i]; waterSurfaces[i].gameObject.SetActive(true); }
        if (!Application.isPlaying) return;
        foreach (var jet in toiletJets) if (jet != null) { jet.gameObject.SetActive(true); jet.Play(); }
        if (waterRoar != null) waterRoar.Play();
        AttachView(Camera.main);
    }
    void AttachView(Camera camera)
    {
        DetachView(); view = camera;
        if (view == null || underwaterMaterial == null) return;
        cameraData = view.GetUniversalAdditionalCameraData();
        originalOpaque = cameraData.requiresColorOption;
        cameraData.requiresColorOption = CameraOverrideOption.On;
        overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlay.name = "Flood_UnderwaterRefraction";
        Destroy(overlay.GetComponent<Collider>());
        overlay.transform.SetParent(view.transform, false);
        overlay.GetComponent<Renderer>().sharedMaterial = underwaterMaterial;
        overlay.SetActive(false);
        var listener = view.GetComponent<AudioListener>();
        if (listener != null)
        {
            muffler = listener.GetComponent<AudioLowPassFilter>();
            ownedMuffler = muffler == null;
            if (ownedMuffler) muffler = listener.gameObject.AddComponent<AudioLowPassFilter>();
            originalFilterEnabled = muffler.enabled; originalCutoff = muffler.cutoffFrequency;
            if (ownedMuffler) muffler.enabled = false;
        }
    }
    void LateUpdate()
    {
        if (!active) return;
        elapsed += Time.deltaTime;
        float rise = riseHeight * Mathf.Clamp01(elapsed / Mathf.Max(0.01f, riseDuration));
        bool submerged = false;
        if (Application.isPlaying && Camera.main != view) AttachView(Camera.main);
        for (int i = 0; i < waterSurfaces.Count; i++)
        {
            var surface = waterSurfaces[i]; if (surface == null) continue;
            surface.localPosition = startPositions[i] + Vector3.up * rise;
            var renderer = surface.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.GetPropertyBlock(waterProperties);
                var source = toiletJets.Count > 0 && toiletJets[0] != null ? toiletJets[0].transform.position : surface.position;
                waterProperties.SetVector("_FloodSource", source);
                waterProperties.SetFloat("_FloodRadius", 0.5f + elapsed * 12f);
                renderer.SetPropertyBlock(waterProperties);
            }
            if (view == null) continue;
            var local = surface.InverseTransformPoint(view.transform.position);
            // Only immerse the player inside this floor's actual flooded footprint.
            submerged |= Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.z) <= 0.5f
                && view.transform.position.y < surface.position.y - 0.035f
                && view.transform.position.y > surface.parent.TransformPoint(startPositions[i]).y - 0.2f;
        }
        if (overlay != null)
        {
            overlay.SetActive(submerged);
            float distance = view.nearClipPlane + 0.025f;
            overlay.transform.localPosition = Vector3.forward * distance;
            float height = 2 * distance * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.2f;
            overlay.transform.localScale = new Vector3(height * view.aspect, height, 1);
        }
        if (muffler != null)
        { muffler.enabled = submerged || (!ownedMuffler && originalFilterEnabled); muffler.cutoffFrequency = submerged ? 650 : originalCutoff; }
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
        if (overlay != null) { overlay.SetActive(false); Destroy(overlay); }
        if (cameraData != null) cameraData.requiresColorOption = originalOpaque;
        if (muffler != null)
        {
            if (ownedMuffler) Destroy(muffler);
            else { muffler.enabled = originalFilterEnabled; muffler.cutoffFrequency = originalCutoff; }
        }
        overlay = null; view = null; cameraData = null; muffler = null;
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
