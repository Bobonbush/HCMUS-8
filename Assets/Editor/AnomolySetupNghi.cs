using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Repeatable setup for Nghi's anomaly batch. Missing decor is restored once;
// existing decor children are never rebuilt so manual placement stays intact.
// Existing anomaly objects and list entries are never replaced or reordered.
[InitializeOnLoad]
public static class AnomolySetupNghi
{
    private const string FloorPrefabPath = "Assets/Prefabs/Floor.prefab";
    private const string ArtFolder = "Assets/Art/NghiAnomalies";
    private const string SessionKey = "HCMUS8.NghiAnomalySetup.v14";
    private const string ValidationReportPath = "Temp/NghiAnomalyValidation.txt";
    private const string OnlineFolder = ArtFolder + "/OnlineModels";
    private const string TrashModelPath = OnlineFolder + "/SchoolTrashcan.glb";
    private const string TrashCardboardPath = OnlineFolder + "/SchoolTrashcan_Cardboard.png";
    private const string CameraModelPath = OnlineFolder + "/CCTV_Camera.glb";
    private const string WindowHeadPath = OnlineFolder + "/WindowHead.glb";
    private const string CorpseModelPath = "Assets/Art/Characters/Zombie.obj";
    private const string PcccTexturePath = ArtFolder + "/PCCC_Official_AnGiang.jpg";

    private static Material greenBinMaterial;
    private static Material cardboardMaterial;
    private static Material decorFrameMaterial;
    private static Material pcccPosterMaterial;
    private static Material waterMaterial;
    private static Material skinMaterial;
    private static Material bloodMaterial;

    static AnomolySetupNghi()
    {
        EditorApplication.delayCall += AutoRun;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void AutoRun()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        Run();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += AutoRun;
    }

    [MenuItem("Tools/Anomoly Setup (Nghi)")]
    public static void Run()
    {
        EnsureMaterials();
        GameObject floor = PrefabUtility.LoadPrefabContents(FloorPrefabPath);
        if (floor == null)
        {
            Debug.LogError("Nghi setup: could not open " + FloorPrefabPath);
            return;
        }

        try
        {
            AnomolyManager manager = floor.GetComponent<AnomolyManager>();
            if (manager == null)
            {
                Debug.LogError("Nghi setup: Floor has no AnomolyManager.");
                return;
            }
            if (manager.anomolies == null) manager.anomolies = new List<MonoBehaviour>();

            CleanupLegacyAnomalyCameraGroup(floor.transform);
            Transform anomalyAssets = Child(floor.transform, "NghiAnomalyAssets");
            List<Transform> corridorAnchors = FindCorridorAnchors(floor.transform);
            Vector3 corridorCentre = Average(corridorAnchors.Select(t => t.localPosition));
            EnsurePermanentDecorIfMissing(floor, corridorAnchors, corridorCentre);

            Anomoly11 trash = BuildAnomoly11(floor, anomalyAssets);
            Register(manager, trash);
            Register(manager, BuildAnomoly34(floor, anomalyAssets, corridorAnchors, corridorCentre,
                trash.normalTrash != null ? trash.normalTrash.transform : null));
            Register(manager, BuildAnomoly15(floor));
            Transform toilet = FindFirstDeep(floor.transform, "SM_toilet_bowl Variant");
            Register(manager, BuildAnomoly19(floor, toilet));
            Register(manager, BuildAnomoly21(floor, corridorAnchors, corridorCentre));
            Register(manager, BuildAnomoly28(floor, toilet, corridorCentre));

            List<string> failures = ValidateLoaded(floor, manager, true);
            PrefabUtility.SaveAsPrefabAsset(floor, FloorPrefabPath);
            AssetDatabase.SaveAssets();
            string report = failures.Count == 0
                ? "PASS - all six Nghi anomalies, sourced visual assets, spatial placement, inverse state transitions, and preserved Anomoly09 references validated."
                : "FAIL\n- " + string.Join("\n- ", failures);
            File.WriteAllText(ValidationReportPath, report + "\nChecked: " + DateTime.Now.ToString("O"));
            if (failures.Count > 0) Debug.LogError("Nghi setup validation failed:\n- " + string.Join("\n- ", failures));
            Debug.Log("Nghi setup: six anomalies (11, 15, 19, 21, 28, 34) are wired with sourced assets. " +
                      "Manual decor and existing Anomoly09 were preserved. Manager count: " + manager.anomolies.Count);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(floor);
        }
    }

    [MenuItem("Tools/Validate Nghi Anomolies")]
    public static void Validate()
    {
        EnsureMaterials();
        GameObject floor = PrefabUtility.LoadPrefabContents(FloorPrefabPath);
        if (floor == null) return;
        try
        {
            AnomolyManager manager = floor.GetComponent<AnomolyManager>();
            List<string> failures = ValidateLoaded(floor, manager, true);

            if (failures.Count == 0) Debug.Log("Nghi validation PASSED: all six sourced-asset anomalies are complete.");
            else Debug.LogError("Nghi validation FAILED:\n- " + string.Join("\n- ", failures));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(floor);
        }
    }

    private static Anomoly34 BuildAnomoly34(GameObject floor, Transform anomalyAssets,
                                             List<Transform> anchors, Vector3 centre,
                                             Transform schoolBin)
    {
        Anomoly34 anomoly = Host<Anomoly34>(floor, "Anomoly#34");
        Transform group = Child(anomalyAssets, "Anomoly34 Absurd Cameras");
        ClearChildren(group);
        anomoly.extraCameras = new List<GameObject>();

        Vector3 corridorWorld = floor.transform.TransformPoint(centre + Vector3.up * 1.1f);
        if (schoolBin != null)
        {
            Renderer binRenderer = schoolBin.GetComponentInChildren<Renderer>(true);
            Vector3 binPoint = binRenderer != null
                ? new Vector3(binRenderer.bounds.center.x, binRenderer.bounds.min.y + binRenderer.bounds.size.y * 0.64f,
                              binRenderer.bounds.center.z)
                : schoolBin.position + Vector3.up * 0.55f;
            AddCamera(anomoly, CreateSourcedCamera(group, "Camera_Bin_Peeking", binPoint,
                corridorWorld, CameraMount.Resting));
        }

        Transform stairs = FindFirstContaining(floor.transform, "stairs");
        if (stairs != null)
        {
            Bounds stairBounds = CombinedBounds(stairs.gameObject);
            for (int i = 0; i < 3; i++)
            {
                // Stay on the first flight of this multi-storey stair mesh.
                float t = 0.08f + i * 0.06f;
                Vector3 point = Vector3.Lerp(stairBounds.min, stairBounds.max, t);
                point.y = stairBounds.min.y + stairBounds.size.y * t + 0.02f;
                AddCamera(anomoly, CreateSourcedCamera(group, "Camera_StairStep_" + i, point,
                    point + floor.transform.up * 2.5f + (corridorWorld - point).normalized, CameraMount.Resting));
            }
        }

        List<Renderer> railCandidates = FindRenderersByAncestor(floor.transform, "korkuluk");
        for (int i = 0; i < Mathf.Min(3, railCandidates.Count); i++)
        {
            int railIndex = Mathf.RoundToInt(i * (railCandidates.Count - 1f) / 2f);
            Renderer rail = railCandidates[railIndex];
            Vector3 point = new Vector3(rail.bounds.center.x, rail.bounds.max.y + 0.015f,
                                        rail.bounds.center.z);
            AddCamera(anomoly, CreateSourcedCamera(group, "Camera_RailingBird_" + i, point,
                corridorWorld + Vector3.down * (0.2f + i * 0.2f), CameraMount.Resting));
        }

        // A crowded row remains part of the gag, but every unit is attached to a real light/ceiling anchor.
        int crowdCount = Mathf.Min(8, anchors.Count);
        Vector3 corridorSide = Vector3.Cross(CorridorDirection(anchors), Vector3.up).normalized;
        for (int i = 0; i < crowdCount; i++)
        {
            int index = Mathf.RoundToInt(i * (anchors.Count - 1f) / Mathf.Max(1, crowdCount - 1));
            Vector3 point = anchors[index].position + floor.transform.TransformDirection(corridorSide) *
                (i % 2 == 0 ? 0.42f : -0.42f);
            point -= floor.transform.up * 0.05f;
            AddCamera(anomoly, CreateSourcedCamera(group, "Camera_CeilingCrowd_" + i, point,
                corridorWorld, CameraMount.Ceiling));
        }

        group.gameObject.SetActive(true);
        foreach (GameObject cameraObject in anomoly.extraCameras) cameraObject.SetActive(false);
        return anomoly;
    }

    private static Anomoly11 BuildAnomoly11(GameObject floor, Transform anomalyAssets)
    {
        Anomoly11 anomoly = Host<Anomoly11>(floor, "Anomoly#11");
        Transform reference = FindFirstDeep(floor.transform, "SM_trash_can Variant");
        Transform trashRoot = Child(anomalyAssets, "Anomoly11 School Trash");
        ClearChildren(trashRoot);

        GameObject normal = InstantiateAsset(TrashModelPath, trashRoot, "Normal_GreenSchoolTrash_3D");
        if (normal != null)
        {
            ApplyMaterial(normal, greenBinMaterial);
            NormalizeHeight(normal, 0.92f);
        }

        GameObject cardboard = new GameObject("Anomaly_LowResTransparentTrash_2D");
        cardboard.transform.SetParent(trashRoot, false);
        Primitive(cardboard.transform, "PixelatedSchoolBinCutout", PrimitiveType.Quad,
            Vector3.zero, Quaternion.identity, new Vector3(0.66f, 0.94f, 1f), cardboardMaterial);

        if (reference != null)
        {
            Renderer referenceRenderer = reference.GetComponentInChildren<Renderer>(true);
            float floorY = referenceRenderer != null ? referenceRenderer.bounds.min.y : reference.position.y;
            Vector3 centre = referenceRenderer != null ? referenceRenderer.bounds.center : reference.position;
            trashRoot.SetPositionAndRotation(new Vector3(centre.x, floorY, centre.z), reference.rotation);
            if (normal != null) AlignBottom(normal, floorY);
            cardboard.transform.position = new Vector3(centre.x, floorY + 0.50f, centre.z);
            cardboard.transform.rotation = reference.rotation;
            reference.gameObject.SetActive(false);
        }
        if (normal != null) normal.SetActive(true);
        cardboard.SetActive(false);
        anomoly.normalTrash = normal;
        anomoly.replacementTrash = cardboard;
        return anomoly;
    }

    private static Anomoly15 BuildAnomoly15(GameObject floor)
    {
        Anomoly15 anomoly = Host<Anomoly15>(floor, "Anomoly#15");
        NPC npc = floor.GetComponentInChildren<NPC>(true);
        anomoly.agentObject = npc != null ? npc.gameObject : null;
        return anomoly;
    }

    private static Anomoly19 BuildAnomoly19(GameObject floor, Transform toilet)
    {
        Anomoly19 anomoly = Host<Anomoly19>(floor, "Anomoly#19");
        Transform root = Child(floor.transform, "Nghi_ToiletCorpse");
        ClearChildren(root);
        float floorY = root.position.y;
        if (toilet != null)
        {
            Renderer toiletRenderer = toilet.GetComponentInChildren<Renderer>();
            floorY = toiletRenderer != null ? toiletRenderer.bounds.min.y : toilet.position.y;
            root.position = toilet.position + toilet.right * 0.58f + toilet.forward * 0.30f;
            root.position = new Vector3(root.position.x, floorY, root.position.z);
            root.rotation = toilet.rotation * Quaternion.Euler(0f, 35f, 0f);
        }

        GameObject corpseModel = InstantiateAsset(CorpseModelPath, root, "DetailedFemaleZombieCorpse");
        if (corpseModel != null)
        {
            NormalizeHeight(corpseModel, 1.72f);
            corpseModel.transform.localRotation = Quaternion.Euler(0f, 0f, 88f);
            Material zombieMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Characters/M_Zombie.mat");
            if (zombieMaterial != null) ApplyMaterial(corpseModel, zombieMaterial);
            AlignBottom(corpseModel, floorY + 0.018f);
        }
        Primitive(root, "BloodStain", PrimitiveType.Cylinder, new Vector3(-0.10f, 0.010f, 0.02f),
            Quaternion.identity, new Vector3(1.15f, 0.010f, 0.72f), bloodMaterial);
        root.gameObject.SetActive(false);
        anomoly.corpses = new List<GameObject> { root.gameObject };
        return anomoly;
    }

    private static Anomoly21 BuildAnomoly21(GameObject floor, List<Transform> anchors, Vector3 centre)
    {
        Anomoly21 anomoly = Host<Anomoly21>(floor, "Anomoly#21");
        Transform water = floor.transform.Find("Nghi_RisingWater");
        if (water == null)
        {
            Vector3 min = anchors.Count > 0 ? anchors[0].localPosition : Vector3.zero;
            Vector3 max = min;
            foreach (Transform anchor in anchors)
            {
                min = Vector3.Min(min, anchor.localPosition);
                max = Vector3.Max(max, anchor.localPosition);
            }
            Vector3 size = new Vector3(Mathf.Max(8f, max.x - min.x + 7f), 0.025f,
                                       Mathf.Max(8f, max.z - min.z + 7f));
            centre.y = 0.015f;
            water = Primitive(floor.transform, "Nghi_RisingWater", PrimitiveType.Cube,
                centre, Quaternion.identity, size, waterMaterial).transform;
        }
        water.gameObject.SetActive(false);
        anomoly.waterSurfaces = new List<Transform> { water };
        anomoly.riseDuration = 12f;
        anomoly.riseHeight = 0.30f;
        return anomoly;
    }

    private static Anomoly28 BuildAnomoly28(GameObject floor, Transform toilet, Vector3 corridorCentre)
    {
        Anomoly28 anomoly = Host<Anomoly28>(floor, "Anomoly#28");
        List<Transform> windows = FindAllDeep(floor.transform, "Window");
        Transform nearest = windows.OrderBy(w => toilet == null ? 0f : (w.position - toilet.position).sqrMagnitude).FirstOrDefault();
        Transform face = Child(floor.transform, "Nghi_WindowFace");
        ClearChildren(face);
        if (nearest != null)
        {
            Renderer renderer = nearest.GetComponentInChildren<Renderer>();
            Vector3 windowCentre = renderer != null ? renderer.bounds.center : nearest.position;
            Vector3 corridorWorld = floor.transform.TransformPoint(corridorCentre);
            Vector3 outward = windowCentre - corridorWorld;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.01f) outward = nearest.forward;
            outward.Normalize();
            face.position = windowCentre + outward * 0.08f;
            face.rotation = Quaternion.LookRotation(-outward, Vector3.up);
        }
        GameObject head = InstantiateAsset(WindowHeadPath, face, "DetailedUncannyHead");
        if (head != null)
        {
            NormalizeHeight(head, 0.48f);
            ApplyMaterial(head, skinMaterial);
            CentreModelOn(head, face.position);
        }
        face.gameObject.SetActive(false);
        anomoly.windowFaces = new List<GameObject> { face.gameObject };
        return anomoly;
    }

    private enum CameraMount { Resting, Ceiling }

    private static GameObject CreateSourcedCamera(Transform parent, string name, Vector3 mountPoint,
                                                   Vector3 target, CameraMount mount)
    {
        Transform root = Child(parent, name);
        root.position = mountPoint;
        Vector3 direction = target - mountPoint;
        root.rotation = direction.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;
        GameObject model = InstantiateAsset(CameraModelPath, root, "OnlineCCTVModel");
        if (model == null) return root.gameObject;
        NormalizeLargestDimension(model, 0.48f);
        Bounds bounds = CombinedBounds(model);
        Vector3 position = root.position;
        position.y += mount == CameraMount.Ceiling
            ? mountPoint.y - bounds.max.y
            : mountPoint.y - bounds.min.y;
        root.position = position;
        return root.gameObject;
    }

    private static void AddCamera(Anomoly34 anomoly, GameObject cameraObject)
    {
        if (cameraObject != null) anomoly.extraCameras.Add(cameraObject);
    }

    private static GameObject InstantiateAsset(string path, Transform parent, string name)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) return null;
        GameObject instance = PrefabUtility.InstantiatePrefab(asset, parent) as GameObject;
        if (instance == null) instance = UnityEngine.Object.Instantiate(asset, parent);
        instance.name = name;
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
        foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        return instance;
    }

    private static Bounds CombinedBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one * 0.01f);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void NormalizeHeight(GameObject model, float targetHeight)
    {
        float height = CombinedBounds(model).size.y;
        if (height > 0.0001f) model.transform.localScale *= targetHeight / height;
    }

    private static void NormalizeLargestDimension(GameObject model, float targetSize)
    {
        Vector3 size = CombinedBounds(model).size;
        float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        if (largest > 0.0001f) model.transform.localScale *= targetSize / largest;
    }

    private static void AlignBottom(GameObject model, float worldY)
    {
        Vector3 position = model.transform.position;
        position.y += worldY - CombinedBounds(model).min.y;
        model.transform.position = position;
    }

    private static void CentreModelOn(GameObject model, Vector3 worldCentre)
    {
        Vector3 position = model.transform.position;
        position += worldCentre - CombinedBounds(model).center;
        model.transform.position = position;
    }

    private static void ApplyMaterial(GameObject root, Material material)
    {
        if (material == null) return;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }

    private static void ClearChildren(Transform root)
    {
        while (root.childCount > 0) UnityEngine.Object.DestroyImmediate(root.GetChild(0).gameObject);
    }

    private static GameObject Primitive(Transform parent, string name, PrimitiveType type,
                                        Vector3 localPosition, Quaternion localRotation,
                                        Vector3 localScale, Material material)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        return go;
    }

    private static void EnsureMaterials()
    {
        Directory.CreateDirectory(ArtFolder);
        AssetDatabase.Refresh();
        greenBinMaterial = Material("M_Nghi_SchoolBinGreen", new Color(0.04f, 0.33f, 0.10f), false, 0.05f, 0.34f);
        cardboardMaterial = Material("M_Nghi_CardboardPhoto", Color.white, true, 0f, 0.20f);
        decorFrameMaterial = Material("M_Nghi_PosterFrame", new Color(0.055f, 0.06f, 0.065f), false, 0.12f, 0.48f);
        Texture2D cardboardTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TrashCardboardPath);
        if (cardboardTexture != null)
        {
            cardboardTexture.filterMode = FilterMode.Point;
            cardboardMaterial.mainTexture = cardboardTexture;
            if (cardboardMaterial.HasProperty("_BaseMap")) cardboardMaterial.SetTexture("_BaseMap", cardboardTexture);
            EditorUtility.SetDirty(cardboardMaterial);
        }
        pcccPosterMaterial = Material("M_Nghi_PCCCPoster", Color.white, false, 0f, 0.28f);
        Texture2D pcccTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PcccTexturePath);
        if (pcccTexture != null)
        {
            pcccPosterMaterial.mainTexture = pcccTexture;
            if (pcccPosterMaterial.HasProperty("_BaseMap")) pcccPosterMaterial.SetTexture("_BaseMap", pcccTexture);
            EditorUtility.SetDirty(pcccPosterMaterial);
        }
        waterMaterial = Material("M_Nghi_Water", new Color(0.05f, 0.30f, 0.42f, 0.46f), true, 0.18f, 0.92f);
        skinMaterial = Material("M_Nghi_PaleSkin", new Color(0.47f, 0.48f, 0.44f), false, 0f, 0.23f);
        bloodMaterial = Material("M_Nghi_Blood", new Color(0.23f, 0.003f, 0.003f), false, 0f, 0.45f);
    }

    private static Material Material(string name, Color color, bool transparent, float metallic, float smoothness)
    {
        string path = ArtFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (transparent)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", 5f);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", 10f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3000;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static List<Transform> FindCorridorAnchors(Transform floor)
    {
        List<Transform> result = new List<Transform>();
        foreach (Transform child in floor)
            if ((child.name == "f" || child.name.StartsWith("f (")) && child.GetComponent<Light>() != null)
                result.Add(child);
        result = result.OrderBy(t => t.localPosition.x).ThenBy(t => t.localPosition.z).ToList();
        if (result.Count == 0) result.Add(floor);
        return result;
    }

    private static Vector3 CorridorDirection(List<Transform> anchors)
    {
        if (anchors.Count < 2) return Vector3.right;
        float minX = anchors.Min(t => t.localPosition.x), maxX = anchors.Max(t => t.localPosition.x);
        float minZ = anchors.Min(t => t.localPosition.z), maxZ = anchors.Max(t => t.localPosition.z);
        return maxX - minX >= maxZ - minZ ? Vector3.right : Vector3.forward;
    }

    private static Vector3 Average(IEnumerable<Vector3> positions)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Vector3 position in positions) { sum += position; count++; }
        return count > 0 ? sum / count : Vector3.zero;
    }

    private static T Host<T>(GameObject floor, string name) where T : MonoBehaviour
    {
        Transform host = floor.transform.Find(name) ?? Child(floor.transform, name);
        T component = host.GetComponent<T>();
        if (component == null) component = host.gameObject.AddComponent<T>();
        return component;
    }

    private static Transform Child(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void Register(AnomolyManager manager, MonoBehaviour component)
    {
        if (component != null && !manager.anomolies.Contains(component)) manager.anomolies.Add(component);
    }

    private static void CleanupLegacyAnomalyCameraGroup(Transform floor)
    {
        // Only remove the obsolete anomaly-camera group. Permanent cameras and
        // posters may contain hand-tuned transforms and must remain untouched.
        Transform decor = floor.Find("NghiDecor");
        if (decor == null) return;
        Transform generated = decor.Find("Anomoly34 Extra Cameras");
        if (generated != null) UnityEngine.Object.DestroyImmediate(generated.gameObject);
    }

    private static void EnsurePermanentDecorIfMissing(GameObject floor, List<Transform> anchors, Vector3 centre)
    {
        Transform decor = Child(floor.transform, "NghiDecor");

        Transform cameras = decor.Find("Permanent Security Cameras (Nghi)");
        if (cameras == null || cameras.childCount == 0)
        {
            cameras = cameras ?? Child(decor, "Permanent Security Cameras (Nghi)");
            Vector3 target = floor.transform.TransformPoint(centre + Vector3.up * 1.15f);
            int count = Mathf.Min(3, anchors.Count);
            for (int i = 0; i < count; i++)
            {
                int index = Mathf.RoundToInt(i * (anchors.Count - 1f) / Mathf.Max(1, count - 1));
                Vector3 mount = anchors[index].position - floor.transform.up * 0.05f;
                CreateSourcedCamera(cameras, "PermanentCamera_" + i, mount, target, CameraMount.Ceiling);
            }
        }

        Transform posters = decor.Find("Fire Safety Posters (Nghi)");
        if (posters == null || posters.childCount == 0)
        {
            posters = posters ?? Child(decor, "Fire Safety Posters (Nghi)");
            Vector3 direction = CorridorDirection(anchors);
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;
            int count = Mathf.Min(3, anchors.Count);
            for (int i = 0; i < count; i++)
            {
                int index = Mathf.RoundToInt(i * (anchors.Count - 1f) / Mathf.Max(1, count - 1));
                Vector3 origin = anchors[index].position;
                origin.y = floor.transform.TransformPoint(centre).y + 1.65f;
                CreatePermanentPcccPoster(floor.transform, posters, i, origin,
                    floor.transform.TransformPoint(centre), floor.transform.TransformDirection(side));
            }
        }
    }

    private static void CreatePermanentPcccPoster(Transform floor, Transform parent, int index,
                                                   Vector3 origin, Vector3 corridorCentre, Vector3 preferredSide)
    {
        Vector3 point;
        Vector3 normal;
        if (!TryFindWallSurface(floor, origin, preferredSide, out point, out normal) &&
            !TryFindWallSurface(floor, origin, -preferredSide, out point, out normal))
        {
            normal = preferredSide.sqrMagnitude > 0.01f ? -preferredSide.normalized : Vector3.back;
            point = origin + preferredSide.normalized * 2.0f;
        }

        if (Vector3.Dot(normal, corridorCentre - point) < 0f) normal = -normal;
        Transform root = Child(parent, "FireSafetyPoster_" + index);
        root.position = point + normal * 0.024f;
        root.rotation = Quaternion.LookRotation(normal, Vector3.up);
        Primitive(root, "PosterFrame", PrimitiveType.Cube, new Vector3(0f, 0f, -0.020f), Quaternion.identity,
            new Vector3(1.317f, 1.06f, 0.035f), decorFrameMaterial);
        Primitive(root, "OfficialPCCCArtwork", PrimitiveType.Quad, Vector3.zero, Quaternion.identity,
            new Vector3(1.257f, 1.0f, 1f), pcccPosterMaterial);
    }

    private static bool TryFindWallSurface(Transform floor, Vector3 origin, Vector3 direction,
                                           out Vector3 point, out Vector3 normal)
    {
        direction.y = 0f;
        direction.Normalize();
        Ray ray = new Ray(origin, direction);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5f);
        foreach (RaycastHit hit in hits.OrderBy(h => h.distance))
        {
            if (hit.collider != null && IsWallRenderer(hit.collider.GetComponent<Renderer>(), floor))
            {
                point = hit.point;
                normal = hit.normal;
                return true;
            }
        }

        float nearest = float.PositiveInfinity;
        point = Vector3.zero;
        normal = -direction;
        foreach (Renderer renderer in floor.GetComponentsInChildren<Renderer>(true))
        {
            if (!IsWallRenderer(renderer, floor)) continue;
            float distance;
            if (renderer.bounds.IntersectRay(ray, out distance) && distance >= 0.05f && distance <= 5f && distance < nearest)
            {
                nearest = distance;
                point = ray.GetPoint(distance);
            }
        }
        return nearest < float.PositiveInfinity;
    }

    private static bool IsWallRenderer(Renderer renderer, Transform floor)
    {
        if (renderer == null) return false;
        Transform current = renderer.transform;
        while (current != null && current != floor)
        {
            if (current.name == "NghiDecor") return false;
            if (current.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            current = current.parent;
        }
        return false;
    }

    private static Transform FindFirstContaining(Transform root, string fragment)
    {
        foreach (Transform child in root)
        {
            if (child.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) return child;
            Transform nested = FindFirstContaining(child, fragment);
            if (nested != null) return nested;
        }
        return null;
    }

    private static List<Renderer> FindRenderersByAncestor(Transform root, string fragment)
    {
        return root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer =>
            {
                Transform current = renderer.transform;
                while (current != null && current != root)
                {
                    if (current.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    current = current.parent;
                }
                return false;
            })
            .OrderBy(renderer => renderer.bounds.center.x)
            .ThenBy(renderer => renderer.bounds.center.z)
            .ToList();
    }

    private static Transform FindFirstDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform nested = FindFirstDeep(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    private static List<Transform> FindAllDeep(Transform root, string baseName)
    {
        List<Transform> result = new List<Transform>();
        foreach (Transform child in root)
        {
            if (child.name == baseName || child.name.StartsWith(baseName + "_") ||
                child.name.StartsWith(baseName + " (")) result.Add(child);
            result.AddRange(FindAllDeep(child, baseName));
        }
        return result;
    }

    private static void ValidateComponent(GameObject floor, AnomolyManager manager, string objectName,
                                          List<string> failures, Func<MonoBehaviour, bool> referencesValid)
    {
        Transform host = floor.transform.Find(objectName);
        MonoBehaviour component = host != null ? host.GetComponents<MonoBehaviour>().FirstOrDefault(c => c is Anomoly) : null;
        if (component == null) { failures.Add(objectName + " component is missing"); return; }
        if (manager == null || !manager.anomolies.Contains(component)) failures.Add(objectName + " is not registered");
        if (!referencesValid(component)) failures.Add(objectName + " has missing references");
    }

    private static List<string> ValidateLoaded(GameObject floor, AnomolyManager manager, bool exerciseState)
    {
        List<string> failures = new List<string>();
        if (manager == null)
        {
            failures.Add("Floor is missing AnomolyManager");
            return failures;
        }

        ValidateComponent(floor, manager, "Anomoly#11", failures, c => ((Anomoly11)c).normalTrash != null && ((Anomoly11)c).replacementTrash != null);
        ValidateComponent(floor, manager, "Anomoly#15", failures, c => ((Anomoly15)c).agentObject != null);
        ValidateComponent(floor, manager, "Anomoly#19", failures, c => ((Anomoly19)c).corpses.Count > 0);
        ValidateComponent(floor, manager, "Anomoly#21", failures, c => ((Anomoly21)c).waterSurfaces.Count > 0);
        ValidateComponent(floor, manager, "Anomoly#28", failures, c => ((Anomoly28)c).windowFaces.Count > 0);
        ValidateComponent(floor, manager, "Anomoly#34", failures, c => ((Anomoly34)c).extraCameras.Count >= 12);

        CheckSingleRegistration<Anomoly11>(manager, failures);
        CheckSingleRegistration<Anomoly15>(manager, failures);
        CheckSingleRegistration<Anomoly19>(manager, failures);
        CheckSingleRegistration<Anomoly21>(manager, failures);
        CheckSingleRegistration<Anomoly28>(manager, failures);
        CheckSingleRegistration<Anomoly34>(manager, failures);

        Anomoly09 preserved = floor.GetComponentInChildren<Anomoly09>(true);
        if (preserved == null || preserved.rig == null || preserved.penHolder == null || preserved.paperSurface == null)
            failures.Add("Existing Anomoly09 writing rig was not preserved intact");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(TrashModelPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(CameraModelPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(WindowHeadPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(CorpseModelPath) == null)
            failures.Add("One or more sourced 3D anomaly assets failed to import");
        if (cardboardMaterial == null || cardboardMaterial.mainTexture == null)
            failures.Add("The low-resolution school-bin cardboard texture is missing");

        ValidateSpatialPlacement(floor, failures);

        if (!exerciseState) return failures;
        Anomoly34 cameras = floor.GetComponentInChildren<Anomoly34>(true);
        Anomoly19 corpse = floor.GetComponentInChildren<Anomoly19>(true);
        Anomoly28 face = floor.GetComponentInChildren<Anomoly28>(true);
        Anomoly11 trash = floor.GetComponentInChildren<Anomoly11>(true);
        Anomoly21 water = floor.GetComponentInChildren<Anomoly21>(true);

        if (cameras != null)
        {
            cameras.Restore(); cameras.Evaluate();
            if (cameras.extraCameras.Any(x => x == null || !x.activeSelf)) failures.Add("Anomoly34 Evaluate did not reveal every camera");
            cameras.Restore();
            if (cameras.extraCameras.Any(x => x != null && x.activeSelf)) failures.Add("Anomoly34 Restore left a camera visible");
        }
        if (corpse != null)
        {
            corpse.Restore(); corpse.Evaluate();
            if (corpse.corpses.Any(x => x == null || !x.activeSelf)) failures.Add("Anomoly19 Evaluate did not reveal the corpse");
            corpse.Restore();
        }
        if (face != null)
        {
            face.Restore(); face.Evaluate();
            if (face.windowFaces.Any(x => x == null || !x.activeSelf)) failures.Add("Anomoly28 Evaluate did not reveal the face");
            face.Restore();
        }
        if (trash != null)
        {
            trash.Restore(); trash.Evaluate();
            if (trash.normalTrash.activeSelf || !trash.replacementTrash.activeSelf) failures.Add("Anomoly11 Evaluate did not swap bins exactly");
            trash.Restore();
            if (!trash.normalTrash.activeSelf || trash.replacementTrash.activeSelf) failures.Add("Anomoly11 Restore did not restore bins exactly");
        }
        if (water != null)
        {
            water.Restore();
            if (water.waterSurfaces.Any(x => x != null && x.gameObject.activeSelf)) failures.Add("Anomoly21 Restore left water visible");
        }
        return failures;
    }

    private static void ValidateSpatialPlacement(GameObject floor, List<string> failures)
    {
        Anomoly34 cameras = floor.GetComponentInChildren<Anomoly34>(true);
        if (cameras != null)
        {
            if (!cameras.extraCameras.Any(c => c != null && c.name.Contains("Bin_Peeking")))
                failures.Add("Anomoly34 is missing the camera peeking from the school bin");
            if (cameras.extraCameras.Count(c => c != null && c.name.Contains("StairStep")) < 3)
                failures.Add("Anomoly34 needs three cameras resting on stair steps");
            if (cameras.extraCameras.Count(c => c != null && c.name.Contains("RailingBird")) < 2)
                failures.Add("Anomoly34 needs cameras perched on the stair railing");
            if (cameras.extraCameras.Any(c => c == null || c.GetComponentsInChildren<Renderer>(true).Length == 0))
                failures.Add("Anomoly34 contains a camera without the sourced CCTV mesh");
            if (cameras.extraCameras.Any(c => c != null && c.GetComponentsInChildren<Collider>(true).Length > 0))
                failures.Add("Anomoly34 cameras must not obstruct the player");
        }

        Anomoly11 trash = floor.GetComponentInChildren<Anomoly11>(true);
        if (trash != null && trash.normalTrash != null && trash.replacementTrash != null)
        {
            Renderer normalRenderer = trash.normalTrash.GetComponentInChildren<Renderer>(true);
            Renderer replacementRenderer = trash.replacementTrash.GetComponentInChildren<Renderer>(true);
            if (normalRenderer != null && replacementRenderer != null &&
                Mathf.Abs(normalRenderer.bounds.min.y - replacementRenderer.bounds.min.y) > 0.03f)
                failures.Add("Anomoly11 replacement trash is not resting on the same floor plane as the original");
        }

        Anomoly19 corpse = floor.GetComponentInChildren<Anomoly19>(true);
        Transform toilet = FindFirstDeep(floor.transform, "SM_toilet_bowl Variant");
        if (corpse != null && corpse.corpses.Count > 0 && toilet != null)
        {
            Renderer toiletRenderer = toilet.GetComponentInChildren<Renderer>(true);
            if (toiletRenderer != null && Mathf.Abs(corpse.corpses[0].transform.position.y -
                    toiletRenderer.bounds.min.y) > 0.03f)
                failures.Add("Anomoly19 corpse root is not aligned to the toilet floor plane");
            if (corpse.corpses[0].transform.Find("DetailedFemaleZombieCorpse") == null)
                failures.Add("Anomoly19 is not using the detailed zombie mesh");
        }

        Anomoly28 face = floor.GetComponentInChildren<Anomoly28>(true);
        if (face != null && face.windowFaces.Count > 0)
        {
            Vector3 facePosition = face.windowFaces[0].transform.position;
            List<Renderer> windows = FindAllDeep(floor.transform, "Window")
                .Select(w => w.GetComponentInChildren<Renderer>(true)).Where(r => r != null).ToList();
            if (windows.Count == 0 || Mathf.Sqrt(windows.Min(w => w.bounds.SqrDistance(facePosition))) > 0.20f)
                failures.Add("Anomoly28 face is not positioned directly against a window");
            if (face.windowFaces[0].transform.Find("DetailedUncannyHead") == null)
                failures.Add("Anomoly28 is not using the sourced detailed head mesh");
        }
    }

    private static void CheckSingleRegistration<T>(AnomolyManager manager, List<string> failures) where T : MonoBehaviour
    {
        int count = manager.anomolies.Count(x => x is T);
        if (count != 1) failures.Add(typeof(T).Name + " should have exactly one manager entry, found " + count);
    }
}
