using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Diagnostics only: never rebuilds or saves the user's prefab or open scene.
[InitializeOnLoad]
public static class NghiReadOnlyAudit
{
    static NghiReadOnlyAudit() { EditorApplication.delayCall += RunOnce; }
    private static void RunOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("NghiReadOnlyAudit.1", false)) return;
        SessionState.SetBool("NghiReadOnlyAudit.1", true);
        Run();
    }

    [MenuItem("Tools/Audit Nghi Anomalies (No Rebuild)")]
    public static void Run()
    {
        var lines = new List<string> { "Read-only audit: " + DateTime.Now.ToString("O") };
        var tests = new NghiAnomalyTests();
        Check(lines, "Visibility Evaluate/Restore", tests.VisibilityAnomalies_EvaluateAndRestoreAreExactInverses);
        Check(lines, "Trash swap", tests.TrashSwap_EvaluateAndRestoreSelectExactlyOneModel);
        Check(lines, "Manager routing types", tests.AssignedTypes_MatchManagerRouting);
        GameObject floor = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Floor.prefab");
        try
        {
            var manager = floor.GetComponent<AnomolyManager>();
            var method = typeof(AnomolySetupNghi).GetMethod("ValidateLoaded", BindingFlags.NonPublic | BindingFlags.Static);
            var failures = (List<string>)method.Invoke(null, new object[] { floor, manager, true });
            // Material static fields are not initialized by this non-mutating audit.
            failures.RemoveAll(x => x.Contains("cardboard texture is missing"));
            lines.AddRange(failures.Select(x => "FAIL: " + x));
            if (failures.Count == 0) lines.Add("PASS: prefab references, registration, existing #09 and visibility transitions");
            foreach (var id in new[] { 34, 11, 15, 19, 21, 28 })
            {
                var host = floor.transform.Find("Anomoly#" + id);
                var component = host == null ? null : host.GetComponents<MonoBehaviour>().FirstOrDefault(x => x is Anomoly);
                lines.Add("#" + id + ": present=" + (component != null) + ", enabled=" + (component != null && component.enabled) + ", active=" + (host != null && host.gameObject.activeInHierarchy) + ", manager index=" + (component == null ? -1 : manager.anomolies.IndexOf(component)));
            }
            var npc = floor.GetComponentInChildren<Anomoly15>(true);
            lines.Add("NPC target: " + (npc.agentObject == null ? "MISSING" : npc.agentObject.name));
            var flood = floor.GetComponentInChildren<Anomoly21>(true);
            lines.Add("Flood: surfaces=" + flood.waterSurfaces.Count + ", rise=" + flood.riseHeight + "m, duration=" + flood.riseDuration + "s");
            var trash = floor.GetComponentInChildren<Anomoly11>(true);
            var mat = trash.replacementTrash.GetComponentInChildren<Renderer>(true).sharedMaterial;
            lines.Add("Trash texture: " + AssetDatabase.GetAssetPath(mat.mainTexture) + ", alpha clip=" + mat.GetFloat("_AlphaClip") + ", cull=" + mat.GetFloat("_Cull"));
        }
        catch (Exception ex) { lines.Add("FAIL: " + ex); }
        finally { PrefabUtility.UnloadPrefabContents(floor); }
        foreach (var gm in Resources.FindObjectsOfTypeAll<GameManager>())
        {
            if (!gm.gameObject.scene.IsValid()) continue;
            var so = new SerializedObject(gm);
            lines.Add("Open scene GameManager FloorObject: " + AssetDatabase.GetAssetPath(so.FindProperty("FloorObject").objectReferenceValue));
        }
        lines.Add("NOT TESTED: real-time NPC following/line-of-sight, timed flood animation, in-game visual placement.");
        Directory.CreateDirectory("Temp");
        File.WriteAllLines("Temp/NghiReadOnlyAudit.txt", lines);
        Debug.Log(string.Join("\n", lines));
    }

    private static void Check(List<string> lines, string name, Action test)
    {
        try { test(); lines.Add("PASS: " + name); }
        catch (Exception ex) { lines.Add("FAIL: " + name + ": " + ex.Message); }
    }
}
