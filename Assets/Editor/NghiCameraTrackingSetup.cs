using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NghiCameraTrackingSetup
{
    static NghiCameraTrackingSetup() { EditorApplication.delayCall += Request; }
    static void Request()
    {
        if (!File.Exists("Temp/NghiCameraTracking.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Temp/NghiCameraTracking.request");
        try { Install(); }
        catch (Exception e) { File.WriteAllText("Temp/NghiCameraTrackingReport.txt", "FAIL: " + e); }
    }

    [MenuItem("Tools/Make CCTV Permanent With Anomaly Tracking")]
    public static void Install()
    {
        CheckTracking();
        var floor = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Floor.prefab");
        int count = 0;
        try
        {
            var anomaly = floor.GetComponentInChildren<Anomoly34>(true);
            // Include the original school CCTV as well as the former bonus cameras.
            foreach (var root in floor.GetComponentsInChildren<Transform>(true))
            {
                if (root.name == "TrackingHead") continue;
                if (root.Find("OnlineCCTVModel") == null && root.Find("TrackingHead/OnlineCCTVModel") == null) continue;
                if (!anomaly.extraCameras.Contains(root.gameObject)) anomaly.extraCameras.Add(root.gameObject);
            }
            NghiAnomalyRefinement.Cameras(floor);
            CheckMountedHeads(anomaly);
            foreach (var camera in anomaly.extraCameras)
            {
                if (camera == null) continue;
                camera.SetActive(true);
                count++;
            }
            PrefabUtility.SaveAsPrefabAsset(floor, "Assets/Prefabs/Floor.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(floor); }
        File.WriteAllText("Temp/NghiCameraTrackingReport.txt", "PASS: " + count + " cameras: housing pivots remain attached through distant tracking in all directions; mounts stay fixed; rotations restore; elevator fixed.");
    }

    static void CheckMountedHeads(Anomoly34 anomaly)
    {
        var heads = anomaly.cameraHeads.FindAll(h => h != null);
        var positions = heads.ConvertAll(h => h.position);
        var rotations = heads.ConvertAll(h => h.rotation);
        var mounts = new System.Collections.Generic.List<Transform>();
        foreach (var camera in anomaly.extraCameras)
            if (camera != null && camera.transform.Find("StationaryMount") != null)
                mounts.Add(camera.transform.Find("StationaryMount"));
        var mountPositions = mounts.ConvertAll(m => m.position);
        var mountRotations = mounts.ConvertAll(m => m.rotation);
        var update = typeof(Anomoly34).GetMethod("TrackPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
        anomaly.Evaluate();
        for (int step = 0; step < 36; step++)
        {
            float angle = step * Mathf.PI / 18;
            update.Invoke(anomaly, new object[] { new Vector3(Mathf.Cos(angle) * 10000, 2000, Mathf.Sin(angle) * 10000), 1f });
            for (int i = 0; i < heads.Count; i++)
                Require(Vector3.Distance(heads[i].position, positions[i]) < .0001f, "Housing pivot detached: " + heads[i].parent.name);
            for (int i = 0; i < mounts.Count; i++)
                Require(Vector3.Distance(mounts[i].position, mountPositions[i]) < .0001f && Quaternion.Angle(mounts[i].rotation, mountRotations[i]) < .01f, "Mount moved");
        }
        anomaly.Restore();
        for (int i = 0; i < heads.Count; i++)
            Require(Quaternion.Angle(heads[i].rotation, rotations[i]) < .01f, "Housing rotation did not restore");
    }

    public static void CheckTracking()
    {
        var host = new GameObject("Camera tracking regression");
        try
        {
            var anomaly = host.AddComponent<Anomoly34>();
            var camera = new GameObject("School camera");
            camera.transform.SetParent(host.transform);
            var elevator = new GameObject("Camera_Elevator");
            elevator.transform.SetParent(host.transform);
            var elevatorHead = new GameObject("TrackingHead").transform;
            elevatorHead.SetParent(elevator.transform);
            anomaly.extraCameras.Add(camera);
            anomaly.cameraHeads.Add(camera.transform);
            anomaly.extraCameras.Add(elevator);
            anomaly.cameraHeads.Add(elevatorHead);
            var update = typeof(Anomoly34).GetMethod("TrackPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            // A distant player still drives tracking; no proximity/visibility gate.
            var args = new object[] { new Vector3(0, 0, 10000), 1f };
            update.Invoke(anomaly, args);
            Require(Quaternion.Angle(camera.transform.rotation, Quaternion.identity) < .01f, "Normal camera moved");
            anomaly.Evaluate();
            update.Invoke(anomaly, args);
            Require(Quaternion.Angle(camera.transform.rotation, Quaternion.identity) > 1, "Active camera did not track");
            Require(Quaternion.Angle(elevatorHead.rotation, Quaternion.identity) < .01f, "Elevator tracked");
            anomaly.Restore();
            update.Invoke(anomaly, args);
            Require(Quaternion.Angle(camera.transform.rotation, Quaternion.identity) < .01f, "Camera failed to restore");
            Require(camera.activeSelf && elevator.activeSelf, "Restore hid a camera");
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
