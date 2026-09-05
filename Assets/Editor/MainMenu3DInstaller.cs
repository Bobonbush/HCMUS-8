using System;
using System.IO;
using System.Linq;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenu3DInstaller
{
    const string ScenePath="Assets/Scenes/UI/MainMenu.unity";
    const string HandPath="Assets/Art/UI/MainMenu3D/RealisticHandsSource/AnatomicalHand_Rigged.glb";
    const string MistFontPath="Assets/Art/UI/MainMenu3D/Fonts/Creepster-Regular.ttf";
    const string MistFontAssetPath="Assets/Art/UI/MainMenu3D/Fonts/Creepster Main Menu SDF.asset";
    const string RoundedMeshPath="Assets/Art/UI/MainMenu3D/Rounded Handset.asset";
    const string RoundedSpritePath="Assets/Art/UI/MainMenu3D/Rounded UI Corners.asset";
    static TMP_FontAsset font;
    static MainMenu3DInstaller()
    {
        EditorApplication.delayCall+=Request;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Request;
        };
    }
    static void Request()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(File.Exists("Temp/MainMenuOptionsStyle.request"))
        {
            File.Delete("Temp/MainMenuOptionsStyle.request");
            try{RestyleOptionsOnly();}catch(Exception e){File.WriteAllText("Temp/MainMenuOptionsStyleReport.txt","FAIL: "+e);Debug.LogException(e);}
        }
        if(!File.Exists("Temp/MainMenu3D.request"))return;
        File.Delete("Temp/MainMenu3D.request");try{Install();}catch(Exception e){File.WriteAllText("Temp/MainMenu3DReport.txt","FAIL: "+e);Debug.LogException(e);}
    }

    [MenuItem("HCMUS-8/UI/Restyle holographic Options only")]
    public static void RestyleOptionsOnly()
    {
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);
        bool loaded=scene.IsValid()&&scene.isLoaded;if(!loaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var menu=FindInScene<MainMenu3DPresentation>(scene);var options=FindInScene<OptionsScreen>(scene);
            if(menu==null||options==null)throw new Exception("Main menu or Options screen missing");
            Color ink=new Color(.72f,1f,.84f,1f),accent=new Color(.28f,1f,.62f,1f);
            int rowIndex=0;
            foreach(var row in options.GetComponentsInChildren<SettingsRow>(true))
            {
                var image=row.GetComponent<Image>();if(image!=null)image.color=rowIndex++%2==0?new Color(.018f,.075f,.058f,.88f):new Color(.024f,.095f,.07f,.9f);
                var bar=row.transform.Find("AccentBar")?.GetComponent<Image>();if(bar!=null)bar.color=new Color(accent.r,accent.g,accent.b,0);
            }
            foreach(var image in options.GetComponentsInChildren<Image>(true))
            {
                if(image.name=="TitlePlate")image.color=new Color(.018f,.09f,.065f,.86f);
                else if(image.name=="ValueBox")image.color=new Color(.055f,.20f,.13f,.95f);
                else if(image.name=="Fill")image.color=accent;
                else if(image.name=="Handle")image.color=new Color(.65f,1f,.79f);
                else if(image.name=="Background")image.color=new Color(.06f,.13f,.1f,.9f);
                else if(image.name=="ColumnHeader")image.color=new Color(.012f,.035f,.03f,.96f);
            }
            foreach(var text in options.GetComponentsInChildren<TMP_Text>(true))text.color=ink;
            var frame=menu.hologramAura?.transform.Find("HologramFrame");
            if(frame!=null){var frameImage=frame.GetComponent<Image>();if(frameImage!=null)frameImage.enabled=false;var frameOutline=frame.GetComponent<Outline>();if(frameOutline!=null)frameOutline.enabled=false;((RectTransform)frame).sizeDelta=new Vector2(1040,920);var scan=frame.Find("ScanLine");if(scan!=null)scan.gameObject.SetActive(false);var duplicate=frame.Find("ProjectionTitle");if(duplicate!=null)duplicate.gameObject.SetActive(false);}
            var glow=menu.hologramAura?.transform.Find("ProjectionGlow")?.GetComponent<Image>();if(glow!=null)glow.enabled=false;
            var veil=menu.hologramAura?.transform.Find("WorldBlur")?.GetComponent<Image>();if(veil!=null)veil.color=new Color(.005f,.035f,.028f,.2f);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/MainMenuOptionsStyleReport.txt","PASS: holographic Options restyled without rebuilding the hand or menu.");
        }
        finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(!loaded)EditorSceneManager.CloseScene(scene,true);}
    }

    [MenuItem("HCMUS-8/UI/Rebuild editable 3D main menu")]
    public static void Install()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath(ScenePath);
        bool alreadyLoaded=scene.IsValid()&&scene.isLoaded;
        if(!alreadyLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            InstallInScene(scene);
        }
        finally
        {
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(!alreadyLoaded)EditorSceneManager.CloseScene(scene,true);
        }
    }

    static void InstallInScene(Scene scene)
    {
        var allTransforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var staleObjects=Array.FindAll(allTransforms,stale=>stale!=null&&(stale.name=="MainMenu3D"||stale.name=="PhoneAndHands"||stale.name=="DoorPrompt"));
        foreach(var stale in staleObjects)if(stale!=null)UnityEngine.Object.DestroyImmediate(stale.gameObject);
        var backend=FindInScene<MainMenuController>(scene);
        var options=FindInScene<OptionsScreen>(scene);
        var camera=FindInScene<Camera>(scene);
        if(backend==null||options==null||camera==null)throw new Exception("Main menu backend, options, or camera missing");
        camera.cullingMask=~0;
        camera.fieldOfView=58f;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        camera.transform.position=new Vector3(69.14f,1.67f,-87.8f);
        camera.transform.rotation=Quaternion.Euler(0,180f,0);
        var canvas=backend.gameObject;
        var legacyButtons=Find(canvas.transform,"MenuButtons");var title=Find(canvas.transform,"Title");
        var background=canvas.transform.Find("Background")?.gameObject;
        if(legacyButtons!=null)legacyButtons.SetActive(false);if(title!=null)title.SetActive(false);if(background!=null)background.SetActive(false);

        var root=new GameObject("MainMenu3D");
        var presentation=root.AddComponent<MainMenu3DPresentation>();
        presentation.presentationRevision=4;
        presentation.backend=backend;presentation.options=options;presentation.viewCamera=camera;presentation.viewPivot=camera.transform;
        presentation.mistFontSource=AssetDatabase.LoadAssetAtPath<Font>(MistFontPath);
        PreparePersistentPresentationAssets(presentation);
        presentation.inputActions=AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        presentation.routeMap=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/MainMenu3D/RideRoute.png");
        presentation.legacyButtons=legacyButtons;presentation.legacyTitle=title;presentation.legacyBackground=background;

        Material charcoal=Mat("Phone anodized charcoal",new Color(.018f,.025f,.03f),.82f,.72f);
        Material glass=Mat("Phone glass",new Color(.015f,.03f,.035f),.95f,.88f);
        var phone=new GameObject("PhoneAndHands").transform;phone.SetParent(camera.transform,false);phone.localPosition=new Vector3(0,-.015f,.69f);phone.localScale=Vector3.one*.8f;presentation.phoneRig=phone;
        Cube(phone,"SleekPhone",Vector3.zero,new Vector3(.34f,.59f,.025f),charcoal);
        var glassObject=Cube(phone,"Glass",new Vector3(0,0,-.014f),new Vector3(.322f,.565f,.006f),glass);presentation.phoneGlass=glassObject.GetComponent<Renderer>();
        Cube(phone,"SideButton",new Vector3(.176f,.12f,0),new Vector3(.012f,.085f,.018f),charcoal);
        Cube(phone,"Speaker",new Vector3(0,.274f,-.022f),new Vector3(.07f,.008f,.004f),charcoal);
        var hands=new GameObject("Hands").transform;hands.SetParent(phone,false);
        AssetDatabase.ImportAsset(HandPath,ImportAssetOptions.ForceSynchronousImport);
        presentation.rightHand=BuildRightPhoneHand(hands);
        PosePhoneHand(presentation.rightHand);

        var screen=WorldCanvas(phone,"PhoneScreen",new Vector2(400,700),new Vector3(0,0,-.021f),new Vector3(.00078f,.00078f,.00078f),camera);
        var cg=screen.gameObject.AddComponent<CanvasGroup>();presentation.phoneScreen=cg;
        // The runtime layout supplies the phone screen and glyph-only PLAY prompt.
        // A font-bearing label is kept here as the serialized UI font reference.
        presentation.floorLabel=Label(screen.transform,"Status","HCMUS8",25,FontStyles.Bold,TextAlignmentOptions.Center,Color.white,new Vector2(.5f,.5f),new Vector2(344,50),Vector2.zero);

        // Instantiate the whole authored Floor, clearing the old renderer/light overrides.
        GameObject oldFloor=Array.Find(scene.GetRootGameObjects(),g=>g.name=="Floor");
        Vector3 floorPosition=oldFloor!=null?oldFloor.transform.position:new Vector3(49.09616f,0,-87.97f);
        Quaternion floorRotation=oldFloor!=null?oldFloor.transform.rotation:Quaternion.identity;
        Vector3 floorScale=oldFloor!=null?oldFloor.transform.localScale:Vector3.one;
        if(oldFloor!=null)UnityEngine.Object.DestroyImmediate(oldFloor);
        var floor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Floor.prefab"),scene);
        floor.name="Floor";floor.transform.SetPositionAndRotation(floorPosition,floorRotation);floor.transform.localScale=floorScale;
        floor.SetActive(true);
        var cabins=floor.GetComponentsInChildren<ElevatorSounds>(true);
        Array.Sort(cabins,(a,b)=>Vector3.Dot(camera.transform.right,a.transform.position-camera.transform.position).CompareTo(Vector3.Dot(camera.transform.right,b.transform.position-camera.transform.position)));
        if(cabins.Length<2)throw new Exception("Floor must contain both authored elevator cabins");
        var left=cabins[0];var right=cabins[cabins.Length-1];
        presentation.leftDoorA=left.doorLeafA;presentation.leftDoorB=left.doorLeafB;
        presentation.leftInnerDoorA=left.doorLeafA2;presentation.leftInnerDoorB=left.doorLeafB2;
        presentation.rightDoorA=right.doorLeafA;presentation.rightDoorB=right.doorLeafB;
        presentation.rightInnerDoorA=right.doorLeafA2;presentation.rightInnerDoorB=right.doorLeafB2;
        Vector3 entrance=(left.doorLeafA.position+left.doorLeafB.position)*.5f;
        var prompt=WorldCanvas(root.transform,"DoorPrompt",new Vector2(520,260),new Vector3(entrance.x,1.9f,-92.46f),Vector3.one*.003f,camera);
        prompt.transform.rotation=Quaternion.Euler(0,180,0);
        presentation.doorPrompt=prompt.gameObject.AddComponent<CanvasGroup>();

        // Build the complete phone, PLAY? lettering, and title now, in Edit Mode. Runtime only
        // binds these serialized objects, so artists can tweak every child in the Inspector.
        MainMenuPhoneLayout.Build(presentation,out _,out _);

        var spill=new GameObject("ScreenSpill",typeof(Light));spill.transform.SetParent(phone,false);spill.transform.localPosition=new Vector3(0,-.07f,-.2f);
        var spillLight=spill.GetComponent<Light>();spillLight.type=LightType.Point;spillLight.range=.85f;spillLight.intensity=.22f;spillLight.color=new Color(.72f,.88f,.8f);spillLight.shadows=LightShadows.None;

        var aura=ScreenCanvas(root.transform,"HologramAura");aura.sortingOrder=20;presentation.hologramAura=aura.gameObject.AddComponent<CanvasGroup>();
        var captured=new GameObject("CapturedBackgroundBlur",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(PauseBlur));captured.transform.SetParent(aura.transform,false);Stretch(captured.GetComponent<RectTransform>());captured.GetComponent<RawImage>().raycastTarget=false;
        var blur=Panel(aura.transform,"WorldBlur",new Color(.005f,.055f,.045f,.36f),new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);Stretch(blur.rectTransform);
        var holo=Panel(aura.transform,"ProjectionGlow",new Color(.06f,.56f,.35f,.13f),new Vector2(.5f,.5f),new Vector2(900,650),Vector2.zero);
        var frame=Panel(aura.transform,"HologramFrame",new Color(.015f,.11f,.085f,.12f),new Vector2(.5f,.5f),new Vector2(1040,920),Vector2.zero);
        var outline=frame.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.36f,1f,.7f,.82f);outline.effectDistance=new Vector2(2,-2);
        Label(frame.transform,"ProjectionTitle","DEVICE SETTINGS  //  HOLOGRAPHIC LINK",25,FontStyles.Bold,TextAlignmentOptions.TopLeft,new Color(.55f,1,.75f,0),new Vector2(.5f,1),new Vector2(770,55),new Vector2(0,-24));
        var scan=Panel(frame.transform,"ScanLine",new Color(.45f,1f,.72f,.3f),new Vector2(.5f,.5f),new Vector2(790,2),new Vector2(0,244));scan.raycastTarget=false;
        holo.enabled=false;frame.enabled=false;outline.enabled=false;scan.gameObject.SetActive(false);
        presentation.hologramAura.alpha=0;presentation.hologramAura.blocksRaycasts=false;
        var volumeObject=new GameObject("HologramBackgroundBlur");volumeObject.transform.SetParent(root.transform,false);
        var volume=volumeObject.AddComponent<Volume>();volume.isGlobal=true;volume.weight=0;presentation.hologramBlur=volume;
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Art/UI/MainMenu3D/HologramBlur.asset");
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,"Assets/Art/UI/MainMenu3D/HologramBlur.asset");}
        if(!profile.TryGet<DepthOfField>(out var depth)){depth=profile.Add<DepthOfField>();}
        depth.active=true;depth.mode.Override(DepthOfFieldMode.Gaussian);depth.gaussianStart.Override(0);depth.gaussianEnd.Override(8);depth.gaussianMaxRadius.Override(1.5f);volume.sharedProfile=profile;
        if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>();bloom.active=true;bloom.intensity.Override(.32f);bloom.threshold.Override(.7f);
        if(!profile.TryGet<Vignette>(out var vignette))vignette=profile.Add<Vignette>();vignette.active=true;vignette.color.Override(new Color(0,.09f,.055f));vignette.intensity.Override(.32f);vignette.smoothness.Override(.55f);
        var optionsCanvas=options.GetComponent<Canvas>();if(optionsCanvas==null)optionsCanvas=options.gameObject.AddComponent<Canvas>();optionsCanvas.overrideSorting=true;optionsCanvas.sortingOrder=40;
        if(options.GetComponent<GraphicRaycaster>()==null)options.gameObject.AddComponent<GraphicRaycaster>();
        foreach(var image in options.GetComponentsInChildren<Image>(true))
        {
            Color c=image.color;float a=Mathf.Clamp(c.a*.68f,.12f,.72f);image.color=new Color(c.r,c.g,c.b,a);
        }
        foreach(var text in options.GetComponentsInChildren<TMP_Text>(true))text.color=new Color(.7f,1f,.82f,text.color.a);

        var overlay=ScreenCanvas(root.transform,"TransitionOverlay");presentation.blackout=overlay.gameObject.AddComponent<CanvasGroup>();
        presentation.blackout.alpha=0;presentation.blackout.blocksRaycasts=false;
        var black=Panel(overlay.transform,"Blackout",Color.black,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);Stretch(black.rectTransform);

        // Remove gameplay scripts on this scene instance: disabled MonoBehaviours still
        // execute Awake (ElevatorSounds would otherwise open the authored closed doors).
        // Preserve all renderers, materials, lights, volumes, and authored active states.
        // This lobby is floor 11. Bake that number before removing gameplay scripts from the
        // decorative menu-scene copy of the Floor prefab.
        foreach(var display in floor.GetComponentsInChildren<ElevatorDisplay>(true))
        {
            var number=display.GetComponent<TMP_Text>();
            if(number!=null){number.text="11";number.enabled=true;}
        }
        foreach(var behaviour in floor.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if(behaviour==null)continue;
            var ns=behaviour.GetType().Namespace??"";
            if(ns.StartsWith("UnityEngine.Rendering")||ns.StartsWith("TMPro")||behaviour is ClockHands)continue;
            UnityEngine.Object.DestroyImmediate(behaviour);
        }
        foreach(var collider in floor.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Temp/MainMenu3DReport.txt","PASS: editable wall title/PLAY; one rigged right hand holding the phone with bounded thumb taps; swapped elevators; holographic blurred Options; floor-11 LEDs.");
    }

    static T FindInScene<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).FirstOrDefault();

    static Transform BuildRightPhoneHand(Transform parent)
    {
        var root=new GameObject("RightPhoneHand").transform;root.SetParent(parent,false);root.localPosition=new Vector3(.205f,-.17f,.018f);root.localEulerAngles=new Vector3(90,-8,18);root.localScale=Vector3.one*.94f;
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(HandPath);if(asset==null)throw new Exception("Realistic CC0 hand model failed to import");
        var model=UnityEngine.Object.Instantiate(asset,root);model.name="AnatomicalHand_Rigged";model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
        NormalizeModel(model.transform,.325f);
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){renderer.enabled=true;renderer.shadowCastingMode=ShadowCastingMode.Off;}
        return root;
    }

    [MenuItem("HCMUS-8/UI/Validate editable 3D main menu")]
    public static void Validate()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath(ScenePath);
        bool alreadyLoaded=scene.IsValid()&&scene.isLoaded;
        if(!alreadyLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var menu=FindInScene<MainMenu3DPresentation>(scene);
            if(menu==null||menu.presentationRevision!=4)throw new Exception("Editable main-menu revision 4 is missing");
            MainMenuPhoneLayout.BindExisting(menu,out _,out var mist);
            if(menu.phoneRig==null||menu.playButton==null||menu.settingsButton==null||menu.cancelButton==null||mist==null)
                throw new Exception("Serialized phone/title/PLAY references are incomplete");
            var arm=menu.phoneRig.Find("Hands/RightPhoneHand");
            if(arm==null||menu.phoneRig.Find("Hands/SupportHand")!=null||menu.phoneRig.Find("Hands/PointerHand")!=null)throw new Exception("Exactly one RightPhoneHand must replace the old two-hand setup");
            foreach(string boneName in new[]{"thumb_trapez","thumb_meta","thumb_prox","thumb_dist"})
                if(!arm.GetComponentsInChildren<Transform>(true).Any(t=>t.name==boneName))throw new Exception("Thumb bone missing: "+boneName);
            int validMeshes=arm.GetComponentsInChildren<Renderer>(true).Count(r=>r.localBounds.size.sqrMagnitude>0);
            if(validMeshes<2)throw new Exception("Downloaded arm or anatomical hand render mesh is empty");
            var wall=menu.transform.Find("GameIdentity/WallTitle");
            if(wall==null||wall.GetComponent<Canvas>()==null||wall.GetComponent<Canvas>().renderMode!=RenderMode.WorldSpace)throw new Exception("Editable world-space wall title is missing");
            if(menu.hologramAura==null||menu.hologramAura.GetComponentInChildren<PauseBlur>(true)==null||menu.hologramBlur==null)throw new Exception("Hologram aura or captured background blur is missing");
            Vector3 prompt=menu.doorPrompt.transform.position;
            Vector3 leftCenter=(menu.leftDoorA.position+menu.leftDoorB.position)*.5f,rightCenter=(menu.rightDoorA.position+menu.rightDoorB.position)*.5f;
            if(Vector3.Distance(prompt,leftCenter)>=Vector3.Distance(prompt,rightCenter))throw new Exception("PLAY prompt was not switched to the left screen elevator");
            int floorEleven=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMP_Text>(true)).Count(t=>t.text=="11");
            if(floorEleven<1)throw new Exception("No elevator LED is serialized as floor 11");
            Debug.Log($"MAIN_MENU_V4_VALIDATION_PASS: one right hand, {validMeshes} renderers, bounded thumb rig, swapped elevators, editable wall title, holographic Options, {floorEleven} floor-11 LED(s).");
        }
        finally
        {
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(!alreadyLoaded)EditorSceneManager.CloseScene(scene,true);
        }
    }

    [MenuItem("HCMUS-8/UI/Capture main menu preview")]
    public static void CapturePreview()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath(ScenePath);
        bool alreadyLoaded=scene.IsValid()&&scene.isLoaded;
        if(!alreadyLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var camera=FindInScene<Camera>(scene);if(camera==null)throw new Exception("Main-menu camera missing");
            const int width=1280,height=720;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            RenderTexture prior=RenderTexture.active;var oldTarget=camera.targetTexture;
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                File.WriteAllBytes("Temp/MainMenuV4Preview.png",image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=prior;
                UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
            }
            Debug.Log("MAIN_MENU_V4_PREVIEW: Temp/MainMenuV4Preview.png");
        }
        finally
        {
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(!alreadyLoaded)EditorSceneManager.CloseScene(scene,true);
        }
    }

    static void PosePhoneHand(Transform root)
    {
        // One large right hand wraps the handset. The thumb remains free over the screen and side
        // button; runtime adds only small joint rotations to this stable grip pose.
        PoseFinger(root,"pinky",44,58,24); PoseFinger(root,"ring",40,54,23);
        PoseFinger(root,"midd",34,47,20); PoseFinger(root,"index",27,38,16);
        RotateBone(root,"thumb_trapez",new Vector3(6,-8,-4));RotateBone(root,"thumb_meta",new Vector3(10,-8,-9));
        RotateBone(root,"thumb_prox",new Vector3(16,-4,-12)); RotateBone(root,"thumb_dist",new Vector3(10,0,-3));
    }

    static void PoseFinger(Transform root,string finger,float proximal,float middle,float distal)
    {
        RotateBone(root,finger+"_prox",new Vector3(proximal,0,0));
        RotateBone(root,finger+"_midd",new Vector3(middle,0,0));
        RotateBone(root,finger+"_dist",new Vector3(distal,0,0));
    }

    static void RotateBone(Transform root,string name,Vector3 euler)
    {
        var bone=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        if(bone!=null)bone.localRotation*=Quaternion.Euler(euler);
    }

    static void NormalizeModel(Transform model,float targetSize)
    {
        Bounds bounds=new Bounds();bool first=true;
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Bounds b=renderer.localBounds;
            for(int i=0;i<8;i++)
            {
                Vector3 corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                Vector3 local=model.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                if(first){bounds=new Bounds(local,Vector3.zero);first=false;}else bounds.Encapsulate(local);
            }
        }
        if(first)return;
        float size=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
        float scale=targetSize/Mathf.Max(.00001f,size);
        model.localScale=Vector3.one*scale;model.localPosition=-bounds.center*scale;
    }

    static void PreparePersistentPresentationAssets(MainMenu3DPresentation presentation)
    {
        var mist=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MistFontAssetPath);
        if(mist==null)
        {
            mist=TMP_FontAsset.CreateFontAsset(presentation.mistFontSource);
            mist.name="Creepster Main Menu SDF";
            AssetDatabase.CreateAsset(mist,MistFontAssetPath);
            if(mist.material!=null&&!AssetDatabase.Contains(mist.material))AssetDatabase.AddObjectToAsset(mist.material,mist);
            foreach(var atlas in mist.atlasTextures)if(atlas!=null&&!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,mist);
        }
        presentation.mistFontAsset=mist;

        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(RoundedMeshPath);
        if(mesh==null){mesh=MainMenuPhoneLayout.CreateRoundedHandsetMesh();AssetDatabase.CreateAsset(mesh,RoundedMeshPath);}
        presentation.roundedHandsetMesh=mesh;

        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(RoundedSpritePath);
        if(texture==null)
        {
            texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Rounded UI Corners",wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                Vector2 q=new Vector2(Mathf.Max(Mathf.Abs(x-15.5f)-7.5f,0),Mathf.Max(Mathf.Abs(y-15.5f)-7.5f,0));
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8.5f-q.magnitude)));
            }
            texture.Apply();AssetDatabase.CreateAsset(texture,RoundedSpritePath);
            var sprite=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,Vector4.one*10);sprite.name="Rounded UI Corners Sprite";
            AssetDatabase.AddObjectToAsset(sprite,texture);AssetDatabase.ImportAsset(RoundedSpritePath);
        }
        presentation.roundedPanelSprite=AssetDatabase.LoadAllAssetsAtPath(RoundedSpritePath).OfType<Sprite>().FirstOrDefault();
    }
    static GameObject Cube(Transform p,string n,Vector3 lp,Vector3 scale,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;return g;}
    static GameObject Capsule(Transform p,string n,Vector3 lp,Vector3 scale,Material m,Vector3 rot){var g=GameObject.CreatePrimitive(PrimitiveType.Capsule);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localEulerAngles=rot;g.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;return g;}
    static Material Mat(string name,Color color,float smooth,float metal){var path="Assets/Art/UI/MainMenu3D/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);EditorUtility.SetDirty(m);return m;}
    static Canvas WorldCanvas(Transform p,string n,Vector2 size,Vector3 lp,Vector3 scale,Camera camera){var g=new GameObject(n,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localRotation=Quaternion.identity;g.transform.localScale=scale;var r=(RectTransform)g.transform;r.sizeDelta=size;var c=g.GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.worldCamera=camera;c.sortingOrder=30;return c;}
    static Canvas ScreenCanvas(Transform p,string n){var g=new GameObject(n,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));g.transform.SetParent(p,false);var c=g.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=500;return c;}
    static Image Panel(Transform p,string n,Color c,Vector2 anchor,Vector2 size,Vector2 pos){var g=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;r.anchoredPosition=pos;var i=g.GetComponent<Image>();i.color=c;return i;}
    static RawImage Raw(Transform p,string n,Color c){var g=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));g.transform.SetParent(p,false);var i=g.GetComponent<RawImage>();i.color=c;return i;}
    static TMP_Text Label(Transform p,string n,string text,float size,FontStyles style,TextAlignmentOptions align,Color color,Vector2 anchor,Vector2 dimensions,Vector2 pos){var g=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=anchor;r.sizeDelta=dimensions;r.anchoredPosition=pos;var t=g.GetComponent<TextMeshProUGUI>();t.text=text;t.font=Font;t.fontSize=size;t.fontStyle=style;t.alignment=align;t.color=color;t.textWrappingMode=TextWrappingModes.NoWrap;return t;}
    static Button Button(Transform p,string n,string text,Vector2 pos,Vector2 size,Color color){var panel=Panel(p,n,color,new Vector2(.5f,0),size,pos);var b=panel.gameObject.AddComponent<Button>();var label=Label(panel.transform,"Label",text,19,FontStyles.Bold,TextAlignmentOptions.Center,new Color(.04f,.17f,.13f),new Vector2(.5f,.5f),size,Vector2.zero);b.targetGraphic=panel;return b;}
    static TMP_FontAsset Font{get{if(font==null)font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/UIFontSDF.asset");return font;}}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static GameObject Find(Transform root,string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t.gameObject;return null;}
}

