using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class NghiPosterPlacement
{
    const string FloorPath = "Assets/Prefabs/Floor.prefab";
    [MenuItem("Tools/Save Selected PCCC Placements to Shared Floor Prefab")]
    public static void SaveSelected()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first; save edit-mode placements only.");
        var posters = Selection.transforms.Select(t => t.name == "OfficialPCCCArtwork" ? t.parent : t)
            .SelectMany(t => t.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name.StartsWith("FireSafetyPoster_")).Distinct().ToArray();
        if (posters.Length == 0) throw new InvalidOperationException("Select the PCCC poster(s) or their parent group in the scene first.");
        var floor = PrefabUtility.LoadPrefabContents(FloorPath);
        try
        {
            foreach (var poster in posters)
            {
                Transform instance = poster;
                while (instance != null)
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(instance.gameObject);
                    if (source != null && AssetDatabase.GetAssetPath(source) == FloorPath && source.transform.parent == null) break;
                    instance = instance.parent;
                }
                if (instance == null) throw new InvalidOperationException("Selected poster is not in a Floor.prefab instance: " + poster.name);
                var target = floor.transform.Find("NghiDecor/Fire Safety Posters (Nghi)/" + poster.name);
                if (target == null) throw new InvalidOperationException("Missing matching prefab poster: " + poster.name);
                Matrix4x4 relative = target.parent.worldToLocalMatrix * floor.transform.localToWorldMatrix * instance.worldToLocalMatrix * poster.localToWorldMatrix;
                target.localPosition = relative.GetColumn(3);
                target.localRotation = relative.rotation;
                target.localScale = relative.lossyScale;
                // The artwork can have its own rotation override, independent of the frame.
                var artwork = poster.Find("OfficialPCCCArtwork");
                var targetArtwork = target.Find("OfficialPCCCArtwork");
                if (artwork != null && targetArtwork != null)
                {
                    targetArtwork.localPosition = artwork.localPosition;
                    targetArtwork.localRotation = artwork.localRotation;
                    targetArtwork.localScale = artwork.localScale;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(floor, FloorPath);
            Debug.Log("Saved " + posters.Length + " PCCC placements in floor-local coordinates. Share Floor.prefab and its .meta; other scene overrides were not applied.");
        }
        finally { PrefabUtility.UnloadPrefabContents(floor); }
    }
}
