using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class NghiAnomalyRefinement
{
    const string Art = "Assets/Art/NghiAnomalies/";
    const string Request = "Temp/NghiRefinement.request";
    static NghiAnomalyRefinement() { EditorApplication.delayCall += Requested; }
    static void Requested()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Install(); } catch (Exception e) { File.WriteAllText("Temp/NghiRefinementReport.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Refine Nghi Anomalies")]
    public static void Install()
    {
        // Remove only empty root objects left by the earlier edit-mode visibility probe.
        var cleanedScenes=new System.Collections.Generic.HashSet<UnityEngine.SceneManagement.Scene>();
        foreach(var t in Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.parent==null
            &&t.gameObject.scene.IsValid()&&t.gameObject.scene.isLoaded&&!EditorSceneManagerIsPreview(t.gameObject.scene)
            &&t.name.StartsWith("WindowFace_Cluster_")&&t.GetComponentsInChildren<Renderer>(true).Length==0).ToArray())
        {cleanedScenes.Add(t.gameObject.scene);Object.DestroyImmediate(t.gameObject);}
        foreach(var scene in cleanedScenes)
        {UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);}
        NghiFacePlacement.Save();
        var floor = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Floor.prefab");
        try
        {
            Trash(floor); Cameras(floor); Flood(floor); Faces(floor); Corpse(floor);
            var npc = floor.GetComponentInChildren<Anomoly15>(true);
            npc.followSpeed = 1.75f; npc.detectionRadius = 3.5f;
            PrefabUtility.SaveAsPrefabAsset(floor, "Assets/Prefabs/Floor.prefab");
            AssetDatabase.SaveAssets();
            var authoringScene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/FirstPerson.unity");
            if(authoringScene.IsValid()&&authoringScene.isLoaded)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(authoringScene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(authoringScene);
            }
            File.WriteAllText("Temp/NghiRefinementReport.txt", "Installed #11 billboard, #15 patrol encounter, #19 waiting pose, #21 flood, #28 anchored cluster, #34 gaze, #35 closed classroom.\n" + DateTime.Now);
        }
        finally { PrefabUtility.UnloadPrefabContents(floor); }
    }
    static bool EditorSceneManagerIsPreview(UnityEngine.SceneManagement.Scene scene)
    {return UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(scene);}
    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        var b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }
    static Transform Child(Transform parent, string name)
    {
        var existing = parent.Find(name); if (existing != null) return existing;
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
    }
    static Material Mat(string name, string shader, Color color)
    {
        string path = Art + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(material, path); }
        material.shader = Shader.Find(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material); return material;
    }
    static void Trash(GameObject floor)
    {
        var a = floor.GetComponentInChildren<Anomoly11>(true);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "OnlineModels/SchoolTrashcan_Realistic.png");
        importer.alphaIsTransparency = true; importer.mipmapEnabled = true; importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048; importer.SaveAndReimport();
        var mat = Mat("M_Nghi_RealisticBin", "Nghi/BillboardCutout", Color.white);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(importer.assetPath));
        mat.SetFloat("_AlphaClip", 1); mat.SetFloat("_Cutoff", 0.12f); mat.SetFloat("_Cull", 0);
        mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue = 2450;
        var root = a.replacementTrash;
        root.SetActive(false);
        if (root.GetComponent<AnomalyBillboard>() == null) root.AddComponent<AnomalyBillboard>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        { r.enabled = true; r.sharedMaterial = mat; r.transform.localScale = new Vector3(1.05f, 1.1f, 1); }
        var normal = BoundsOf(a.normalTrash);
        root.transform.position = new Vector3(normal.center.x, normal.min.y + 0.55f, normal.center.z);
    }
    static void Cameras(GameObject floor)
    {
        var a = floor.GetComponentInChildren<Anomoly34>(true); a.cameraHeads.Clear();
        foreach (var root in a.extraCameras)
        {
            var pivot = Child(root.transform, "TrackingHead");
            if (pivot.childCount == 0)
            {
                var model = root.transform.Find("OnlineCCTVModel");
                if (model != null) model.SetParent(pivot, true);
            }
            a.cameraHeads.Add(pivot);
            var visual=pivot.Find("OnlineCCTVModel");
            if(visual!=null&&root.transform.Find("StationaryMount")==null)
            {
                var filter=visual.GetComponentInChildren<MeshFilter>(true);
                if(filter!=null&&filter.sharedMesh.isReadable)
                {
                    var source=filter.sharedMesh;
                    var mount=Object.Instantiate(visual.gameObject,root.transform,false);mount.name="StationaryMount";
                    mount.transform.SetPositionAndRotation(visual.position,visual.rotation);
                    mount.transform.localScale=visual.localScale;
                    mount.GetComponentInChildren<MeshFilter>(true).sharedMesh=CameraPart(source,false);
                    filter.sharedMesh=CameraPart(source,true);
                }
            }
        }
    }
    static Mesh CameraPart(Mesh source,bool head)
    {
        string path=Art+(head?"CameraHead":"CameraMount")+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        mesh=Object.Instantiate(source);mesh.name=head?"CCTV rotating housing":"CCTV fixed bracket";
        var vertices=source.vertices;
        for(int sub=0;sub<source.subMeshCount;sub++)
        {
            var input=source.GetTriangles(sub);var output=new System.Collections.Generic.List<int>();
            for(int i=0;i<input.Length;i+=3)
            {
                var centre=(vertices[input[i]]+vertices[input[i+1]]+vertices[input[i+2]])/3;
                bool housing=centre.y>-0.64f&&centre.x>-1.6f;
                if(housing!=head)continue;
                output.Add(input[i]);output.Add(input[i+1]);output.Add(input[i+2]);
            }
            mesh.SetTriangles(output,sub);
        }
        mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh Grid()
    {
        const string path = Art + "FloodGrid.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        const int n = 160;
        var vertices = new Vector3[(n+1)*(n+1)]; var uv = new Vector2[vertices.Length];
        var triangles = new int[n*n*6];
        for (int z=0; z<=n; z++) for (int x=0; x<=n; x++)
        { int i=z*(n+1)+x; vertices[i]=new Vector3(x/(float)n-0.5f,0,z/(float)n-0.5f); uv[i]=new Vector2(x/(float)n,z/(float)n); }
        int t=0;
        for (int z=0; z<n; z++) for (int x=0; x<n; x++)
        { int i=z*(n+1)+x; triangles[t++]=i; triangles[t++]=i+n+1; triangles[t++]=i+1; triangles[t++]=i+1; triangles[t++]=i+n+1; triangles[t++]=i+n+2; }
        mesh=new Mesh { name="Flood wave grid", vertices=vertices, uv=uv, triangles=triangles };
        mesh.RecalculateNormals(); mesh.bounds=new Bounds(Vector3.zero,new Vector3(1,20,1));
        AssetDatabase.CreateAsset(mesh,path); return mesh;
    }
    public static void Flood(GameObject floor)
    {
        var a=floor.GetComponentInChildren<Anomoly21>(true); a.riseHeight=0.035f; a.riseDuration=240f; a.onsetDelay=18f; a.spreadSpeed=0.028f; a.maxSpreadRadius=4.5f; a.rippleHeight=0.0015f;
        var mat=Mat("M_Nghi_FloodWaves","Nghi/Flood",new Color(0.035f,0.11f,0.12f,0.9f));
        foreach (var water in a.waterSurfaces)
        {
            water.GetComponent<MeshFilter>().sharedMesh=Grid(); water.GetComponent<Renderer>().sharedMaterial=mat;
            foreach(var c in water.GetComponents<Collider>()) Object.DestroyImmediate(c);
            water.gameObject.SetActive(false);
        }
        mat.SetFloat("_WaveHeight",a.rippleHeight); mat.SetFloat("_FloodRadius",0); mat.SetFloat("_FlowSpeed",0.22f); mat.SetFloat("_FoamStrength",0.025f);
        var effects=Child(a.transform,"ToiletOutflow");
        var splash=Mat("M_Nghi_Foam","Nghi/Splash",new Color(0.6f,0.77f,0.76f,0.55f));
        if (effects.childCount==0)
        {

            foreach(var toilet in floor.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="SM_toilet_bowl Variant"))
            {
                var jet=Child(effects,"Bowl jet "+a.toiletJets.Count); var b=BoundsOf(toilet.gameObject);
                jet.position=new Vector3(b.center.x,b.max.y-0.08f,b.center.z); jet.rotation=Quaternion.LookRotation(toilet.forward+Vector3.up*0.7f);
                var ps=jet.gameObject.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main; main.playOnAwake=false; main.loop=true; main.startLifetime=new ParticleSystem.MinMaxCurve(0.45f,1.1f);
                main.startSpeed=new ParticleSystem.MinMaxCurve(1.8f,3.7f); main.startSize=new ParticleSystem.MinMaxCurve(0.025f,0.11f);
                main.gravityModifier=1; main.maxParticles=250; main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=ps.emission; emission.rateOverTime=100;
                var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.angle=24; shape.radius=0.14f;
                var collision=ps.collision; collision.enabled=true; collision.type=ParticleSystemCollisionType.World; collision.bounce=0.15f;
                var r=ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial=splash; r.renderMode=ParticleSystemRenderMode.Stretch; r.lengthScale=1.5f;
                a.toiletJets.Add(ps); jet.gameObject.SetActive(false);
            }
        }
        foreach(var ps in a.toiletJets)
        {
            var main=ps.main; main.startSpeed=new ParticleSystem.MinMaxCurve(0.06f,0.15f); main.startSize=new ParticleSystem.MinMaxCurve(0.004f,0.012f); main.startLifetime=0.45f;
            var emission=ps.emission; emission.rateOverTime=8; var shape=ps.shape; shape.radius=0.025f; shape.angle=6;
        }
        // The original corridor mesh ended short of the bowl. Give the leak its own
        // small footprint centred on the source so the first drops are visible there.
        if(a.toiletJets.Count>0)
        foreach(var water in a.waterSurfaces)
        {
            var source=a.toiletJets[0].transform.position;
            water.position=new Vector3(source.x,water.position.y,source.z);
            water.localScale=new Vector3(9.2f,0.025f,9.2f);
        }
        if(a.waterRoar==null)
        {
            var sound=Child(a.transform,"RoaringDrain");
            if(a.toiletJets.Count>0) sound.position=a.toiletJets[0].transform.position;
            a.waterRoar=sound.gameObject.AddComponent<AudioSource>();
        }
        a.waterRoar.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Art+"FloodRoar.wav");
        a.waterRoar.loop=true; a.waterRoar.playOnAwake=false; a.waterRoar.spatialBlend=1f; a.waterRoar.volume=0.08f; a.waterRoar.minDistance=0.5f; a.waterRoar.maxDistance=5;
    }
    static void Faces(GameObject floor)
    {
        var w=floor.GetComponentInChildren<Anomoly28>(true); w.faceCount=Mathf.Max(11,w.faceCount);
        // Keep the exact authored root. Recess the mesh within it, leaving the anchor editable.
        var anchor=w.windowFaces.First(f=>f!=null).transform;
        if(anchor.Find("GlassClearanceApplied")==null)
        {
            foreach(Transform child in anchor) child.position-=anchor.forward*0.35f;
            Child(anchor,"GlassClearanceApplied");
        }
        var a=floor.GetComponentInChildren<Anomoly35>(true);
        var door=floor.transform.Find("Elevator/ClassRoomDoor (3)");
        if(door==null) throw new Exception("Closed classroom door not found");
        var db=BoundsOf(door.gameObject); a.doorway=door;
        var room=Child(a.transform,"ClosedRoomAnchor");
        room.position=new Vector3(db.center.x, floor.transform.position.y, db.center.z);
        room.rotation=Quaternion.LookRotation(-floor.transform.right);
        a.roomAnchor=room; a.roomSize=new Vector3(3.6f,3.1f,5f);
        for(int i=0;i<a.classroomFaces.Count;i++)
        {
            var face=a.classroomFaces[i];
            float x=(i%3-1)*0.8f;
            float z=i<3 ? 0.65f : 1.5f+(i/3)*0.65f;
            face.transform.position=room.TransformPoint(new Vector3(x,1.2f+(i*23%50)/100f,z));
            face.transform.rotation=Quaternion.LookRotation(-room.forward);
            face.transform.localScale=Vector3.one*(0.7f+(i*17%70)/100f);
            face.SetActive(false);
        }
        // Physical room envelope prevents rolling faces from escaping through decorative meshes.
        var barriers=Child(a.transform,"FacePhysicsRoom");
        a.physicsRoom=barriers.gameObject;
        Box(barriers,"Threshold",room,new Vector3(0,1.5f,-0.03f),new Vector3(3.8f,3.2f,0.12f));
        Box(barriers,"Back",room,new Vector3(0,1.5f,5.1f),new Vector3(3.8f,3.2f,0.1f));
        Box(barriers,"Left",room,new Vector3(-1.9f,1.5f,2.5f),new Vector3(0.1f,3.2f,5));
        Box(barriers,"Right",room,new Vector3(1.9f,1.5f,2.5f),new Vector3(0.1f,3.2f,5));
        Box(barriers,"Floor",room,new Vector3(0,-0.06f,2.5f),new Vector3(3.8f,0.1f,5));
        barriers.gameObject.SetActive(false);
    }
    static void Box(Transform parent,string name,Transform room,Vector3 offset,Vector3 size)
    {
        var t=Child(parent,name); t.SetPositionAndRotation(room.TransformPoint(offset),room.rotation);
        var c=t.GetComponent<BoxCollider>(); if(c==null)c=t.gameObject.AddComponent<BoxCollider>(); c.size=size;
    }
    static void Corpse(GameObject floor)
    {
        var a=floor.GetComponentInChildren<Anomoly19>(true); var root=a.corpses[0].transform;
        if(root.Find("WaitingStudent")!=null){CorrectWaitingStudent(floor,root.Find("WaitingStudent"));WaitingSeat(root,root.Find("WaitingStudent"));return;}
        string path=Art+"OnlineModels/NPCWoman.glb";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var model=Object.Instantiate(source,root,false); model.name="WaitingStudent";
        var idle=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name.EndsWith("|Idle")||c.name=="Idle");
        idle.SampleAnimation(model,0);
        model.transform.localScale*=1.72f/BoundsOf(model).size.y;
        var bones=model.GetComponentsInChildren<Transform>(true);
        Func<string,Transform> bone=n=>bones.First(t=>t.name==n);
        var forward=model.transform.forward;
        Aim(bone("UpperLeg.L"),bone("LowerLeg.L"),forward+Vector3.down*0.3f);
        Aim(bone("UpperLeg.R"),bone("LowerLeg.R"),forward+model.transform.right*0.2f+Vector3.down*0.3f);
        Aim(bone("LowerLeg.L"),bone("Foot.L"),Vector3.down);
        Aim(bone("LowerLeg.R"),bone("Foot.R"),Vector3.down);
        Aim(bone("UpperArm.L"),bone("LowerArm.L"),Vector3.down+forward*0.3f);
        Aim(bone("UpperArm.R"),bone("LowerArm.R"),Vector3.down+forward*0.3f);
        Aim(bone("LowerArm.L"),bone("Wrist.L"),forward+Vector3.down*0.3f);
        Aim(bone("LowerArm.R"),bone("Wrist.R"),forward+Vector3.down*0.3f);
        bone("Neck").localRotation*=Quaternion.Euler(-12,0,18);
        foreach(var animator in model.GetComponentsInChildren<Animator>(true))Object.DestroyImmediate(animator);
        foreach(var animation in model.GetComponentsInChildren<Animation>(true))Object.DestroyImmediate(animation);
        foreach(var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh=new Mesh(); skinned.BakeMesh(mesh);
            string mp=Art+"WaitingStudent_"+skinned.name+".asset";
            AssetDatabase.CreateAsset(mesh,mp);
            var materials=skinned.sharedMaterials;
            var go=skinned.gameObject; Object.DestroyImmediate(skinned);
            go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterials=materials;
        }
        var toilet=floor.GetComponentsInChildren<Transform>(true).First(t=>t.name=="SM_toilet_bowl Variant");
        var tb=BoundsOf(toilet.gameObject);
        var b=BoundsOf(model);
        model.transform.position+=new Vector3(tb.max.x+0.15f-b.min.x,tb.min.y+0.02f-b.min.y,tb.center.z-b.center.z);
        foreach(Transform child in root)if(child!=model.transform)child.gameObject.SetActive(false);
        CorrectWaitingStudent(floor,model.transform);
        WaitingSeat(root,model.transform);
        root.gameObject.SetActive(false);
    }
    static void CorrectWaitingStudent(GameObject floor,Transform model)
    {
        // glTF animation/BakeMesh can retain a 100x armature scale. Normalize the baked result.
        var b=BoundsOf(model.gameObject);
        if(b.size.y>1.6f||b.size.y<1.1f)model.localScale*=1.35f/Mathf.Max(0.001f,b.size.y);
        var toilet=floor.GetComponentsInChildren<Transform>(true).First(t=>t.name=="SM_toilet_bowl Variant");
        var tb=BoundsOf(toilet.gameObject);b=BoundsOf(model.gameObject);
        model.position+=new Vector3(tb.max.x+0.15f-b.min.x,tb.min.y+0.02f-b.min.y,tb.center.z-b.center.z);
    }
    static void WaitingSeat(Transform root,Transform model)
    {
        foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
        {
            bool skin=renderer.name.Contains("Head");bool shirt=renderer.name.Contains("Body");
            var color=skin?new Color(0.45f,0.48f,0.42f):shirt?new Color(0.66f,0.65f,0.57f):new Color(0.055f,0.065f,0.085f);
            var mat=Mat(skin?"M_Nghi_StudentSkin":shirt?"M_Nghi_StudentShirt":"M_Nghi_StudentUniform","Universal Render Pipeline/Lit",color);
            mat.SetFloat("_Smoothness",0.16f);
            renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>mat).ToArray();
        }
        var body=model.GetComponentsInChildren<MeshRenderer>(true).First(t=>t.name.Contains("Body")).bounds;
        var centre=new Vector3(body.center.x,body.min.y+0.035f,body.center.z);
        var stool=Child(root,"WaitingStool");stool.position=centre;stool.rotation=model.rotation;
        while(stool.childCount>0)Object.DestroyImmediate(stool.GetChild(0).gameObject);
        var material=Mat("M_Nghi_WaitingStool","Universal Render Pipeline/Lit",new Color(0.12f,0.085f,0.06f));
        var seat=GameObject.CreatePrimitive(PrimitiveType.Cube);seat.name="Worn wooden seat";seat.transform.SetParent(stool,false);
        seat.transform.localScale=new Vector3(0.39f,0.055f,0.36f);seat.GetComponent<Renderer>().sharedMaterial=material;
        Object.DestroyImmediate(seat.GetComponent<Collider>());
        float floorY=BoundsOf(model.gameObject).min.y;
        float height=Mathf.Max(0.1f,stool.position.y-floorY);
        for(int i=0;i<4;i++)
        {
            var leg=GameObject.CreatePrimitive(PrimitiveType.Cube);leg.name="Stool leg";leg.transform.SetParent(stool,false);
            leg.transform.localPosition=new Vector3(i%2==0?-0.14f:0.14f,-height*0.5f,i<2?-0.13f:0.13f);
            leg.transform.localScale=new Vector3(0.035f,height,0.035f);leg.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(leg.GetComponent<Collider>());
        }
    }
    static void Aim(Transform joint,Transform next,Vector3 direction)
    { joint.rotation=Quaternion.FromToRotation(next.position-joint.position,direction.normalized)*joint.rotation; }
}
