using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;

// Editor menu to start/stop a mock OpenXR session (stereo rendering without a headset)
// while in play mode. XR no longer auto-initialises (InitManagerOnStart is off), so
// normal desktop play and XR Device Simulator sessions are untouched unless you ask.
//
//   HCMUS/VR/Start Mock VR (stereo)  - initialise OpenXR with the Mock Runtime feature
//   HCMUS/VR/Stop Mock VR            - shut the session down again
public static class MockVRMenu
{
    [MenuItem("HCMUS/VR/Start Mock VR (stereo)", false, 10)]
    public static void StartMockVR()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("MockVR: enter Play mode first, then start Mock VR.");
            return;
        }
        var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        if (manager == null)
        {
            Debug.LogError("MockVR: no XR manager settings found.");
            return;
        }
        if (manager.activeLoader != null)
        {
            Debug.Log("MockVR: already running.");
            return;
        }
        manager.InitializeLoaderSync();
        if (manager.activeLoader == null)
        {
            Debug.LogError("MockVR: OpenXR loader failed to initialise.");
            return;
        }
        manager.StartSubsystems();
        Debug.Log("MockVR: stereo session started (device: " + UnityEngine.XR.XRSettings.loadedDeviceName + ").");
    }

    [MenuItem("HCMUS/VR/Stop Mock VR", false, 11)]
    public static void StopMockVR()
    {
        var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        if (manager == null || manager.activeLoader == null) return;
        manager.StopSubsystems();
        manager.DeinitializeLoader();
        Debug.Log("MockVR: session stopped.");
    }
}
