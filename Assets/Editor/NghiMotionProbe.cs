using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NghiMotionProbe
{
    static NghiMotionProbe() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        if (!File.Exists("Temp/NghiMotionProbe.request")) return;
        File.Delete("Temp/NghiMotionProbe.request");
        var log = new System.Text.StringBuilder();
        foreach (var path in Directory.GetFiles("Assets/Art/NghiAnomalies/Rocketbox", "*.fbx"))
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            log.AppendLine(path);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) log.AppendLine("NODE " + AnimationUtility.CalculateTransformPath(t, model.transform));
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) log.AppendLine("MESH " + r.name + " MATERIALS " + string.Join(",",r.sharedMaterials.Select(m=>m.name)));
            foreach (var c in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")))
            {
                log.AppendLine("CLIP " + c.name + " length=" + c.length);
                foreach (var b in AnimationUtility.GetCurveBindings(c).Take(35)) log.AppendLine(b.path + " : " + b.propertyName);
            }
        }
        File.WriteAllText("Temp/NghiMotionProbe.txt", log.ToString());
    }
}
