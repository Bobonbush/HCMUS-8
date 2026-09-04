using System;
using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class NghiSelectionCheck
{
    static NghiSelectionCheck() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        if (!File.Exists("Temp/NghiSelection.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Temp/NghiSelection.request");
        try
        {
            new NghiAnomalyTests().SwitchingAwayFromFollow_RestoresPatrolAndClearsFollowState();
            File.WriteAllText("Temp/NghiSelectionReport.txt", "PASS: switching from active #15 restores patrol, clears follow state, disables chase, and resets anomaly type.");
        }
        catch (Exception e) { File.WriteAllText("Temp/NghiSelectionReport.txt", "FAIL: " + e); }
    }
}
