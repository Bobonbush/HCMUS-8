using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class NghiRefinementValidation
{
    static NghiRefinementValidation() { EditorApplication.delayCall+=Requested; }
    static void Requested()
    {
        if(!File.Exists("Temp/NghiValidateRefinement.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("Temp/NghiValidateRefinement.request"); Run();
    }
    [MenuItem("Tools/Validate Refined Anomalies")]
    public static void Run()
    {
        var log=new List<string>();
        foreach(var m in Resources.FindObjectsOfTypeAll<AnomolyManager>())
            log.Add("Floor context: "+m.name+" scene="+m.gameObject.scene.path+" loaded="+m.gameObject.scene.isLoaded+" source="+AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(m.gameObject)));
        foreach(var t in Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.parent==null&&t.name.StartsWith("WindowFace_Cluster_")))
            log.Add("Temporary context: "+t.name+" scene="+t.gameObject.scene.path+" loaded="+t.gameObject.scene.isLoaded+" preview="+EditorSceneManager.IsPreviewScene(t.gameObject.scene));
        Action<string,Action> check=(name,test)=>{try{test();log.Add("PASS: "+name);}catch(Exception e){log.Add("FAIL: "+name+" "+e);}};
        var tests=new NghiAnomalyTests();
        check("Registered prefab wiring",tests.FloorPrefab_HasEveryNghiAnomalyRegisteredAndWired);
        check("Visibility restoration",tests.VisibilityAnomalies_EvaluateAndRestoreAreExactInverses);
        check("Trash swap",tests.TrashSwap_EvaluateAndRestoreSelectExactlyOneModel);
        var floor=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Floor.prefab");
        try
        {
            var w=floor.GetComponentInChildren<Anomoly28>(true);
            check("Window anchor unchanged; requested face count; restore removes generated visibility",()=>{
                var t=w.windowFaces[0].transform;var pos=t.localPosition;var rot=t.localRotation;var scale=t.localScale;
                w.Evaluate();
                Require(t.localPosition==pos&&t.localRotation==rot&&t.localScale==scale,"Authored anchor moved");
                Require(t.parent.GetComponentsInChildren<Transform>(true).Count(x=>x.name.StartsWith("WindowFace_Cluster_"))==w.faceCount-1,"Wrong cluster count");
                w.Restore();
                Require(!t.gameObject.activeSelf,"Original still active");
                w.Evaluate();
                Require(t.parent.GetComponentsInChildren<Transform>(true).Count(x=>x.name.StartsWith("WindowFace_Cluster_"))==w.faceCount-1,"Repeated evaluation leaked copies");
                w.Restore();
            });
            check("Flood shader and references",()=>{
                var a=floor.GetComponentInChildren<Anomoly21>(true);
                Require(a.riseHeight<=0.04f && a.riseDuration>=150 && a.restroomExit!=null && a.spreadSpeed>=0.1f,"Shallow hallway seep defaults");
                Require(a.toiletJets.Count>0&&a.waterRoar.clip!=null,"Missing effects");
                Require(!ShaderUtil.ShaderHasError(a.waterSurfaces[0].GetComponent<Renderer>().sharedMaterial.shader),"Water shader error");
            });
            check("Closed classroom containment and restoration",()=>{
                var a=floor.GetComponentInChildren<Anomoly35>(true);
                Require(a.doorway.name=="ClassRoomDoor (3)","Wrong room");
                foreach(var f in a.classroomFaces)Require(a.roomAnchor.InverseTransformPoint(f.transform.position).z>0.5f,"Face clips threshold");
                a.Evaluate();a.Restore();Require(!a.physicsRoom.activeSelf,"Physics barrier remained active");
            });
            check("Preview trash",()=>Render(floor.GetComponentInChildren<Anomoly11>(true).replacementTrash,"Trash",new Vector3(0,0,2)));
            check("Preview waiting corpse",()=>Render(floor.GetComponentInChildren<Anomoly19>(true).corpses[0],"Corpse",new Vector3(1,0.4f,2)));
            var corpse=floor.GetComponentInChildren<Anomoly19>(true).corpses[0];
            check("Seated corpse is human sized",()=>{
                var rs=corpse.transform.Find("WaitingStudent").GetComponentsInChildren<Renderer>(true);
                var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
                Require(b.size.y>1.1f&&b.size.y<1.6f&&b.size.x<1.5f&&b.size.z<1.5f,"Baked rig scale is wrong: "+b.size);
            });
            foreach(var r in corpse.GetComponentsInChildren<Renderer>(true))
                if(r.gameObject.activeSelf)log.Add("Corpse geometry: "+r.name+" bounds="+r.bounds+" active="+r.gameObject.activeInHierarchy);
            check("Preview camera",()=>Render(floor.GetComponentInChildren<Anomoly34>(true).extraCameras[0],"Camera",new Vector3(1,0.4f,2)));
            check("Preview face",()=>Render(w.windowFaces[0],"Face",new Vector3(0,0,2)));
        }
        finally{PrefabUtility.UnloadPrefabContents(floor);}
        File.WriteAllLines("Temp/NghiRefinementValidation.txt",log); Debug.Log(string.Join("\n",log));
    }
    static void Require(bool condition,string error){if(!condition)throw new Exception(error);}
    static void Render(GameObject source,string name,Vector3 offset)
    {
        var scene=EditorSceneManager.NewPreviewScene();var rt=new RenderTexture(720,720,24);var old=RenderTexture.active;Texture2D image=null;
        try
        {
            var copy=Object.Instantiate(source);SceneMove(copy,scene);copy.SetActive(true);copy.transform.position=Vector3.zero;copy.transform.rotation=Quaternion.identity;
            var rs=copy.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            var go=new GameObject("Preview Camera");SceneMove(go,scene);var camera=go.AddComponent<Camera>();camera.cameraType=CameraType.Preview;
            camera.scene=scene;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.11f,0.12f,0.14f);camera.fieldOfView=38;camera.targetTexture=rt;
            camera.transform.position=b.center+offset.normalized*b.size.magnitude*1.65f;camera.transform.LookAt(b.center);
            var light=new GameObject("Light");SceneMove(light,scene);var l=light.AddComponent<Light>();l.type=LightType.Directional;l.intensity=2;light.transform.rotation=Quaternion.Euler(30,155,0);
            camera.Render();RenderTexture.active=rt;image=new Texture2D(720,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,720,720),0,0);image.Apply();
            File.WriteAllBytes("Temp/NghiRefined"+name+".png",image.EncodeToPNG());
        }
        finally{RenderTexture.active=old;if(image!=null)Object.DestroyImmediate(image);Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void SceneMove(GameObject go,UnityEngine.SceneManagement.Scene scene){UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);}
}
