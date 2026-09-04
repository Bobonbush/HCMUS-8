using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Explicit request only. Runs a short check in the current game, then leaves Play mode.
[InitializeOnLoad]
public static class NghiWalkingPlayCheck
{
    static Anomoly17 target;
    static float started, firstPhase, maxSpeed, maxRotation;
    static Transform calf;
    static Quaternion firstRotation;
    static NghiWalkingPlayCheck()
    {
        EditorApplication.delayCall += Request;
        EditorApplication.playModeStateChanged += Changed;
    }
    static void Request()
    {
        if (!File.Exists("Temp/NghiWalkPlay.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Temp/NghiWalkPlay.request");
        SessionState.SetBool("NghiWalkPlay",true);
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool("NghiWalkPlay",false)) return;
        if (state==PlayModeStateChange.EnteredPlayMode)
        { started=Time.realtimeSinceStartup; EditorApplication.update += Tick; }
        if (state==PlayModeStateChange.EnteredEditMode)
        { SessionState.SetBool("NghiWalkPlay",false); EditorApplication.update -= Tick; }
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float time=Time.realtimeSinceStartup-started;
        try
        {
            if (time<2) return;
            if (target==null)
            {
                target=UnityEngine.Object.FindObjectsByType<Anomoly17>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                    .FirstOrDefault(a=>a.gameObject.activeInHierarchy);
                if(target==null) throw new Exception("No active floor with #17");
                target.Evaluate();
                calf=target.alternateAppearance.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Bip01 L Calf");
                firstRotation=calf.localRotation; firstPhase=target.alternateAnimation[target.walkClip].time;
            }
            maxSpeed=Mathf.Max(maxSpeed,target.npc.GetComponent<NavMeshAgent>().velocity.magnitude);
            maxRotation=Mathf.Max(maxRotation,Quaternion.Angle(firstRotation,calf.localRotation));
            if(time<9) return;
            float phase=target.alternateAnimation[target.walkClip].time;
            bool pass=maxSpeed>0.2f && maxRotation>10 && phase!=firstPhase;
            File.WriteAllText("Temp/NghiWalkPlay.txt",(pass?"PASS":"FAIL")+": actual NPC patrol speed="+maxSpeed+
                ", calf rotation excursion="+maxRotation+", animation time="+phase+", state="+target.alternateAnimation.IsPlaying(target.walkClip));
            Finish();
        }
        catch(Exception e) { File.WriteAllText("Temp/NghiWalkPlay.txt",e.ToString()); Finish(); }
    }
    static void Finish()
    {
        if(target!=null) target.Restore();
        EditorApplication.update-=Tick;
        EditorApplication.ExitPlaymode();
    }
}
