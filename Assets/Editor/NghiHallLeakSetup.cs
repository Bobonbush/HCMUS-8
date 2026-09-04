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
public static class NghiHallLeakSetup
{
    static NghiHallLeakSetup(){EditorApplication.delayCall+=Request;}
    static void Request()
    {
        if(!File.Exists("Temp/NghiHallLeak.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("Temp/NghiHallLeak.request");
        try{Install();}catch(Exception e){File.WriteAllText("Temp/NghiHallLeakReport.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Install Restroom Hall Seep")]
    public static void Install()
    {
        var log=new List<string>();var floor=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Floor.prefab");
        try
        {
            Configure(floor,log);
            // An earlier authored deletion left one paired null slot in #34.
            var cameras=floor.GetComponentInChildren<Anomoly34>(true);
            for(int i=cameras.extraCameras.Count-1;i>=0;i--)
                if(cameras.extraCameras[i]==null)
                {cameras.extraCameras.RemoveAt(i);if(i<cameras.cameraHeads.Count)cameras.cameraHeads.RemoveAt(i);}
            var a=floor.GetComponentInChildren<Anomoly21>(true);
            var elapsed=typeof(Anomoly21).GetField("elapsed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var tick=typeof(Anomoly21).GetMethod("LateUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            a.Evaluate();
            foreach(float time in new[]{0f,20f,40f,65f,100f,600f})
            {
                elapsed.SetValue(a,time);tick.Invoke(a,null);
                var block=new MaterialPropertyBlock();a.waterSurfaces[0].GetComponent<Renderer>().GetPropertyBlock(block);
                float travel=block.GetFloat("_FloodRadius"),depth=block.GetFloat("_FloodDepth");
                var source=a.toiletJets[0].transform.position;source.y=a.restroomExit.position.y;
                float exitDistance=Vector3.Distance(source,a.restroomExit.position);
                if(depth>.0401f)throw new Exception("Exceeded shallow water depth");
                log.Add("t="+time+"s depth="+depth+"m, doorway reached="+(travel>exitDistance)+", hallway travel="+Mathf.Max(0,travel-exitDistance));
                if(time==65 && travel-exitDistance<2)throw new Exception("Hall puddle develops too late");
                if(time==65)Preview(floor,a);
            }
            a.Restore();
            if(a.waterSurfaces.Any(t=>t.gameObject.activeSelf))throw new Exception("Restore leaves water enabled");
            if(ShaderUtil.ShaderHasError(a.waterSurfaces[0].GetComponent<Renderer>().sharedMaterial.shader))throw new Exception("Water shader failed compile");
            log.Add("PASS: shallow cap, hall timing, doorway routing, shader, restore");
            PrefabUtility.SaveAsPrefabAsset(floor,"Assets/Prefabs/Floor.prefab");AssetDatabase.SaveAssets();
            File.WriteAllLines("Temp/NghiHallLeakReport.txt",log);
        }
        finally{PrefabUtility.UnloadPrefabContents(floor);}
    }
    public static void Configure(GameObject floor,List<string> log=null)
    {
        var a=floor.GetComponentInChildren<Anomoly21>(true);
        a.riseHeight=.035f;a.riseDuration=150;a.onsetDelay=8;a.spreadSpeed=.14f;a.maxSpreadRadius=12;a.rippleHeight=.0015f;
        var door=floor.transform.Find("Elevator/Door_G_Door (4)");
        if(door==null)throw new Exception("Restroom entrance missing");
        var exit=a.transform.Find("RestroomExitFlow");
        if(exit==null){exit=new GameObject("RestroomExitFlow").transform;exit.SetParent(a.transform,false);}
        var b=NghiSubtleAnomalies.BoundsOf(door.gameObject);
        // This door is swung open into the hall: its hinge sits at the lower jamb.
        // Use the centre of the opening, not the swung leaf's bounding-box centre.
        float openingWidth=Mathf.Clamp(b.size.x,1,1.8f);
        exit.position=new Vector3(door.position.x,floor.transform.position.y+.02f,door.position.z+openingWidth*.5f);
        exit.rotation=Quaternion.LookRotation(floor.transform.TransformDirection(Vector3.left),Vector3.up);
        a.restroomExit=exit;a.doorwayWidth=openingWidth;
        a.roomLimits=new Vector4(-.95f,2.1f,-5.8f,.15f);
        a.hallLimits=new Vector4(-.95f,9,-.15f,3.7f);
        // Keep the wet surface above the real walking plane, including threshold trim.
        float floorY=floor.transform.position.y+.015f;
        var ray=new Ray(exit.position+exit.forward*1.1f+Vector3.up*.35f,Vector3.down);
        foreach(var c in floor.GetComponentsInChildren<Collider>(true))
        {
            if(c.isTrigger || !c.enabled)continue;
            if(c.Raycast(ray,out var hit,.7f) && hit.normal.y>.7f && hit.point.y<floor.transform.position.y+.2f)
                floorY=Mathf.Max(floorY,hit.point.y+.004f);
        }
        foreach(var water in a.waterSurfaces)
        {
            water.position=exit.position+exit.right*3.5f-exit.forward*1.5f;
            water.position=new Vector3(water.position.x,floorY,water.position.z);
            water.localScale=new Vector3(12,.025f,15);water.gameObject.SetActive(false);
        }
        if(a.waterRoar!=null){a.waterRoar.volume=.14f;a.waterRoar.maxDistance=7;a.waterRoar.minDistance=.7f;}
        log?.Add("Restroom exit="+floor.transform.InverseTransformPoint(exit.position)+", width="+a.doorwayWidth+", water base="+(floorY-floor.transform.position.y));
        log?.Add("Door renderer bounds="+b);
    }
    static void Preview(GameObject floor,Anomoly21 a)
    {
        var scene=floor.scene;var disabled=new List<Renderer>();
        foreach(var r in floor.GetComponentsInChildren<Renderer>())
            if(r.enabled && r.bounds.min.y>floor.transform.position.y+2.7f && !r.transform.IsChildOf(a.transform)) {r.enabled=false;disabled.Add(r);}
        var rt=new RenderTexture(1000,800,24);var old=RenderTexture.active;GameObject go=null,light=null;
        try
        {
            go=new GameObject("Leak preview camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
            var cam=go.AddComponent<Camera>();cam.scene=scene;cam.cameraType=CameraType.Preview;cam.targetTexture=rt;cam.orthographic=true;cam.orthographicSize=6;
            cam.GetUniversalAdditionalCameraData().requiresColorOption=CameraOverrideOption.On;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.17f,.18f);
            cam.transform.position=a.restroomExit.position+a.restroomExit.right*3+Vector3.up*12;cam.transform.rotation=Quaternion.Euler(90,0,0);
            light=new GameObject("Leak preview light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light,scene);light.AddComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=2;light.transform.rotation=Quaternion.Euler(60,30,0);
            cam.Render();RenderTexture.active=rt;var image=new Texture2D(1000,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,800),0,0);image.Apply();File.WriteAllBytes("Temp/HallLeak65seconds.png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{foreach(var r in disabled)r.enabled=true;RenderTexture.active=old;Object.DestroyImmediate(rt);if(go!=null)Object.DestroyImmediate(go);if(light!=null)Object.DestroyImmediate(light);}
    }
}
