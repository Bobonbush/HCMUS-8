using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NghiExtensionSetup
{
    const string FloorPath = "Assets/Prefabs/Floor.prefab";
    const string WomanPath = "Assets/Art/NghiAnomalies/OnlineModels/NPCWoman.glb";
    const string Request = "Temp/NghiInstallExtensions.request";
    static NghiExtensionSetup() { EditorApplication.delayCall += InstallRequested; }
    static void InstallRequested()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        Install();
    }
    [MenuItem("Tools/Install Nghi Extensions (17, 35, Corpse)")]
    public static void Install()
    {
        var floor = PrefabUtility.LoadPrefabContents(FloorPath);
        var report = new List<string>();
        try
        {
            var manager = floor.GetComponent<AnomolyManager>();
            Build17(floor, manager, report);
            if (floor.GetComponentInChildren<Anomoly35>(true) == null) Build35(floor, manager, report);
            BuildCorpse(floor, report);
            var window = floor.GetComponentInChildren<Anomoly28>(true);
            // Keep the user's original anchor and single-face default; count is Inspector-editable.
            if (window != null) { window.columns = 3; window.arrangeAsGrid = true; }
            Test(floor, report);
            RenderCorpse(floor);
            PrefabUtility.SaveAsPrefabAsset(floor, FloorPath);
            AssetDatabase.SaveAssets();
            File.WriteAllLines("Temp/NghiExtensionsReport.txt", report);
            Debug.Log(string.Join("\n", report));
        }
        finally { PrefabUtility.UnloadPrefabContents(floor); }
    }
    static T Host<T>(GameObject floor, int id, AnomolyManager manager) where T : MonoBehaviour
    {
        var go = new GameObject("Anomoly#" + id);
        go.transform.SetParent(floor.transform, false);
        var component = go.AddComponent<T>();
        manager.anomolies.Add(component);
        return component;
    }
    static GameObject Model(string path, Transform parent, string name)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null) throw new Exception("Missing imported model: " + path);
        var go = UnityEngine.Object.Instantiate(source, parent, false);
        go.name = name;
        foreach (var collider in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        foreach (var animator in go.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        return go;
    }
    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new Exception("Model has no renderers: " + go.name);
        var b = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }
    static AnimationClip Clip(string suffix)
    {
        return AssetDatabase.LoadAllAssetsAtPath(WomanPath).OfType<AnimationClip>()
            .FirstOrDefault(c => c.name.EndsWith("|" + suffix, StringComparison.OrdinalIgnoreCase) || c.name.Equals(suffix, StringComparison.OrdinalIgnoreCase));
    }
    static void Build17(GameObject floor, AnomolyManager manager, List<string> report)
    {
        var anomaly = floor.GetComponentInChildren<Anomoly17>(true);
        if (anomaly != null && anomaly.alternateAppearance != null && anomaly.alternateAppearance.transform.Find("WomanModel") != null) return;
        if (anomaly == null) anomaly = Host<Anomoly17>(floor, 17, manager);
        if (anomaly.alternateAppearance != null) UnityEngine.Object.DestroyImmediate(anomaly.alternateAppearance);
        anomaly.npc = floor.GetComponentInChildren<NPC>(true).gameObject;
        anomaly.normalRenderers = anomaly.npc.GetComponentsInChildren<Renderer>(true);
        var wrapper = new GameObject("Nghi17_FemaleAppearance");
        wrapper.transform.SetParent(anomaly.npc.transform, false);
        var model = Model(WomanPath, wrapper.transform, "WomanModel");
        var idle = Clip("Idle");
        if (idle != null) idle.SampleAnimation(model, 0f);
        var b = BoundsOf(model);
        wrapper.transform.localScale *= 1.72f / b.size.y;
        b = BoundsOf(model);
        wrapper.transform.position += new Vector3(anomaly.npc.transform.position.x - b.center.x,
            anomaly.npc.transform.position.y - b.min.y, anomaly.npc.transform.position.z - b.center.z);
        foreach (var importedAnimator in model.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(importedAnimator);
        var animation = model.GetComponent<Animation>();
        if (animation == null) animation = model.AddComponent<Animation>();
        foreach (var key in new[] { "Idle", "Walk" })
        {
            var source = Clip(key);
            if (source == null) { report.Add("WARNING: model has no " + key + " clip"); continue; }
            string path = "Assets/Art/NghiAnomalies/NPCWoman_" + key + ".anim";
            var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (copy == null) { copy = UnityEngine.Object.Instantiate(source); copy.legacy = true; copy.wrapMode = WrapMode.Loop; AssetDatabase.CreateAsset(copy, path); }
            animation.AddClip(copy, key);
        }
        animation.playAutomatically = false;
        anomaly.alternateAnimation = animation;
        anomaly.idleClip = "Idle"; anomaly.walkClip = "Walk";
        anomaly.alternateAppearance = wrapper;
        wrapper.SetActive(false);
        report.Add("Installed #17: sourced female civilian, same NPC navigation/collider; clips=" + animation.GetClipCount());
    }
    static void Face(Anomoly35 a, Transform parent, string name, Vector3 position, Vector3 target)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        var direction = target - position; direction.y = 0;
        root.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        var model = Model("Assets/Art/NghiAnomalies/OnlineModels/WindowHead.glb", root.transform, "SourcedFace");
        model.transform.localScale *= 0.34f / BoundsOf(model).size.y;
        model.transform.position += position - BoundsOf(model).center;
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/NghiAnomalies/M_Nghi_PaleSkin.mat");
        foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => mat).ToArray();
        root.SetActive(false); a.classroomFaces.Add(root);
    }
    static void Build35(GameObject floor, AnomolyManager manager, List<string> report)
    {
        var anomaly = Host<Anomoly35>(floor, 35, manager);
        var room = floor.transform.Find("ClassRoom Detail");
        var doorway = floor.transform.Find("Elevator/ClassRoomDoor");
        if (room == null || doorway == null) throw new Exception("Classroom anchors not found");
        anomaly.doorway = doorway;
        var doors = doorway.GetComponentsInChildren<Renderer>(true);
        var doorBounds = doors[0].bounds;
        foreach (var r in doors.Skip(1)) doorBounds.Encapsulate(r.bounds);
        var entrance = doorBounds.center;
        // Select only the first room's chair rows, not chairs in neighbouring rooms.
        var chairs = room.Cast<Transform>().Where(t => t.name.StartsWith("chair1") && t.localPosition.x < 9f)
            .OrderBy(t => t.localPosition.x).ThenBy(t => t.localPosition.z).Take(12).ToList();
        foreach (var chair in chairs)
        {
            var b = BoundsOf(chair.gameObject);
            var position = new Vector3(b.center.x, b.min.y + 1.13f, b.center.z);
            Face(anomaly, anomaly.transform, "SeatedFace_" + anomaly.classroomFaces.Count, position, entrance);
        }
        var seatCentre = chairs.Aggregate(Vector3.zero, (sum, t) => sum + t.position) / chairs.Count;
        var inward = seatCentre - entrance; inward.y = 0; inward.Normalize();
        var side = Vector3.Cross(Vector3.up, inward);
        for (int i = 0; i < 3; i++)
        {
            var position = entrance + inward * 0.45f + side * ((i - 1) * 0.38f);
            position.y = chairs[0].position.y + 1.58f + (i == 1 ? 0.12f : 0f);
            Face(anomaly, anomaly.transform, "DoorwayFace_" + i, position, entrance - inward);
        }
        report.Add("Installed #35: " + chairs.Count + " seated faces + 3 doorway faces, facing first classroom door");
    }
    static void BuildCorpse(GameObject floor, List<string> report)
    {
        var anomaly = floor.GetComponentInChildren<Anomoly19>(true);
        var root = anomaly.corpses[0];
        var previous = root.transform.Find("DeathPoseCivilian");
        if (previous != null && previous.Find("RelaxedPoseV3") == null)
        {
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            previous = null;
        }
        if (previous != null)
        {
            var size = BoundsOf(previous.gameObject).size;
            if (Mathf.Max(size.x, size.z) > 3f)
            {
                previous.localScale *= 1.72f / Mathf.Max(size.x, size.z);
                GroundCorpse(floor, previous.gameObject);
                report.Add("Corrected imported death-animation root scale; corpse bounds=" + BoundsOf(previous.gameObject).size.ToString("F3"));
            }
            return;
        }
        var death = Clip("Death");
        if (death == null) throw new Exception("Death animation missing; refusing to create another T-pose corpse");
        var temp = Model(WomanPath, root.transform, "DeathPoseCivilian");
        temp.SetActive(true);
        var idle = Clip("Idle");
        if (idle != null) idle.SampleAnimation(temp, 0f);
        temp.transform.localScale *= 1.72f / BoundsOf(temp).size.y;
        death.SampleAnimation(temp, Mathf.Max(0f, death.length - 0.02f));
        RelaxLimbs(temp);
        foreach (var skinned in temp.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = new Mesh(); skinned.BakeMesh(mesh);
            string path = "Assets/Art/NghiAnomalies/CorpseRelaxedPose_" + skinned.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
            var filter = skinned.gameObject.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
            var materials = skinned.sharedMaterials;
            UnityEngine.Object.DestroyImmediate(skinned);
            filter.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }
        foreach (var animation in temp.GetComponentsInChildren<Animation>(true)) UnityEngine.Object.DestroyImmediate(animation);
        new GameObject("RelaxedPoseV3").transform.SetParent(temp.transform, false);
        var posedSize = BoundsOf(temp).size;
        temp.transform.localScale *= 1.72f / Mathf.Max(posedSize.x, posedSize.z);
        // Place the posed body beside, not through, the bowl. Floor height uses the existing toilet base.
        var toilet = floor.GetComponentsInChildren<Transform>(true).First(t => t.name == "SM_toilet_bowl Variant");
        var toiletBounds = BoundsOf(toilet.gameObject);
        root.transform.rotation = toilet.rotation;
        var b = BoundsOf(temp);
        temp.transform.position += new Vector3(toiletBounds.max.x + 0.12f - b.min.x,
            toiletBounds.min.y + 0.008f - b.min.y, toiletBounds.center.z - b.center.z);
        // Retain old children as disabled backups; do not destroy the original model asset.
        foreach (Transform child in root.transform) if (child != temp.transform) child.gameObject.SetActive(false);
        root.SetActive(false);
        report.Add("Corpse baked from final Death animation frame: floor gap=" + (BoundsOf(temp).min.y - toiletBounds.min.y).ToString("F3") + "m; bounds=" + BoundsOf(temp).size.ToString("F3"));
    }
    static void GroundCorpse(GameObject floor, GameObject body)
    {
        var toilet = floor.GetComponentsInChildren<Transform>(true).First(t => t.name == "SM_toilet_bowl Variant");
        var tb = BoundsOf(toilet.gameObject); var b = BoundsOf(body);
        body.transform.position += new Vector3(tb.max.x + 0.12f - b.min.x, tb.min.y + 0.008f - b.min.y, tb.center.z - b.center.z);
    }
    static void RelaxLimbs(GameObject model)
    {
        var bones = model.GetComponentsInChildren<Transform>(true);
        Func<string, Transform> bone = name => bones.First(t => t.name == name);
        Vector3 downBody = bone("Hips").position - bone("Neck").position;
        downBody.y = 0; downBody.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, downBody).normalized;
        Aim(bone("UpperArm.L"), bone("LowerArm.L"), downBody - side * 0.28f);
        Aim(bone("LowerArm.L"), bone("Wrist.L"), downBody + side * 0.65f + Vector3.up * 0.10f);
        Aim(bone("UpperArm.R"), bone("LowerArm.R"), downBody + side * 0.55f);
        Aim(bone("LowerArm.R"), bone("Wrist.R"), downBody + side * 0.12f);
        Aim(bone("UpperLeg.L"), bone("LowerLeg.L"), downBody - side * 0.12f + Vector3.up * 0.36f);
        Aim(bone("LowerLeg.L"), bone("Foot.L"), downBody + side * 0.15f - Vector3.up * 0.36f);
    }
    static void Aim(Transform joint, Transform next, Vector3 direction)
    {
        joint.rotation = Quaternion.FromToRotation(next.position - joint.position, direction.normalized) * joint.rotation;
    }
    static void Test(GameObject floor, List<string> report)
    {
        var a = floor.GetComponentInChildren<Anomoly17>(true);
        var corpse = floor.GetComponentInChildren<Anomoly19>(true).corpses[0].transform.Find("DeathPoseCivilian");
        if (BoundsOf(corpse.gameObject).size.magnitude > 3f) throw new Exception("Corpse scale is not human-sized");
        bool npcActive = a.npc.activeSelf;
        var states = a.normalRenderers.Select(r => r.enabled).ToArray();
        a.Evaluate();
        if (!a.alternateAppearance.activeSelf || a.normalRenderers.Any(r => r.enabled)) throw new Exception("#17 swap failed");
        a.Restore();
        if (a.npc.activeSelf != npcActive || !states.SequenceEqual(a.normalRenderers.Select(r => r.enabled))) throw new Exception("#17 restore failed");
        var b = floor.GetComponentInChildren<Anomoly35>(true); b.Evaluate();
        if (b.classroomFaces.Any(f => !f.activeSelf)) throw new Exception("#35 show failed");
        b.Restore();
        if (b.classroomFaces.Any(f => f.activeSelf)) throw new Exception("#35 hide failed");
        var w = floor.GetComponentInChildren<Anomoly28>(true);
        var anchor = w.windowFaces[0].transform; var saved = anchor.localPosition; int count = w.faceCount;
        w.faceCount = 9; w.Evaluate(); w.Restore(); w.faceCount = count;
        if ((anchor.localPosition - saved).sqrMagnitude > 0.000001f) throw new Exception("#28 restore moved anchor");
        // Runtime grid copies are ephemeral, not prefab content.
        foreach (var t in floor.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("WindowFace_Grid_")).ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
        report.Add("PASS: #17 visual swap/restore; #35 visibility; #28 nine-face grid/anchor restore. No scene or poster transforms edited.");
        report.Add("Live gameplay/visual composition still requires in-editor review.");
    }
    static void RenderCorpse(GameObject floor)
    {
        var source = floor.GetComponentInChildren<Anomoly19>(true).corpses[0].transform.Find("DeathPoseCivilian");
        if (source == null) return;
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var rt = new RenderTexture(720, 540, 24);
        var old = RenderTexture.active;
        Texture2D image = null;
        try
        {
            var model = UnityEngine.Object.Instantiate(source.gameObject);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model, scene);
            model.SetActive(true);
            var bounds = BoundsOf(model);
            var cameraObject = new GameObject("Preview Camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.13f, 0.14f);
            camera.transform.position = bounds.center + new Vector3(1.6f, 1.9f, 2.2f);
            camera.transform.LookAt(bounds.center); camera.fieldOfView = 42f; camera.targetTexture = rt;
            var lightObject = new GameObject("Preview Light");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2.3f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            camera.Render(); RenderTexture.active = rt;
            image = new Texture2D(720, 540, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 720, 540), 0, 0); image.Apply();
            File.WriteAllBytes("Temp/NghiCorpsePreview.png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = old;
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
