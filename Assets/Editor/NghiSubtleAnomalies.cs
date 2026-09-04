using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class NghiSubtleAnomalies
{
    const string Root = "Assets/Art/NghiAnomalies/Rocketbox/";
    static NghiSubtleAnomalies() { EditorApplication.delayCall += Requested; }
    static void Requested()
    {
        if (!File.Exists("Temp/NghiSubtle.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Temp/NghiSubtle.request");
        try { Install(); } catch (Exception e) { File.WriteAllText("Temp/NghiSubtleReport.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Install Subtle Leak and Walking Civilian")]
    public static void Install()
    {
        var log = new List<string>();
        var floor = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Floor.prefab");
        try
        {
            var a = floor.GetComponentInChildren<Anomoly17>(true);
            if (a.alternateAppearance != null) Object.DestroyImmediate(a.alternateAppearance);
            a.normalRenderers = a.npc.GetComponentsInChildren<Renderer>(true);
            var wrapper = new GameObject("Nghi17_FemaleAppearance");
            wrapper.transform.SetParent(a.npc.transform, false);
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Female_Adult_13.fbx"), wrapper.transform);
            model.name = "WomanModel";
            foreach (var animator in model.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(animator);
            foreach (var animation in model.GetComponentsInChildren<Animation>(true)) Object.DestroyImmediate(animation);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = r.sharedMaterials.Select(m => MaterialFor(m.name)).ToArray();
                if (r is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = true;
            }
            var idle = Motion("f_idle_breathe_01.max.fbx", "Idle", model, log);
            var walk = Motion("f_walk_neutral.max.fbx", "Walk", model, log);
            idle.SampleAnimation(model, 0);
            var b = BoundsOf(model);
            wrapper.transform.localScale *= 1.72f / b.size.y;
            b = BoundsOf(model);
            wrapper.transform.position += new Vector3(a.npc.transform.position.x - b.center.x,
                a.npc.transform.position.y - b.min.y, a.npc.transform.position.z - b.center.z);
            var anim = model.AddComponent<Animation>();
            anim.AddClip(idle,"Idle"); anim.AddClip(walk,"Walk");
            anim.playAutomatically = false; anim.cullingType = AnimationCullingType.AlwaysAnimate;
            a.alternateAppearance = wrapper; a.alternateAnimation = anim;
            a.idleClip = "Idle"; a.walkClip = "Walk";
            a.referenceWalkSpeed = MeasureStride(model, walk, log);
            idle.SampleAnimation(model, 0);
            Preview(wrapper, idle, "Idle", 0);
            Preview(wrapper, walk, "Walk", walk.length * 0.25f);
            wrapper.SetActive(false);
            NghiAnomalyRefinement.Flood(floor);
            var water = floor.GetComponentInChildren<Anomoly21>(true);
            ValidateLeak(water, log);
            a.Evaluate(); a.Restore();
            if (wrapper.activeSelf) throw new Exception("Appearance did not restore");
            log.Add("PASS: replacement visibility restores; model height=" + BoundsOf(model).size.y);
            PrefabUtility.SaveAsPrefabAsset(floor, "Assets/Prefabs/Floor.prefab");
            AssetDatabase.SaveAssets();
            File.WriteAllLines("Temp/NghiSubtleReport.txt",log);
        }
        finally { PrefabUtility.UnloadPrefabContents(floor); }
    }
    static Material MaterialFor(string name)
    {
        string key = name.Contains("opacity") ? "opacity" : name.Contains("head") ? "head" : "body";
        string path = Root + key + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.SetColor("_BaseColor",Color.white);
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "f019_" + key + "_color.tga"));
        mat.SetFloat("_Smoothness",0.17f);
        if (key == "opacity")
        {
            mat.SetFloat("_AlphaClip",1); mat.SetFloat("_Cutoff",0.2f); mat.SetFloat("_Cull",0);
            mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue=2450;
        }
        else
        {
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "f019_" + key + "_normal.tga"));
            mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale",0.65f);
        }
        EditorUtility.SetDirty(mat); return mat;
    }
    static AnimationClip Motion(string sourceName, string name, GameObject model, List<string> log)
        => RetargetMotion(Root + sourceName, Root + name + ".anim", name, model, log);
    public static AnimationClip RetargetMotion(string sourcePath, string path, string name, GameObject model, List<string> log)
    {
        var source = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
        var clip = new AnimationClip { name=name, legacy=true, wrapMode=WrapMode.Loop, frameRate=source.frameRate };
        int count=0;
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var bone = model.transform.Find(binding.path);
            if (bone == null || binding.type != typeof(Transform)) continue;
            // Preserve avatar proportions. The matching biped rotations provide the gait;
            // only the root's vertical bob is translated, leaving navigation in sole control of travel.
            var curve = AnimationUtility.GetEditorCurve(source,binding);
            if (binding.propertyName.StartsWith("m_LocalRotation"))
                AnimationUtility.SetEditorCurve(clip,binding,curve);
            else if (binding.path == "Bip01" && binding.propertyName == "m_LocalPosition.y")
            {
                float offset = bone.localPosition.y - curve.Evaluate(0);
                var keys = curve.keys;
                for(int i=0;i<keys.Length;i++) keys[i].value += offset;
                AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys));
            }
            else continue;
            count++;
        }
        clip.EnsureQuaternionContinuity();
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) AssetDatabase.CreateAsset(clip,path);
        else { EditorUtility.CopySerialized(clip,existing); Object.DestroyImmediate(clip); clip=existing; }
        log.Add(name + ": " + count + " bound bone curves, duration=" + clip.length);
        if(count < 100) throw new Exception("Motion is not bound to the avatar");
        return clip;
    }
    public static float MeasureStride(GameObject model, AnimationClip clip, List<string> log)
    {
        var feet=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Bip01 L Foot" || t.name=="Bip01 R Foot").ToArray();
        var samples=new List<Vector3[]>();
        for(int i=0;i<=40;i++)
        { clip.SampleAnimation(model,clip.length*i/40); samples.Add(feet.Select(t=>new Vector3(0,t.position.y,Vector3.Dot(t.position,model.transform.forward))).ToArray()); }
        float range= samples.Max(s=>s[0].z)-samples.Min(s=>s[0].z);
        float lift= samples.Max(s=>s[0].y)-samples.Min(s=>s[0].y);
        log.Add("Foot movement: forward range="+range+", lift="+lift);
        if(range < 0.1f || lift < 0.015f) throw new Exception("Walk does not move/lift feet");
        // Estimate planted-foot backwards velocity across the low part of each step.
        var speeds=new List<float>();
        for(int f=0;f<feet.Length;f++)
        {
            float min=samples.Min(s=>s[f].y);
            for(int i=1;i<samples.Count;i++)
            {
                var delta=samples[i][f]-samples[i-1][f];
                if(samples[i][f].y<min+0.035f && delta.z<0)
                    speeds.Add(-delta.z/(clip.length/40));
            }
        }
        float speed=speeds.Count>0?speeds.OrderBy(x=>x).ElementAt(speeds.Count/2):1.4f;
        speed=Mathf.Clamp(speed,0.6f,2);
        log.Add("PASS: organic leg motion; reference stride speed="+speed);
        return speed;
    }
    static void ValidateLeak(Anomoly21 a, List<string> log)
    {
        if (a.restroomExit==null) throw new Exception("Missing restroom-to-hall flow anchor");
        log.Add("PASS: restroom-to-hall flow anchor assigned");
        var elapsed=typeof(Anomoly21).GetField("elapsed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var update=typeof(Anomoly21).GetMethod("LateUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        a.Evaluate();
        foreach(float t in new[]{0f,30f,60f,120f,600f})
        {
            elapsed.SetValue(a,t); update.Invoke(a,null);
            var block=new MaterialPropertyBlock(); a.waterSurfaces[0].GetComponent<Renderer>().GetPropertyBlock(block);
            float depth=block.GetFloat("_FloodDepth"),radius=block.GetFloat("_FloodRadius");
            if(depth>0.0401f || radius>a.maxSpreadRadius+0.01f || (t==0 && a.waterSurfaces[0].gameObject.activeSelf)) throw new Exception("Leak exceeded bounds");
            log.Add("PASS: leak at "+t+" seconds, depth="+depth+", radius="+radius);
        }
        a.Restore();
        if(a.waterSurfaces.Any(t=>t.gameObject.activeSelf)) throw new Exception("Leak failed restore");
        if(ShaderUtil.ShaderHasError(a.waterSurfaces[0].GetComponent<Renderer>().sharedMaterial.shader)) throw new Exception("Water shader error");
    }
    public static Bounds BoundsOf(GameObject root)
    {
        var rs=root.GetComponentsInChildren<Renderer>(true); var b=rs[0].bounds;
        foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds); return b;
    }
    public static void Preview(GameObject source, AnimationClip clip, string name, float time, string modelName = "WomanModel")
    {
        var scene=EditorSceneManager.NewPreviewScene(); var rt=new RenderTexture(600,800,24); var old=RenderTexture.active;
        try
        {
            var copy=Object.Instantiate(source); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,scene);
            copy.SetActive(true); copy.transform.position=Vector3.zero; copy.transform.rotation=Quaternion.identity;
            clip.SampleAnimation(copy.transform.Find(modelName).gameObject,time);
            var b=BoundsOf(copy);
            var go=new GameObject("Preview camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
            var camera=go.AddComponent<Camera>(); camera.scene=scene; camera.cameraType=CameraType.Preview;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            camera.targetTexture=rt; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.12f,.13f,.15f);
            camera.fieldOfView=35; camera.transform.position=b.center+new Vector3(.4f,.1f,3.4f); camera.transform.LookAt(b.center);
            var light=new GameObject("Light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light,scene);
            light.AddComponent<Light>().type=LightType.Directional; light.GetComponent<Light>().intensity=2;
            light.transform.rotation=Quaternion.Euler(30,155,0); camera.Render();
            RenderTexture.active=rt; var image=new Texture2D(600,800,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,600,800),0,0); image.Apply(); File.WriteAllBytes("Temp/Nghi17"+name+".png",image.EncodeToPNG()); Object.DestroyImmediate(image);
        }
        finally { RenderTexture.active=old; Object.DestroyImmediate(rt); EditorSceneManager.ClosePreviewScene(scene); }
    }
}
