using UnityEditor;
using UnityEngine;

// Scope all import changes to the two Rocketbox motions and this avatar.
public class NghiRocketboxImport : AssetPostprocessor
{
    const string Root = "Assets/Art/NghiAnomalies/Rocketbox/";
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
    }
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (TextureImporter)assetImporter;
        if (assetPath.Contains("_normal")) importer.textureType = TextureImporterType.NormalMap;
        importer.maxTextureSize = 2048;
    }
    void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.StartsWith(Root)) return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.name = t.name.Replace("Bip02", "Bip01");
    }
}
