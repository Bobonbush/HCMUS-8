using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Scene Floor is the authoring copy; GameManager instantiates the shared prefab.
// Persist only camera transforms, including edits not yet saved to the scene.
[InitializeOnLoad]
public static class NghiCameraPlacement
{
    private const string FloorPath = "Assets/Prefabs/Floor.prefab";

    static NghiCameraPlacement()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;
        var floors = Resources.FindObjectsOfTypeAll<AnomolyManager>()
            .Where(m => m.gameObject.scene.IsValid() && m.gameObject.scene.isLoaded
                && m.name == "Floor" && IsFloorInstance(m.transform)).ToArray();
        // Never pick between multiple authoring copies or use the mirrored Floor (1).
        if (floors.Length == 0) return;
        if (floors.Length > 1)
        {
            Debug.LogWarning("Camera placements were not saved: multiple scene objects named Floor. Select your cameras and use Tools > Save Selected Camera Placements to Shared Floor Prefab.");
            return;
        }
        Save(CameraTransforms(floors[0].transform), InteractionMode.AutomatedAction);
    }

    [MenuItem("Tools/Save Selected Camera Placements to Shared Floor Prefab")]
    public static void SaveSelected()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before saving camera placements.");
        var transforms = new HashSet<Transform>();
        foreach (var selected in Selection.transforms)
        {
            // Selecting a camera's mesh child also saves its complete camera assembly.
            var root = selected;
            for (var ancestor = selected; ancestor != null; ancestor = ancestor.parent)
                if (IsCamera(ancestor)) { root = ancestor; break; }
            foreach (var cameraTransform in CameraTransforms(root)) transforms.Add(cameraTransform);
        }
        if (transforms.Count == 0)
            throw new InvalidOperationException("Select Floor, a camera group, or a security camera first.");
        Save(transforms, InteractionMode.UserAction);
    }

    private static bool IsCamera(Transform t)
    {
        return t.name.StartsWith("Camera_", StringComparison.Ordinal)
            || t.name.StartsWith("PermanentCamera_", StringComparison.Ordinal);
    }

    private static bool IsFloorInstance(Transform t)
    {
        var source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
        return source != null && AssetDatabase.GetAssetPath(source) == FloorPath
            && source.transform.parent == null;
    }

    private static IEnumerable<Transform> CameraTransforms(Transform root)
    {
        return root.GetComponentsInChildren<Transform>(true).Where(IsCamera)
            .SelectMany(t => t.GetComponentsInChildren<Transform>(true)).Distinct();
    }

    private static void Save(IEnumerable<Transform> candidates, InteractionMode mode)
    {
        var transforms = candidates.Distinct().ToArray();
        // Validate the entire selection before writing any properties.
        var floors = new HashSet<Transform>();
        foreach (var t in transforms)
        {
            var floor = t;
            while (floor != null && !IsFloorInstance(floor)) floor = floor.parent;
            if (floor == null || !t.gameObject.scene.IsValid())
                throw new InvalidOperationException("Camera must belong to a scene instance of Floor.prefab: " + t.name);
            floors.Add(floor);
        }
        if (floors.Count > 1)
            throw new InvalidOperationException("Select cameras from one Floor instance at a time.");

        int count = 0;
        foreach (var t in transforms)
        {
            foreach (var propertyName in new[] { "m_LocalPosition", "m_LocalRotation", "m_LocalScale", "m_LocalEulerAnglesHint" })
            {
                // Applying an override may refresh the serialized object; fetch it again each time.
                var serialized = new SerializedObject(t);
                var property = serialized.FindProperty(propertyName);
                if (property == null) continue;
                bool changed = property.prefabOverride;
                foreach (var axis in new[] { "x", "y", "z", "w" })
                {
                    var component = property.FindPropertyRelative(axis);
                    changed |= component != null && component.prefabOverride;
                }
                if (!changed) continue;
                PrefabUtility.ApplyPropertyOverride(property, FloorPath, mode);
                count++;
            }
        }
        if (count == 0) return;
        AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<GameObject>(FloorPath));
        Debug.Log("Saved " + count + " camera transform properties to Floor.prefab. Play mode will use these placements.");
    }
}
