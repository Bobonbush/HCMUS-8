using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class NghiTeacherSetup
{
    const string Root="Assets/Art/NghiAnomalies/Rocketbox/Teacher/";
    static NghiTeacherSetup(){EditorApplication.delayCall+=Request;}
    static void Request()
    {
        if(!File.Exists("Temp/NghiTeacher.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("Temp/NghiTeacher.request");
        try{Install();}catch(Exception e){File.WriteAllText("Temp/NghiTeacherReport.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Install Main Teacher Appearance")]
    public static void Install()
    {
        var log=new List<string>();var floor=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Floor.prefab");
        try
        {
            var a=floor.GetComponentInChildren<Anomoly17>(true);var npc=a.npc;
            var existing=npc.transform.Find("TeacherAppearance");if(existing!=null)Object.DestroyImmediate(existing.gameObject);
            // Keep the existing NPC root, AI, robot rig and collider references intact.
            foreach(var r in npc.GetComponentsInChildren<Renderer>(true))
                if(!r.transform.IsChildOf(a.alternateAppearance.transform))r.enabled=false;
            var wrapper=new GameObject("TeacherAppearance");wrapper.transform.SetParent(npc.transform,false);
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Male_Adult_08.fbx"),wrapper.transform);
            model.name="TeacherModel";
            foreach(var c in model.GetComponentsInChildren<Animator>(true))Object.DestroyImmediate(c);
            foreach(var c in model.GetComponentsInChildren<Animation>(true))Object.DestroyImmediate(c);
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                r.sharedMaterials=r.sharedMaterials.Select(m=>MaterialFor(m.name)).ToArray();
                r.updateWhenOffscreen=true; Shape(r,model,log);
            }
            var idle=NghiSubtleAnomalies.RetargetMotion(Root+"m_idle_breathe_01.max.fbx",Root+"Idle.anim","Idle",model,log);
            var walk=NghiSubtleAnomalies.RetargetMotion(Root+"m_walk_neutral.max.fbx",Root+"Walk.anim","Walk",model,log);
            idle.SampleAnimation(model,0);
            var b=NghiSubtleAnomalies.BoundsOf(model);wrapper.transform.localScale*=1.76f/b.size.y;
            b=NghiSubtleAnomalies.BoundsOf(model);
            wrapper.transform.position+=new Vector3(npc.transform.position.x-b.center.x,npc.transform.position.y-b.min.y,npc.transform.position.z-b.center.z);
            var anim=model.AddComponent<Animation>();anim.AddClip(idle,"Idle");anim.AddClip(walk,"Walk");
            anim.playAutomatically=false;anim.cullingType=AnimationCullingType.AlwaysAnimate;
            var driver=wrapper.AddComponent<NPCVisualAnimator>();driver.motion=anim;driver.agent=npc.GetComponent<NavMeshAgent>();
            driver.visual=model.GetComponentInChildren<Renderer>();driver.referenceWalkSpeed=NghiSubtleAnomalies.MeasureStride(model,walk,log);
            idle.SampleAnimation(model,0);
            a.normalRenderers=model.GetComponentsInChildren<Renderer>(true);
            NghiSubtleAnomalies.Preview(wrapper,idle,"TeacherIdle",0,"TeacherModel");
            NghiSubtleAnomalies.Preview(wrapper,walk,"TeacherWalk",walk.length*.25f,"TeacherModel");
            a.Evaluate();
            if(a.normalRenderers.Any(r=>r.enabled))throw new Exception("Teacher overlaps female anomaly");
            a.Restore();
            if(a.normalRenderers.Any(r=>!r.enabled))throw new Exception("Teacher did not restore");
            log.Add("PASS: same NPC root, teacher visible normally, female swap and restore work");
            var water=floor.GetComponentInChildren<Anomoly21>(true);var source=water.toiletJets[0].transform.position;
            foreach(var t in floor.GetComponentsInChildren<Transform>(true))
            {
                var p=t.position;var d=Vector3.Distance(new Vector3(p.x,0,p.z),new Vector3(source.x,0,source.z));
                if(d<9 && (t.name.ToLower().Contains("door")||t.name.ToLower().Contains("toilet")||t.name.ToLower().Contains("wall")))
                    log.Add("NEAR WATER "+AnimationUtility.CalculateTransformPath(t,floor.transform)+" local="+floor.transform.InverseTransformPoint(p));
            }
            log.Add("Water source local="+floor.transform.InverseTransformPoint(source));
            PrefabUtility.SaveAsPrefabAsset(floor,"Assets/Prefabs/Floor.prefab");AssetDatabase.SaveAssets();
            File.WriteAllLines("Temp/NghiTeacherReport.txt",log);
        }
        finally{PrefabUtility.UnloadPrefabContents(floor);}
    }
    static Material MaterialFor(string name)
    {
        string key=name.Contains("opacity")?"opacity":name.Contains("head")?"head":"body";
        string path=Root+key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",Color.white);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"m014_"+key+"_color.tga"));m.SetFloat("_Smoothness",.15f);
        if(key=="opacity"){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.2f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
        else{m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"m014_"+key+"_normal.tga"));m.SetFloat("_BumpScale",.6f);m.EnableKeyword("_NORMALMAP");}
        EditorUtility.SetDirty(m);return m;
    }
    static void Shape(SkinnedMeshRenderer r,GameObject model,List<string> log)
    {
        var mesh=Object.Instantiate(r.sharedMesh);var vertices=mesh.vertices;
        var matrix=model.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;
        var points=vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
        float min=points.Min(p=>p.y),height=points.Max(p=>p.y)-min;
        float changed=0;
        for(int i=0;i<points.Length;i++)
        {
            var p=points[i];float y=(p.y-min)/height;
            float waist=Mathf.Exp(-Mathf.Pow((y-.55f)/.095f,2));
            float centre=Mathf.Clamp01(1-Mathf.Abs(p.x)/(height*.19f));
            // A fuller waist and modest rounded belly, rather than enlarged arms/chest.
            p.x*=1+.14f*waist;
            float front=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-height*.025f,height*.05f,p.z));
            p.z+=height*.035f*waist*centre*front;
            changed=Mathf.Max(changed,(p-points[i]).magnitude);
            vertices[i]=matrix.inverse.MultiplyPoint3x4(p);
        }
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Root+"TeacherBuild.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}
        r.sharedMesh=mesh;log.Add("Teacher modest belly/waist shaping, max local displacement="+changed);
    }
}
