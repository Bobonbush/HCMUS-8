using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NghiFacePlacement
{
    const string Path = "Assets/Prefabs/Floor.prefab";
    static NghiFacePlacement() { EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingEditMode) Save(); }; }
    [MenuItem("Tools/Save Window Face Placements to Shared Floor Prefab")]
    public static void Save()
    {
        var floors = Resources.FindObjectsOfTypeAll<AnomolyManager>().Where(m => m.name == "Floor"
            && m.gameObject.scene.IsValid() && m.gameObject.scene.isLoaded
            && AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(m.gameObject)) == Path).ToArray();
        if (floors.Length != 1) return;
        var anomaly = floors[0].GetComponentInChildren<Anomoly28>(true);
        if (anomaly == null) return;
        foreach (var face in anomaly.windowFaces.Where(f => f != null).Distinct())
            foreach (var t in face.GetComponentsInChildren<Transform>(true))
                foreach (var key in new[] { "m_LocalPosition", "m_LocalRotation", "m_LocalScale" })
                {
                    var p = new SerializedObject(t).FindProperty(key);
                    if (p.prefabOverride) PrefabUtility.ApplyPropertyOverride(p, Path, InteractionMode.AutomatedAction);
                }
        foreach (var key in new[] { "faceCount", "spacing", "columns", "arrangeAsGrid" })
        {
            var p = new SerializedObject(anomaly).FindProperty(key);
            if (p.prefabOverride) PrefabUtility.ApplyPropertyOverride(p, Path, InteractionMode.AutomatedAction);
        }
        AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
    }
}
